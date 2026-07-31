namespace Pulsy.SlateDB.Options;

public record CompactorOptions
{
    public TimeSpan? PollInterval { get; init; }
    public TimeSpan? ManifestUpdateTimeout { get; init; }

    // Kept at its pre-v0.15 location in the .NET API and mapped to WorkerOptions.
    public ulong? MaxSstSize { get; init; }
    public ulong? MaxConcurrentCompactions { get; init; }
    public bool? EnableTrivialMove { get; init; }
    public CompactionSchedulerOptions? SchedulerOptions { get; init; }
    public CompactionWorkerOptions? WorkerOptions { get; init; }
    public MetricLevel? MetricLevel { get; init; }
    public TimeSpan? CommitCompactedInterval { get; init; }
    public TimeSpan? WorkerHeartbeatTimeout { get; init; }
    public uint? ObjectStoreMaxRetries { get; init; }
}
