using System.Globalization;
using System.Text.Json.Nodes;
using FluentAssertions;
using Pulsy.SlateDB.Options;
using Xunit;

namespace Pulsy.SlateDB.Tests;

[Collection("SlateDb")]
public class SlateDbBuilderTests
{
    private readonly SlateDbFixture _fixture;

    public SlateDbBuilderTests(SlateDbFixture fixture) => _fixture = fixture;

    [Fact]
    public void Builder_Build_OpensDb()
    {
        var url = $"file:///{_fixture.TempDir}/builder_1";
        using var db = SlateDb.Builder("test", url).Build();

        db.Put("bk", "bv");
        db.GetString("bk").Should().Be("bv");
    }

    [Fact]
    public void Builder_WithTypedSettings_OpensDb()
    {
        var url = $"file:///{_fixture.TempDir}/builder_3";
        using var db = SlateDb.Builder("test", url)
            .WithSettings(new SlateDbSettings
            {
                CompressionCodec = CompressionCodec.Lz4,
            })
            .Build();

        db.Put("tk", "tv");
        db.GetString("tk").Should().Be("tv");
    }

    [Fact]
    public void Builder_WithSettingsMovedByV015_OpensDb()
    {
        var url = $"file:///{_fixture.TempDir}/builder_v015_settings";
        using var db = SlateDb.Builder("test", url)
            .WithSettings(new SlateDbSettings
            {
                FilterBitsPerKey = 12,
                MaxWalFlushesBeforeL0Flush = 4096,
                L0MaxSstsPerKey = 4,
                L0FlushParallelism = 2,
                MetricLevel = MetricLevel.Debug,
                ObjectStoreMaxRetries = 3,
                CompactorOptions = new CompactorOptions
                {
                    MaxSstSize = 64 * 1024 * 1024,
                    EnableTrivialMove = true,
                    WorkerOptions = new CompactionWorkerOptions
                    {
                        MaxFetchTasks = 2,
                        BytesToFetch = 1024 * 1024,
                        MaxSubcompactions = 2,
                    },
                    SchedulerOptions = new CompactionSchedulerOptions
                    {
                        MinCompactionSources = 2,
                        MaxCompactionSources = 4,
                        IncludeSizeThreshold = 3,
                    },
                },
                CacheOptions = new CacheOptions
                {
                    CachePuts = true,
                    MaxOpenFileHandles = 128,
                },
            })
            .Build();

        db.Put("v015", "settings");
        db.GetString("v015").Should().Be("settings");
    }

    [Fact]
    public void TypedSettings_UsesV015NativeShape()
    {
        var json = SlateDbSettingsSerializer.ToJson(new SlateDbSettings
        {
            FilterBitsPerKey = 12,
            ObjectStoreMaxRetries = 3,
            CompactorOptions = new CompactorOptions
            {
                MaxSstSize = 64 * 1024 * 1024,
            },
            CacheOptions = new CacheOptions
            {
                CachePuts = true,
            },
        });

        var root = JsonNode.Parse(json)!.AsObject();

        root.ContainsKey("filter_bits_per_key").Should().BeFalse();
        root["object_store_max_retries"]!.GetValue<uint>().Should().Be(3);
        root["compactor_options"]!["worker"]!["max_sst_size"]!
            .GetValue<ulong>().Should().Be(64 * 1024 * 1024);
        root["object_store_cache_options"]!["cache_on_flush"]!
            .GetValue<bool>().Should().BeTrue();
        root["object_store_cache_options"]!["cache_on_compaction"]!
            .GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public void TypedSettings_FormatsSchedulerThresholdInvariantly()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pl-PL");
            var json = SlateDbSettingsSerializer.ToJson(new SlateDbSettings
            {
                CompactorOptions = new CompactorOptions
                {
                    SchedulerOptions = new CompactionSchedulerOptions
                    {
                        IncludeSizeThreshold = 3.5f,
                    },
                },
            });

            JsonNode.Parse(json)!["compactor_options"]!["scheduler_options"]!["include_size_threshold"]!
                .GetValue<string>().Should().Be("3.5");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public void TypedSettings_AlignsCompactorWorkerWithWriter()
    {
        var json = SlateDbSettingsSerializer.ToJson(new SlateDbSettings
        {
            MinFilterKeys = 16,
            CompressionCodec = CompressionCodec.Lz4,
        });

        var worker = JsonNode.Parse(json)!["compactor_options"]!["worker"]!;

        worker["min_filter_keys"]!.GetValue<uint>().Should().Be(16);
        worker["compression_codec"]!.GetValue<string>().Should().Be("Lz4");
    }

    [Fact]
    public void LegacyJsonSettings_AlignsCompactorWorkerWithWriter()
    {
        const string legacyJson = """
            {
              "min_filter_keys": 16,
              "compression_codec": "Lz4"
            }
            """;

        var json = SlateDbSettingsSerializer.NormalizeForNative(
            legacyJson,
            out _);
        var worker = JsonNode.Parse(json)!["compactor_options"]!["worker"]!;

        worker["min_filter_keys"]!.GetValue<uint>().Should().Be(16);
        worker["compression_codec"]!.GetValue<string>().Should().Be("Lz4");
    }

    [Theory]
    [InlineData("""{"compactor_options":null}""", false)]
    [InlineData("""{"compactor_options":{"worker":null}}""", true)]
    public void LegacyJsonSettings_PreservesDisabledCompactorWorker(
        string legacyJson,
        bool hasCompactorObject)
    {
        var json = SlateDbSettingsSerializer.NormalizeForNative(
            legacyJson,
            out _);
        var root = JsonNode.Parse(json)!.AsObject();

        if (hasCompactorObject)
        {
            root["compactor_options"]!.AsObject().ContainsKey("worker")
                .Should().BeTrue();
            root["compactor_options"]!["worker"].Should().BeNull();
        }
        else
        {
            root.ContainsKey("compactor_options").Should().BeTrue();
            root["compactor_options"].Should().BeNull();
        }
    }

    [Fact]
    public void Builder_WithV010JsonSettings_OpensDb()
    {
        const string legacySettings = """
            {
              "flush_interval": "100ms",
              "wal_enabled": true,
              "manifest_poll_interval": "100ms",
              "manifest_update_timeout": "300s",
              "min_filter_keys": 1000,
              "filter_bits_per_key": 10,
              "l0_sst_size_bytes": 67108864,
              "l0_max_ssts": 8,
              "max_unflushed_bytes": 536870912,
              "compactor_options": {
                "poll_interval": "5s",
                "manifest_update_timeout": "300s",
                "max_sst_size": 268435456,
                "max_concurrent_compactions": 4,
                "scheduler_options": {
                  "min_compaction_sources": "4",
                  "max_compaction_sources": "8",
                  "include_size_threshold": "4"
                }
              },
              "compression_codec": "Lz4",
              "object_store_cache_options": {
                "root_folder": null,
                "max_cache_size_bytes": 17179869184,
                "part_size_bytes": 4194304,
                "cache_puts": false,
                "preload_disk_cache_on_startup": null,
                "scan_interval": "3600s"
              },
              "garbage_collector_options": null,
              "default_ttl": null
            }
            """;
        var url = $"file:///{_fixture.TempDir}/builder_v010_json_settings";

        using var db = SlateDb.Builder("test", url)
            .WithSettings(legacySettings)
            .Build();

        db.Put("upgraded", "settings");
        db.GetString("upgraded").Should().Be("settings");
    }
}
