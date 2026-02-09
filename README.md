# SlateDB .NET

.NET bindings for [SlateDB](https://github.com/slatedb/slatedb), an embedded key-value store on object storage.

[![NuGet](https://img.shields.io/nuget/v/Pulsy.SlateDB)](https://www.nuget.org/packages/Pulsy.SlateDB)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](LICENSE)

```bash
dotnet add package Pulsy.SlateDB
```

## Quick Start

```csharp
using var db = SlateDb.Builder("my-db", new ObjectStoreConfig
    {
        Bucket = "my-bucket", Region = "us-east-1", Endpoint = "http://localhost:9000",
    })
    .Build();

db.Put("greeting", "hello world");
db.GetString("greeting");   // "hello world"
db.Delete("greeting");
```

Also accepts [.env file](https://github.com/slatedb/slatedb/tree/main/slatedb-go#environment-variables): `SlateDb.Builder("my-db", envFile: ".env")`.

## API

| Operation  | Example                                                                        |
|------------|--------------------------------------------------------------------------------|
| **Get**    | `db.GetString("key")`, `db.Get<int>("key")`, `db.Get("key")` returns `byte[]?` |
| **Put**    | `db.Put("key", "value")`, `db.Put("key", 42)`, `db.Put("key", bytes)`          |
| **Delete** | `db.Delete("key")`                                                             |
| **Batch**  | `batch.Put(...)` / `batch.Delete(...)`, then `db.Write(batch)`                 |
| **Scan**   | `db.Scan(start, end)`, `db.ScanPrefix("user:")` - iterable via `foreach`       |
| **Reader** | `SlateDb.OpenReader(...)` - read-only checkpoint-pinned view                   |

Supports `string`, `int`, `long`, `double`, `bool`, `byte[]`. Optional `PutOptions` (TTL), `ReadOptions`, `ScanOptions`.

## Settings

All nullable — set only overrides, defaults come from Rust at runtime:

```csharp
.WithSettings(new SlateDbSettings
{
    CompressionCodec = CompressionCodec.Zstd,
    L0SstSizeBytes   = 64 * 1024 * 1024,
})
```

```csharp
SlateDb.SettingsDefault();                // Rust defaults (JSON)
SlateDb.SettingsFromFile("slatedb.toml"); // TOML file
SlateDb.SettingsFromEnv("SLATEDB_");      // Env vars
SlateDb.SettingsLoad();                   // Auto: file > env > defaults
```

See [example project](Pulsy.SlateDB.Example/) for full usage.

## Building from Source

**Prerequisites:** [.NET 9 SDK](https://dotnet.microsoft.com/download), [Rust nightly](https://rustup.rs/), cross-platform: [zig](https://ziglang.org/) + [cargo-zigbuild](https://github.com/rust-cross/cargo-zigbuild)

```bash
./build-native.sh        # current platform
./build-native.sh --all  # all 6 platforms

dotnet build Pulsy.SlateDB/Pulsy.SlateDB.csproj
dotnet test
```

## Versioning

Tracks [slatedb-c](https://github.com/slatedb/slatedb/tree/main/slatedb-c). 4th segment is for binding-only changes.

## License

Apache-2.0
