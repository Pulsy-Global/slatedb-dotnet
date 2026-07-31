namespace Pulsy.SlateDB.Options;

internal sealed class GcDto
{
    public GcDirectoryDto? ManifestOptions { get; init; }
    public GcDirectoryDto? WalOptions { get; init; }
    public GcDirectoryDto? WalFenceOptions { get; init; }
    public GcDirectoryDto? CompactedOptions { get; init; }
    public GcDirectoryDto? CompactionsOptions { get; init; }
    public GcScheduleDto? DetachOptions { get; init; }
    public string? MetricLevel { get; init; }
    public bool? BoundaryFilesEnabled { get; init; }
    public uint? ObjectStoreMaxRetries { get; init; }
}
