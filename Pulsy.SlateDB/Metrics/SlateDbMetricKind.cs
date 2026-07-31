namespace Pulsy.SlateDB.Metrics;

/// <summary>
/// The instrument type of a SlateDB metric.
/// </summary>
public enum SlateDbMetricKind
{
    Counter,
    Gauge,
    UpDownCounter,
    Histogram
}
