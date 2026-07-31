namespace Pulsy.SlateDB.Metrics;

/// <summary>
/// The typed value of a SlateDB metric.
/// </summary>
public abstract record SlateDbMetricValue(SlateDbMetricKind Kind)
;
