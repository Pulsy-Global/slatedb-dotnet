namespace Pulsy.SlateDB.Metrics;

/// <summary>
/// An up/down counter value.
/// </summary>
public sealed record SlateDbUpDownCounterMetricValue(long Value)
    : SlateDbMetricValue(SlateDbMetricKind.UpDownCounter);
