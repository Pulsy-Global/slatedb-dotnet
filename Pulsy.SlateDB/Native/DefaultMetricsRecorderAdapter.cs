using NativeDefaultMetricsRecorder = uniffi.slatedb.DefaultMetricsRecorder;
using NativeMetricLabel = uniffi.slatedb.MetricLabel;
using NativeMetricsRecorder = uniffi.slatedb.MetricsRecorder;

namespace Pulsy.SlateDB.Native;

internal sealed class DefaultMetricsRecorderAdapter(NativeDefaultMetricsRecorder recorder)
    : NativeMetricsRecorder
{
    public uniffi.slatedb.Counter RegisterCounter(
        string name,
        string? description,
        NativeMetricLabel[] labels) =>
        recorder.RegisterCounter(name, description, labels);

    public uniffi.slatedb.Gauge RegisterGauge(
        string name,
        string? description,
        NativeMetricLabel[] labels) =>
        recorder.RegisterGauge(name, description, labels);

    public uniffi.slatedb.UpDownCounter RegisterUpDownCounter(
        string name,
        string? description,
        NativeMetricLabel[] labels) =>
        recorder.RegisterUpDownCounter(name, description, labels);

    public uniffi.slatedb.Histogram RegisterHistogram(
        string name,
        string? description,
        NativeMetricLabel[] labels,
        double[] boundaries) =>
        recorder.RegisterHistogram(name, description, labels, boundaries);
}
