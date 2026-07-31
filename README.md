# SlateDB .NET

.NET bindings for [SlateDB](https://github.com/slatedb/slatedb), an embedded key-value store on object storage.

[![NuGet](https://img.shields.io/nuget/v/Pulsy.SlateDB)](https://www.nuget.org/packages/Pulsy.SlateDB)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](LICENSE)

```bash
dotnet add package Pulsy.SlateDB
```

## Usage

```csharp
using var db = SlateDb.Builder("my-db", new ObjectStoreConfig
    {
        Bucket   = "my-bucket",
        Region   = "us-east-1",
        Endpoint = "http://localhost:9000",
    })
    .Build();

db.Put("deck", "steam");
db.GetString("deck");        // "steam"
db.Get<int>("score");        // null
db.Delete("deck");
```

Typed metrics include counters, gauges, up/down counters, and complete
histogram snapshots:

```csharp
using Pulsy.SlateDB.Metrics;

IReadOnlyList<SlateDbMetric> metrics = db.GetMetrics();

var writeOps = db.GetMetric("slatedb.db.write_ops");
if (writeOps?.Value is SlateDbCounterMetricValue counter)
    Console.WriteLine(counter.Value);

var requestMetrics = db.GetMetrics("slatedb.object_store.request_duration_seconds");
```

See the [example project](Pulsy.SlateDB.Example/Program.cs) for the full API.

## Building from Source

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download), [Rust 1.91.1](https://rustup.rs/), and, for Linux cross-architecture builds, [zig](https://ziglang.org/) with [cargo-zigbuild](https://github.com/rust-cross/cargo-zigbuild).

```bash
./generate-uniffi-bindings.sh  # regenerate C# from SlateDB's UniFFI metadata
./build-native.sh              # build the native library for the current platform
./build-native.sh --all        # build every target supported by the current host

dotnet build Pulsy.SlateDB/Pulsy.SlateDB.csproj
dotnet test Pulsy.SlateDB.sln
```

Both scripts use SlateDB `v0.15.0` by default. Set `SLATEDB_REF` to build or generate from another tag or commit.

## Versioning

Tracks SlateDB's official [UniFFI binding](https://github.com/slatedb/slatedb/tree/main/bindings/uniffi). The fourth version segment is reserved for .NET binding-only changes.

## License

Apache-2.0
