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
    public ulong? L0MaxSsts { get; init; }
    public ulong? MaxUnflushedBytes { get; init; }
    public CompactorOptions? CompactorOptions { get; init; }
    public CompressionCodec? CompressionCodec { get; init; }
    public CacheOptions? CacheOptions { get; init; }
    public GarbageCollectorOptions? GarbageCollectorOptions { get; init; }
    public ulong? DefaultTtlMs { get; init; }
}

public record CompactorOptions
{
    public TimeSpan? PollInterval { get; init; }
    public TimeSpan? ManifestUpdateTimeout { get; init; }
    public ulong? MaxSstSize { get; init; }
    public ulong? MaxConcurrentCompactions { get; init; }
    public CompactionSchedulerOptions? SchedulerOptions { get; init; }
}

public record CompactionSchedulerOptions
{
    public ulong? MinCompactionSources { get; init; }
    public ulong? MaxCompactionSources { get; init; }
    public float? IncludeSizeThreshold { get; init; }
}

public record CacheOptions
{
    public string? RootFolder { get; init; }
    public ulong? MaxCacheSizeBytes { get; init; }
    public ulong? PartSizeBytes { get; init; }
    public bool? CachePuts { get; init; }
    public PreloadLevel? PreloadDiskCacheOnStartup { get; init; }
    public TimeSpan? ScanInterval { get; init; }
}

public record GarbageCollectorOptions
{
    public GcDirectoryOptions? ManifestOptions { get; init; }
    public GcDirectoryOptions? WalOptions { get; init; }
    public GcDirectoryOptions? CompactedOptions { get; init; }
    public GcDirectoryOptions? CompactionsOptions { get; init; }
}

public record GcDirectoryOptions
{
    public TimeSpan? Interval { get; init; }
    public TimeSpan? MinAge { get; init; }
}

public enum CompressionCodec { Snappy, Zlib, Lz4, Zstd }

public enum PreloadLevel { L0Sst, AllSst }
