namespace Pulsy.SlateDB.Options;

internal sealed class CacheDto
{
    public string? RootFolder { get; init; }
    public ulong? MaxCacheSizeBytes { get; init; }
    public ulong? PartSizeBytes { get; init; }
    public bool? CacheOnFlush { get; init; }
    public bool? CacheOnCompaction { get; init; }
    public string? PreloadDiskCacheOnStartup { get; init; }
    public TimeSpan? ScanInterval { get; init; }
    public ulong? MaxOpenFileHandles { get; init; }
}
