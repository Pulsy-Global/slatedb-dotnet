namespace Pulsy.SlateDB.Metrics;

/// <summary>
/// A monotonically increasing counter value.
/// </summary>
public sealed record SlateDbCounterMetricValue(ulong Value)
    : SlateDbMetricValue(SlateDbMetricKind.Counter);
