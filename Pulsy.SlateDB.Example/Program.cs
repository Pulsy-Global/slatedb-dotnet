using System.Text;
using Pulsy.SlateDB;
using Pulsy.SlateDB.Options;

SlateDb.InitLogging(LogLevel.Info);

/*
using var db = SlateDb.Builder("my-db", new ObjectStoreConfig
    {
        Bucket = "my-bucket", Region = "us-east-1", Endpoint = "http://localhost:9000",
        AccessKeyId = "...", SecretAccessKey = "...",
    })
    .WithSettings(new SlateDbSettings
    {
        CompressionCodec = CompressionCodec.Zstd,
    })
    .Build();
*/

using var db = SlateDb.Builder("my-db", "file:///tmp/slatedb-example").Build();

db.Put("key", "value");
Console.WriteLine(db.GetString("key"));         // "value"

db.Put("key", 42);
Console.WriteLine(db.Get<int>("key"));           // 42

db.Put("key_raw", Encoding.UTF8.GetBytes("raw"));
Console.WriteLine(db.Get("key_raw") is not null); // True

db.Delete("key_raw");

using var batch = SlateDb.NewWriteBatch();
batch.Put("user:alice", "admin");
batch.Put("user:bob", "viewer");
batch.Put("user:charlie", "editor");
batch.Delete("key");
db.Write(batch);

foreach (var kv in db.ScanPrefix("user:"))
    Console.WriteLine($"{kv.KeyString} = {kv.ValueString}");

foreach (var kv in db.Scan("user:alice", "user:charlie"))
    Console.WriteLine($"{kv.KeyString} = {kv.ValueString}");

using var reader = SlateDb.OpenReader("my-db", "file:///tmp/slatedb-example", null, null);
Console.WriteLine(reader.GetString("user:alice")); // "admin"
