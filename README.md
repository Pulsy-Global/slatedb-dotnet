# SlateDB .NET

.NET bindings for [SlateDB](https://github.com/slatedb/slatedb) — an embedded key-value store built on object storage.

[![NuGet](https://img.shields.io/nuget/v/Pulsy.SlateDB)](https://www.nuget.org/packages/Pulsy.SlateDB)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](LICENSE)

## Installation

```bash
dotnet add package Pulsy.SlateDB
```

## Quick Start

```csharp
using Pulsy.SlateDB;
using Pulsy.SlateDB.Options;

using var db = SlateDb.Builder("my-db", new ObjectStoreConfig
    {
        Bucket   = "my-bucket",
        Region   = "us-east-1",
        Endpoint = "http://localhost:9000",
    })
    .Build();

db.Put("greeting", "hello world");

var value = db.GetString("greeting");   // "hello world"

db.Delete("greeting");
```

The builder also accepts an `.env` file or URL instead of `ObjectStoreConfig`: `SlateDb.Builder("my-db", envFile: ".env")`.

## API Overview

| Operation | Example |
|-----------|---------|
| **Get** | `db.GetString("key")`, `db.Get<int>("key")`, `db.Get("key")` → `byte[]?` |
| **Put** | `db.Put("key", "value")`, `db.Put("key", 42)`, `db.Put("key", bytes)` |
| **Delete** | `db.Delete("key")` |
| **Batch** | `batch.Put(...)` / `batch.Delete(...)` → `db.Write(batch)` |
| **Scan** | `db.Scan(start, end)`, `db.ScanPrefix("user:")` → `foreach` |
| **Reader** | `SlateDb.OpenReader(...)` — read-only view with checkpoint pinning |

`Put` and `Get` support `string`, `int`, `long`, `double`, `bool`, and `byte[]`. Overloads accept `PutOptions` (TTL), `WriteOptions`, `ReadOptions`, and `ScanOptions`.

## Settings

All settings are nullable — set only what you want to override. Defaults come from the Rust engine at runtime:

```csharp
using var db = SlateDb.Builder("my-db", new ObjectStoreConfig { Bucket = "my-bucket" })
    .WithSettings(new SlateDbSettings
    {
        CompressionCodec = CompressionCodec.Zstd,
        L0SstSizeBytes   = 64 * 1024 * 1024,
    })
    .Build();
```

Settings can also be loaded from external sources:

```csharp
string defaults = SlateDb.SettingsDefault();              // Rust defaults as JSON
string fromFile = SlateDb.SettingsFromFile("slatedb.toml"); // TOML file
string fromEnv  = SlateDb.SettingsFromEnv("SLATEDB_");    // Environment variables
string loaded   = SlateDb.SettingsLoad();                 // Auto: file → env → defaults
```

See the [example project](Pulsy.SlateDB.Example/) for full usage of all operations.

## Building from Source

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Rust nightly](https://rustup.rs/) — `rustup install nightly`
- For cross-platform builds: [zig](https://ziglang.org/) + [cargo-zigbuild](https://github.com/rust-cross/cargo-zigbuild)

### Build native libraries

```bash
# Current platform only
./build-native.sh

# All 6 platforms (osx-arm64, osx-x64, linux-arm64, linux-x64, win-arm64, win-x64)
./build-native.sh --all
```

The script clones `slatedb` into `.slatedb-src/` automatically if no source path is provided.

### Build the .NET project

```bash
dotnet build Pulsy.SlateDB/Pulsy.SlateDB.csproj
dotnet test
```

## Versioning

Package version tracks [slatedb-c](https://github.com/slatedb/slatedb/tree/main/slatedb-c). The 4th version segment is reserved for wrapper-only fixes.

## License

Apache-2.0
