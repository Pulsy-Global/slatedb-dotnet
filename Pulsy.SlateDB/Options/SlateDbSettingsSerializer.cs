using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Pulsy.SlateDB.Native;

namespace Pulsy.SlateDB.Options;

internal static class SlateDbSettingsSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new DurationConverter() },
    };

    public static string ToJson(SlateDbSettings settings)
    {
        var defaultsJson = GetDefaultsJson();
        var baseNode = JsonNode.Parse(defaultsJson)!.AsObject();

        var overrideJson = SerializeOverrides(settings);
        var overrideNode = JsonNode.Parse(overrideJson)?.AsObject();

        if (overrideNode is not null)
            MergeObjects(baseNode, overrideNode);

        return baseNode.ToJsonString();
    }

    private static string GetDefaultsJson()
    {
        var ptr = NativeMethods.slatedb_settings_default();
        if (ptr == nint.Zero)
            throw new SlateDbException("Failed to get default settings from native library");

        var json = Marshal.PtrToStringUTF8(ptr) ?? "";
        unsafe { NativeMemory.Free(ptr.ToPointer()); }
        return json;
    }

    private static string SerializeOverrides(SlateDbSettings s)
    {
        var dto = new SettingsDto
        {
            FlushInterval = s.FlushInterval,
            WalEnabled = s.WalEnabled,
            ManifestPollInterval = s.ManifestPollInterval,
            ManifestUpdateTimeout = s.ManifestUpdateTimeout,
            MinFilterKeys = s.MinFilterKeys,
            FilterBitsPerKey = s.FilterBitsPerKey,
            L0SstSizeBytes = s.L0SstSizeBytes,
            L0MaxSsts = s.L0MaxSsts,
            MaxUnflushedBytes = s.MaxUnflushedBytes,
            CompactorOptions = s.CompactorOptions is { } co ? ToDto(co) : null,
            CompressionCodec = s.CompressionCodec?.ToString(),
            ObjectStoreCacheOptions = s.CacheOptions is { } cache ? ToCacheDto(cache) : null,
            GarbageCollectorOptions = s.GarbageCollectorOptions is { } gc ? ToDto(gc) : null,
            DefaultTtl = s.DefaultTtlMs,
        };

        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    private static CompactorDto ToDto(CompactorOptions co) => new()
    {
        PollInterval = co.PollInterval,
        ManifestUpdateTimeout = co.ManifestUpdateTimeout,
        MaxSstSize = co.MaxSstSize,
        MaxConcurrentCompactions = co.MaxConcurrentCompactions,
        SchedulerOptions = co.SchedulerOptions is { } so ? ToDto(so) : null,
    };

    private static Dictionary<string, string> ToDto(CompactionSchedulerOptions so)
    {
        var dict = new Dictionary<string, string>();
        if (so.MinCompactionSources is { } min)
            dict["min_compaction_sources"] = min.ToString();
        if (so.MaxCompactionSources is { } max)
            dict["max_compaction_sources"] = max.ToString();
        if (so.IncludeSizeThreshold is { } threshold)
            dict["include_size_threshold"] = threshold.ToString("G");
        return dict.Count > 0 ? dict : null!;
    }

    private static CacheDto ToCacheDto(CacheOptions co) => new()
    {
        RootFolder = co.RootFolder,
        MaxCacheSizeBytes = co.MaxCacheSizeBytes,
        PartSizeBytes = co.PartSizeBytes,
        CachePuts = co.CachePuts,
        PreloadDiskCacheOnStartup = co.PreloadDiskCacheOnStartup?.ToString(),
        ScanInterval = co.ScanInterval,
    };

    private static GcDto ToDto(GarbageCollectorOptions gc) => new()
    {
        ManifestOptions = gc.ManifestOptions is { } m ? ToDto(m) : null,
        WalOptions = gc.WalOptions is { } w ? ToDto(w) : null,
        CompactedOptions = gc.CompactedOptions is { } c ? ToDto(c) : null,
        CompactionsOptions = gc.CompactionsOptions is { } cs ? ToDto(cs) : null,
    };

    private static GcDirectoryDto ToDto(GcDirectoryOptions d) => new()
    {
        Interval = d.Interval,
        MinAge = d.MinAge,
    };

    private static void MergeObjects(JsonObject target, JsonObject source)
    {
        foreach (var (key, value) in source)
        {
            if (value is JsonObject sourceObj && target[key] is JsonObject targetObj)
            {
                MergeObjects(targetObj, sourceObj);
            }
            else
            {
                target[key] = value?.DeepClone();
            }
        }
    }

    private static string FormatDuration(TimeSpan ts)
    {
        var totalMs = ts.TotalMilliseconds;
        if (totalMs % 1000 == 0)
            return $"{(long)(totalMs / 1000)}s";
        return $"{(long)totalMs}ms";
    }

    // DTO types matching slatedb-c JSON shape (nulls omitted)

    private sealed class SettingsDto
    {
        public TimeSpan? FlushInterval { get; init; }
        public bool? WalEnabled { get; init; }
        public TimeSpan? ManifestPollInterval { get; init; }
        public TimeSpan? ManifestUpdateTimeout { get; init; }
        public uint? MinFilterKeys { get; init; }
        public uint? FilterBitsPerKey { get; init; }
        public ulong? L0SstSizeBytes { get; init; }
        public ulong? L0MaxSsts { get; init; }
        public ulong? MaxUnflushedBytes { get; init; }
        public CompactorDto? CompactorOptions { get; init; }
        public string? CompressionCodec { get; init; }
        public CacheDto? ObjectStoreCacheOptions { get; init; }
        public GcDto? GarbageCollectorOptions { get; init; }
        public ulong? DefaultTtl { get; init; }
    }

    private sealed class CompactorDto
    {
        public TimeSpan? PollInterval { get; init; }
        public TimeSpan? ManifestUpdateTimeout { get; init; }
        public ulong? MaxSstSize { get; init; }
        public ulong? MaxConcurrentCompactions { get; init; }
        public Dictionary<string, string>? SchedulerOptions { get; init; }
    }

    private sealed class CacheDto
    {
        public string? RootFolder { get; init; }
        public ulong? MaxCacheSizeBytes { get; init; }
        public ulong? PartSizeBytes { get; init; }
        public bool? CachePuts { get; init; }
        public string? PreloadDiskCacheOnStartup { get; init; }
        public TimeSpan? ScanInterval { get; init; }
    }

    private sealed class GcDto
    {
        public GcDirectoryDto? ManifestOptions { get; init; }
        public GcDirectoryDto? WalOptions { get; init; }
        public GcDirectoryDto? CompactedOptions { get; init; }
        public GcDirectoryDto? CompactionsOptions { get; init; }
    }

    private sealed class GcDirectoryDto
    {
        public TimeSpan? Interval { get; init; }
        public TimeSpan? MinAge { get; init; }
    }

    private sealed class DurationConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotSupportedException();

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options) =>
            writer.WriteStringValue(FormatDuration(value));
    }
}
