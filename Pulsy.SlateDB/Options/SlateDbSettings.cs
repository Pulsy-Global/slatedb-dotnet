namespace Pulsy.SlateDB.Options;

public record SlateDbSettings
{
    public TimeSpan? FlushInterval { get; init; }
    public bool? WalEnabled { get; init; }
    public TimeSpan? ManifestPollInterval { get; init; }
    public TimeSpan? ManifestUpdateTimeout { get; init; }
    public uint? MinFilterKeys { get; init; }
    public uint? FilterBitsPerKey { get; init; }
    public ulong? L0SstSizeBytes { get; init; }
    public ulong? MaxWalFlushesBeforeL0Flush { get; init; }
    public ulong? L0MaxSsts { get; init; }
    public ulong? L0MaxSstsPerKey { get; init; }
    public ulong? L0FlushParallelism { get; init; }
    public ulong? MaxUnflushedBytes { get; init; }
    public CompactorOptions? CompactorOptions { get; init; }
    public CompressionCodec? CompressionCodec { get; init; }
    public CacheOptions? CacheOptions { get; init; }
    public GarbageCollectorOptions? GarbageCollectorOptions { get; init; }
    public MetricLevel? MetricLevel { get; init; }
    public ulong? DefaultTtlMs { get; init; }
    public uint? ObjectStoreMaxRetries { get; init; }
}
