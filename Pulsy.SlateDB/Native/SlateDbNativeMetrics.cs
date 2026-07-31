using Pulsy.SlateDB.Metrics;
using NativeDefaultMetricsRecorder = uniffi.slatedb.DefaultMetricsRecorder;
using NativeMetricLabel = uniffi.slatedb.MetricLabel;
using NativeMetricValue = uniffi.slatedb.MetricValue;
using NativeMetricsRecorder = uniffi.slatedb.MetricsRecorder;

namespace Pulsy.SlateDB.Native;

internal sealed class SlateDbNativeMetrics : IDisposable
{
    private readonly NativeDefaultMetricsRecorder _recorder = new();
    private readonly NativeMetricsRecorder _adapter;
    private bool _disposed;

    internal SlateDbNativeMetrics()
    {
        _adapter = new DefaultMetricsRecorderAdapter(_recorder);
    }

    internal NativeMetricsRecorder Adapter => _adapter;

    internal IReadOnlyList<SlateDbMetric> Snapshot()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return ConvertMetrics(SlateDbUniffi.Call(_recorder.Snapshot));
    }

    internal IReadOnlyList<SlateDbMetric> ByName(string name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return ConvertMetrics(SlateDbUniffi.Call(() => _recorder.MetricsByName(name)));
    }

    internal SlateDbMetric? ByNameAndLabels(
        string name,
        IReadOnlyCollection<SlateDbMetricLabel> labels)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var nativeLabels = labels
            .Select(static label => new NativeMetricLabel(label.Key, label.Value))
            .ToArray();
        var metric = SlateDbUniffi.Call(
            () => _recorder.MetricByNameAndLabels(name, nativeLabels));
        return metric is null ? null : ConvertMetric(metric);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _recorder.Dispose();
    }

    private static IReadOnlyList<SlateDbMetric> ConvertMetrics(
        IEnumerable<uniffi.slatedb.Metric> metrics) =>
        Array.AsReadOnly(metrics.Select(ConvertMetric).ToArray());

    private static SlateDbMetric ConvertMetric(uniffi.slatedb.Metric metric) =>
        new(
            metric.Name,
            Array.AsReadOnly(
                metric.Labels
                    .Select(static label => new SlateDbMetricLabel(label.Key, label.Value))
                    .ToArray()),
            metric.Description,
            ConvertValue(metric.Value));

    private static SlateDbMetricValue ConvertValue(NativeMetricValue value) =>
        value switch
        {
            NativeMetricValue.Counter counter =>
                new SlateDbCounterMetricValue(counter.V1),
            NativeMetricValue.Gauge gauge =>
                new SlateDbGaugeMetricValue(gauge.V1),
            NativeMetricValue.UpDownCounter counter =>
                new SlateDbUpDownCounterMetricValue(counter.V1),
            NativeMetricValue.Histogram histogram =>
                new SlateDbHistogramMetricValue(
                    new SlateDbHistogramValue(
                        histogram.V1.Count,
                        histogram.V1.Sum,
                        histogram.V1.Min,
                        histogram.V1.Max,
                        Array.AsReadOnly((double[])histogram.V1.Boundaries.Clone()),
                        Array.AsReadOnly((ulong[])histogram.V1.BucketCounts.Clone()))),
            _ => throw new SlateDbException(
                $"Unsupported native metric value type '{value.GetType().Name}'.")
        };
}
