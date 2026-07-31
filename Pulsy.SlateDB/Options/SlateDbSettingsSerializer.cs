using System.Globalization;
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
        Converters = { new DurationJsonConverter() },
    };

    public static string ToJson(SlateDbSettings settings)
    {
        var defaultsJson = GetDefaultsJson();
        var baseNode = JsonNode.Parse(defaultsJson)!.AsObject();

        var overrideJson = SerializeOverrides(settings);
        var overrideNode = JsonNode.Parse(overrideJson)?.AsObject();

        if (overrideNode is not null)
            MergeObjects(baseNode, overrideNode);

        ApplyTypedCompactorWorkerFallbacks(baseNode, settings);
        return baseNode.ToJsonString();
    }

    public static string NormalizeForNative(string settingsJson, out uint? filterBitsPerKey)
    {
        filterBitsPerKey = null;

        JsonObject? root;
        try
        {
            root = JsonNode.Parse(settingsJson) as JsonObject;
        }
        catch (JsonException)
        {
            // Let the native settings parser return the public SlateDbException.
            return settingsJson;
        }

        if (root is null)
            return settingsJson;

        if (root.Remove("filter_bits_per_key", out var filterBitsNode) &&
            filterBitsNode is not null)
        {
            if (filterBitsNode is not JsonValue filterBitsValue ||
                !filterBitsValue.TryGetValue<uint>(out var filterBits))
            {
                throw new SlateDbException(
                    1,
                    "filter_bits_per_key must be an unsigned 32-bit integer");
            }

            filterBitsPerKey = filterBits;
        }

        if (root["compactor_options"] is JsonObject compactor &&
            compactor.Remove("max_sst_size", out var maxSstSize))
        {
            var worker = compactor["worker"] as JsonObject;
            if (worker is null && !compactor.ContainsKey("worker"))
            {
                worker = new JsonObject();
                compactor["worker"] = worker;
            }

            if (worker is not null)
                worker["max_sst_size"] = maxSstSize;
        }

        if (root["object_store_cache_options"] is JsonObject cache &&
            cache.Remove("cache_puts", out var cachePuts))
        {
            cache["cache_on_flush"] = cachePuts?.DeepClone();
            cache["cache_on_compaction"] = cachePuts;
        }

        var explicitWorker = root["compactor_options"]?["worker"] as JsonObject;
        var workerHasMinFilterKeys = explicitWorker?.ContainsKey("min_filter_keys") == true;
        var workerHasCompressionCodec = explicitWorker?.ContainsKey("compression_codec") == true;

        var defaults = JsonNode.Parse(GetDefaultsJson())!.AsObject();
        MergeObjects(defaults, root);
        ApplyJsonCompactorWorkerFallbacks(
            defaults,
            root.ContainsKey("min_filter_keys") && !workerHasMinFilterKeys,
            root.ContainsKey("compression_codec") && !workerHasCompressionCodec);
        return defaults.ToJsonString();
    }

    private static string GetDefaultsJson()
    {
        return SlateDbUniffi.SettingsDefaultJson();
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
            L0SstSizeBytes = s.L0SstSizeBytes,
            MaxWalFlushesBeforeL0Flush = s.MaxWalFlushesBeforeL0Flush,
            L0MaxSsts = s.L0MaxSsts,
            L0MaxSstsPerKey = s.L0MaxSstsPerKey,
            L0FlushParallelism = s.L0FlushParallelism,
            MaxUnflushedBytes = s.MaxUnflushedBytes,
            CompactorOptions = s.CompactorOptions is { } co ? ToDto(co) : null,
            CompressionCodec = s.CompressionCodec?.ToString(),
            ObjectStoreCacheOptions = s.CacheOptions is { } cache ? ToCacheDto(cache) : null,
            GarbageCollectorOptions = s.GarbageCollectorOptions is { } gc ? ToDto(gc) : null,
            MetricLevel = s.MetricLevel?.ToString(),
            DefaultTtl = s.DefaultTtlMs,
            ObjectStoreMaxRetries = s.ObjectStoreMaxRetries,
        };

        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    private static CompactorDto ToDto(CompactorOptions co) => new()
    {
        PollInterval = co.PollInterval,
        ManifestUpdateTimeout = co.ManifestUpdateTimeout,
        MaxConcurrentCompactions = co.MaxConcurrentCompactions,
        EnableTrivialMove = co.EnableTrivialMove,
        SchedulerOptions = co.SchedulerOptions is { } so ? ToDto(so) : null,
        Worker = co.WorkerOptions is not null || co.MaxSstSize is not null
            ? ToDto(co.WorkerOptions, co.MaxSstSize)
            : null,
        MetricLevel = co.MetricLevel?.ToString(),
        CommitCompactedInterval = co.CommitCompactedInterval,
        WorkerHeartbeatTimeout = co.WorkerHeartbeatTimeout,
        ObjectStoreMaxRetries = co.ObjectStoreMaxRetries,
    };

    private static CompactionWorkerDto ToDto(
        CompactionWorkerOptions? worker,
        ulong? movedMaxSstSize) => new()
        {
            MaxConcurrentCompactions = worker?.MaxConcurrentCompactions,
            CompactionsPollInterval = worker?.CompactionsPollInterval,
            HeartbeatInterval = worker?.HeartbeatInterval,
            MaxSstSize = worker?.MaxSstSize ?? movedMaxSstSize,
            MaxFetchTasks = worker?.MaxFetchTasks,
            BytesToFetch = worker?.BytesToFetch,
            MaxSubcompactions = worker?.MaxSubcompactions,
            MinFilterKeys = worker?.MinFilterKeys,
            CompressionCodec = worker?.CompressionCodec?.ToString(),
            MetricLevel = worker?.MetricLevel?.ToString(),
        };

    private static Dictionary<string, string>? ToDto(CompactionSchedulerOptions so)
    {
        var dict = new Dictionary<string, string>();
        if (so.MinCompactionSources is { } min)
            dict["min_compaction_sources"] = min.ToString(CultureInfo.InvariantCulture);
        if (so.MaxCompactionSources is { } max)
            dict["max_compaction_sources"] = max.ToString(CultureInfo.InvariantCulture);
        if (so.IncludeSizeThreshold is { } threshold)
            dict["include_size_threshold"] = threshold.ToString("G", CultureInfo.InvariantCulture);
        return dict.Count > 0 ? dict : null;
    }

    private static CacheDto ToCacheDto(CacheOptions co) => new()
    {
        RootFolder = co.RootFolder,
        MaxCacheSizeBytes = co.MaxCacheSizeBytes,
        PartSizeBytes = co.PartSizeBytes,
        CacheOnFlush = co.CachePuts,
        CacheOnCompaction = co.CachePuts,
        PreloadDiskCacheOnStartup = co.PreloadDiskCacheOnStartup?.ToString(),
        ScanInterval = co.ScanInterval,
        MaxOpenFileHandles = co.MaxOpenFileHandles,
    };

    private static GcDto ToDto(GarbageCollectorOptions gc) => new()
    {
        ManifestOptions = gc.ManifestOptions is { } m ? ToDto(m) : null,
        WalOptions = gc.WalOptions is { } w ? ToDto(w) : null,
        WalFenceOptions = gc.WalFenceOptions is { } wf ? ToDto(wf) : null,
        CompactedOptions = gc.CompactedOptions is { } c ? ToDto(c) : null,
        CompactionsOptions = gc.CompactionsOptions is { } cs ? ToDto(cs) : null,
        DetachOptions = gc.DetachOptions is { } d ? ToDto(d) : null,
        MetricLevel = gc.MetricLevel?.ToString(),
        BoundaryFilesEnabled = gc.BoundaryFilesEnabled,
        ObjectStoreMaxRetries = gc.ObjectStoreMaxRetries,
    };

    private static GcDirectoryDto ToDto(GcDirectoryOptions d) => new()
    {
        Interval = d.Interval,
        MinAge = d.MinAge,
        DryRun = d.DryRun,
    };

    private static GcScheduleDto ToDto(GcScheduleOptions schedule) => new()
    {
        Interval = schedule.Interval,
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

    private static void ApplyTypedCompactorWorkerFallbacks(
        JsonObject root,
        SlateDbSettings settings)
    {
        var workerOptions = settings.CompactorOptions?.WorkerOptions;
        var minFilterKeys = workerOptions?.MinFilterKeys ?? settings.MinFilterKeys;
        var compressionCodec = workerOptions?.CompressionCodec ?? settings.CompressionCodec;
        var worker = GetCompactorWorker(root);

        if (minFilterKeys is { } min)
            worker["min_filter_keys"] = min;

        if (compressionCodec is { } compression)
            worker["compression_codec"] = compression.ToString();
    }

    private static void ApplyJsonCompactorWorkerFallbacks(
        JsonObject root,
        bool copyMinFilterKeys,
        bool copyCompressionCodec)
    {
        if (!copyMinFilterKeys && !copyCompressionCodec)
            return;

        if (root["compactor_options"] is not JsonObject compactor ||
            compactor["worker"] is not JsonObject worker)
        {
            return;
        }

        if (copyMinFilterKeys)
            worker["min_filter_keys"] = root["min_filter_keys"]?.DeepClone();

        if (copyCompressionCodec)
            worker["compression_codec"] = root["compression_codec"]?.DeepClone();
    }

    private static JsonObject GetCompactorWorker(JsonObject root)
    {
        if (root["compactor_options"] is not JsonObject compactor)
        {
            compactor = new JsonObject();
            root["compactor_options"] = compactor;
        }

        if (compactor["worker"] is not JsonObject worker)
        {
            worker = new JsonObject();
            compactor["worker"] = worker;
        }

        return worker;
    }
}
