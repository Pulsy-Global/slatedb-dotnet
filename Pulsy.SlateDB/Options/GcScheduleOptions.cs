namespace Pulsy.SlateDB.Options;

public record GcScheduleOptions
{
    public TimeSpan? Interval { get; init; }
}
