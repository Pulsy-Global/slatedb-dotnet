namespace Pulsy.SlateDB.Metrics;

/// <summary>
/// A key-value label attached to a SlateDB metric.
/// </summary>
public sealed record SlateDbMetricLabel(string Key, string Value);
