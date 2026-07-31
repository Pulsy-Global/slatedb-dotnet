namespace Pulsy.SlateDB.Options;

public record GarbageCollectorOptions
{
    public GcDirectoryOptions? ManifestOptions { get; init; }
    public GcDirectoryOptions? WalOptions { get; init; }
    public GcDirectoryOptions? WalFenceOptions { get; init; }
    public GcDirectoryOptions? CompactedOptions { get; init; }
    public GcDirectoryOptions? CompactionsOptions { get; init; }
    public GcScheduleOptions? DetachOptions { get; init; }
    public MetricLevel? MetricLevel { get; init; }
    public bool? BoundaryFilesEnabled { get; init; }
    public uint? ObjectStoreMaxRetries { get; init; }
}
