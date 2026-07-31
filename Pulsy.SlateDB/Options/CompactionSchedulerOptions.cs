namespace Pulsy.SlateDB.Options;

public record CompactionSchedulerOptions
{
    public ulong? MinCompactionSources { get; init; }
    public ulong? MaxCompactionSources { get; init; }
    public float? IncludeSizeThreshold { get; init; }
}
