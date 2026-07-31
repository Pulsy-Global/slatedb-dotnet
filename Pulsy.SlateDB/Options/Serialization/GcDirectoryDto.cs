namespace Pulsy.SlateDB.Options;

internal sealed class GcDirectoryDto
{
    public TimeSpan? Interval { get; init; }
    public TimeSpan? MinAge { get; init; }
    public bool? DryRun { get; init; }
}
