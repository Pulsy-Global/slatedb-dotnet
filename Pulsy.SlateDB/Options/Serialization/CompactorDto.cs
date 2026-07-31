namespace Pulsy.SlateDB.Options;

internal sealed class CompactorDto
{
    public TimeSpan? PollInterval { get; init; }
    public TimeSpan? ManifestUpdateTimeout { get; init; }
    public ulong? MaxConcurrentCompactions { get; init; }
    public bool? EnableTrivialMove { get; init; }
    public Dictionary<string, string>? SchedulerOptions { get; init; }
    public CompactionWorkerDto? Worker { get; init; }
    public string? MetricLevel { get; init; }
    public TimeSpan? CommitCompactedInterval { get; init; }
    public TimeSpan? WorkerHeartbeatTimeout { get; init; }
    public uint? ObjectStoreMaxRetries { get; init; }
}
