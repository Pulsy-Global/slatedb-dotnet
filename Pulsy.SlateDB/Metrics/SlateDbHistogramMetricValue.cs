namespace Pulsy.SlateDB.Metrics;

/// <summary>
/// A histogram value including its summary and buckets.
/// </summary>
public sealed record SlateDbHistogramMetricValue(SlateDbHistogramValue Value)
    : SlateDbMetricValue(SlateDbMetricKind.Histogram);
