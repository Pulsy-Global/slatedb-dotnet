namespace Pulsy.SlateDB.Options;

// Matches the SlateDB v0.15 settings JSON. Null properties are omitted by the serializer.
internal sealed class SettingsDto
{
    public TimeSpan? FlushInterval { get; init; }
    public bool? WalEnabled { get; init; }
    public TimeSpan? ManifestPollInterval { get; init; }
    public TimeSpan? ManifestUpdateTimeout { get; init; }
    public uint? MinFilterKeys { get; init; }
    public ulong? L0SstSizeBytes { get; init; }
    public ulong? MaxWalFlushesBeforeL0Flush { get; init; }
    public ulong? L0MaxSsts { get; init; }
    public ulong? L0MaxSstsPerKey { get; init; }
    public ulong? L0FlushParallelism { get; init; }
    public ulong? MaxUnflushedBytes { get; init; }
    public CompactorDto? CompactorOptions { get; init; }
    public string? CompressionCodec { get; init; }
    public CacheDto? ObjectStoreCacheOptions { get; init; }
    public GcDto? GarbageCollectorOptions { get; init; }
    public string? MetricLevel { get; init; }
    public ulong? DefaultTtl { get; init; }
    public uint? ObjectStoreMaxRetries { get; init; }
}
