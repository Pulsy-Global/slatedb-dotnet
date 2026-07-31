namespace Pulsy.SlateDB.Options;

public record GcDirectoryOptions
{
    public TimeSpan? Interval { get; init; }
    public TimeSpan? MinAge { get; init; }
    public bool? DryRun { get; init; }
}
