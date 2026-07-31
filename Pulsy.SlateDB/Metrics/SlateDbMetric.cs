namespace Pulsy.SlateDB.Metrics;

/// <summary>
/// A point-in-time value of a SlateDB metric.
/// </summary>
public sealed record SlateDbMetric(
    string Name,
    IReadOnlyList<SlateDbMetricLabel> Labels,
    string Description,
    SlateDbMetricValue Value);
