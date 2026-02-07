using System.Text;
using Pulsy.SlateDB;
using Pulsy.SlateDB.Options;

LoadEnvFile(FindEnvFile());

var prefix = Env("SLATEDB_PREFIX") ?? "example";

SlateDb.InitLogging(LogLevel.Info);

using var db = SlateDb.Builder(prefix, new ObjectStoreConfig
    {
        Bucket         = Env("AWS_BUCKET") ?? "my-bucket",
        Region         = Env("AWS_REGION"),
        Endpoint       = Env("AWS_ENDPOINT_URL"),
        AccessKeyId    = Env("AWS_ACCESS_KEY_ID"),
        SecretAccessKey = Env("AWS_SECRET_ACCESS_KEY"),
    })
    .WithSettings(new SlateDbSettings
    {
        CompressionCodec = CompressionCodec.Zstd,
    })
    .Build();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

Console.WriteLine($"SlateDB: {prefix}\n");


var seedData = new (string Key, string Value)[]
{
    ("user:alice",   """{"name":"Alice","age":30,"role":"admin"}"""),
    ("user:bob",     """{"name":"Bob","age":25,"role":"user"}"""),
    ("user:charlie", """{"name":"Charlie","age":35,"role":"user"}"""),
    ("user:diana",   """{"name":"Diana","age":28,"role":"moderator"}"""),
    ("user:eve",     """{"name":"Eve","age":22,"role":"user"}"""),
    ("order:0001",   """{"user":"alice","item":"laptop","price":1200}"""),
    ("order:0002",   """{"user":"bob","item":"keyboard","price":80}"""),
    ("order:0003",   """{"user":"alice","item":"monitor","price":400}"""),
    ("order:0004",   """{"user":"charlie","item":"mouse","price":25}"""),
    ("order:0005",   """{"user":"diana","item":"headset","price":150}"""),
    ("order:0006",   """{"user":"bob","item":"webcam","price":60}"""),
    ("order:0007",   """{"user":"eve","item":"chair","price":300}"""),
    ("order:0008",   """{"user":"alice","item":"desk","price":500}"""),
};


while (!cts.Token.IsCancellationRequested)
{
    Console.WriteLine("""
        ────────────────────────────────────────
          1) Write      — seed users and orders
          2) Get        — point lookups
          3) Scan all   — scan all keys
          4) Prefix     — scan by prefix
          5) Range      — scan a key range
          6) Paginate   — cursor-based pagination
          7) Batch      — atomic batch write
          8) Delete     — delete a key
          q) Quit
        ────────────────────────────────────────
        """);
    Console.Write("> ");

    switch (Console.ReadLine()?.Trim().ToLowerInvariant())
    {
        case "1" or "write":    Write();    break;
        case "2" or "get":      Get();      break;
        case "3" or "scan":     ScanAll();  break;
        case "4" or "prefix":   Prefix();   break;
        case "5" or "range":    Range();    break;
        case "6" or "page":     Paginate(); break;
        case "7" or "batch":    Batch();    break;
        case "8" or "delete":   Delete();   break;
        case "q" or "quit" or "exit": return;
        default: Console.WriteLine("Unknown option."); break;
    }
}


void Write()
{
    foreach (var (key, value) in seedData)
        db.Put(key, value);
    Console.WriteLine($"  Wrote {seedData.Length} keys\n");
}

void Get()
{
    foreach (var key in new[] { "user:alice", "user:bob", "user:nonexistent", "order:0001" })
        Console.WriteLine($"  {key} → {db.GetString(key) ?? "(null)"}");
    Console.WriteLine();
}

void ScanAll()
{
    Console.WriteLine("  All keys:");
    using var iter = db.Scan((string?)null, (string?)null);
    foreach (var kv in iter)
        PrintKv(kv);
    Console.WriteLine();
}

void Prefix()
{
    foreach (var pfx in new[] { "user:", "order:" })
    {
        Console.WriteLine($"  Prefix \"{pfx}\":");
        using var iter = db.ScanPrefix(pfx);
        foreach (var kv in iter)
            PrintKv(kv);
    }
    Console.WriteLine();
}

void Range()
{
    const string from = "order:0003", to = "order:0006";
    Console.WriteLine($"  Range [{from} .. {to}):");
    using var iter = db.Scan(from, to);
    foreach (var kv in iter)
        PrintKv(kv);
    Console.WriteLine();
}

void Paginate()
{
    const int pageSize = 3;
    var cursor = "order:";
    var page = 0;

    Console.WriteLine($"  Paginating orders (page size = {pageSize}):");
    while (true)
    {
        page++;
        var items = new List<(string Key, string Value)>();

        using (var iter = db.Scan(cursor, "order:\xff"))
            foreach (var kv in iter)
            {
                items.Add((Str(kv.Key), Str(kv.Value)));
                if (items.Count >= pageSize) break;
            }

        if (items.Count == 0) break;

        Console.WriteLine($"  Page {page}:");
        foreach (var (key, val) in items)
            Console.WriteLine($"    {key} → {val}");

        cursor = items[^1].Key + "\0";
    }
    Console.WriteLine();
}

void Batch()
{
    using var batch = SlateDb.NewWriteBatch();
    batch.Put("user:frank", """{"name":"Frank","age":40,"role":"user"}""");
    batch.Put("order:0009", """{"user":"frank","item":"tablet","price":350}""");
    batch.Delete("user:eve");
    db.Write(batch);
    Console.WriteLine("  Batch: +user:frank, +order:0009, -user:eve\n");
}

void Delete()
{
    const string key = "order:0008";
    db.Delete(key);
    Console.WriteLine($"  Deleted \"{key}\" → Get: {db.GetString(key) ?? "(null)"}\n");
}

void PrintKv(SlateDbKeyValue kv) =>
    Console.WriteLine($"    {Str(kv.Key)} → {Str(kv.Value)}");

static string Str(byte[] bytes) => Encoding.UTF8.GetString(bytes);
static string? Env(string name) => Environment.GetEnvironmentVariable(name);

static string? FindEnvFile()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null)
    {
        var path = Path.Combine(dir.FullName, ".env");
        if (File.Exists(path)) return path;
        dir = dir.Parent;
    }
    return null;
}

static void LoadEnvFile(string? path)
{
    if (path is null || !File.Exists(path)) return;
    foreach (var line in File.ReadLines(path))
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;
        var eq = trimmed.IndexOf('=');
        if (eq <= 0) continue;
        Environment.SetEnvironmentVariable(trimmed[..eq].Trim(), trimmed[(eq + 1)..].Trim());
    }
}
