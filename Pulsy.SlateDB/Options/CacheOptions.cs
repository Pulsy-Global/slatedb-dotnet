namespace Pulsy.SlateDB.Options;

public record CacheOptions
{
    public string? RootFolder { get; init; }
    public ulong? MaxCacheSizeBytes { get; init; }
    public ulong? PartSizeBytes { get; init; }
    public bool? CachePuts { get; init; }
    public PreloadLevel? PreloadDiskCacheOnStartup { get; init; }
    public TimeSpan? ScanInterval { get; init; }
    public ulong? MaxOpenFileHandles { get; init; }
}
