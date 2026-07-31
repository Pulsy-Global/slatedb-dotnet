namespace Pulsy.SlateDB.Options;

public record CompactionWorkerOptions
{
    public ulong? MaxConcurrentCompactions { get; init; }
    public TimeSpan? CompactionsPollInterval { get; init; }
    public TimeSpan? HeartbeatInterval { get; init; }
    public ulong? MaxSstSize { get; init; }
    public ulong? MaxFetchTasks { get; init; }
    public ulong? BytesToFetch { get; init; }
    public ulong? MaxSubcompactions { get; init; }
    public uint? MinFilterKeys { get; init; }
    public CompressionCodec? CompressionCodec { get; init; }
    public MetricLevel? MetricLevel { get; init; }
}
