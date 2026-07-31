namespace Pulsy.SlateDB.Metrics;

/// <summary>
/// A gauge value.
/// </summary>
public sealed record SlateDbGaugeMetricValue(long Value)
    : SlateDbMetricValue(SlateDbMetricKind.Gauge);
