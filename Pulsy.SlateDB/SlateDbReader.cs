using Pulsy.SlateDB.Metrics;
using Pulsy.SlateDB.Native;
using Pulsy.SlateDB.Options;
using NativeDbReader = uniffi.slatedb.DbReader;
using NativeDbReaderBuilder = uniffi.slatedb.DbReaderBuilder;
using NativeReaderMode = uniffi.slatedb.ReaderMode;

namespace Pulsy.SlateDB;

public sealed class SlateDbReader : ISlateDbReadable
{
    private NativeDbReader? _reader;
    private SlateDbNativeMetrics? _metrics;
    private bool _disposed;

    private SlateDbReader(NativeDbReader reader, SlateDbNativeMetrics metrics)
    {
        _reader = reader;
        _metrics = metrics;
    }

    internal static SlateDbReader Open(
        string path,
        string url,
        string? envFile,
        string? checkpointId,
        ReaderOptions? options)
    {
        using var location = SlateDbUniffi.ResolveObjectStore(path, url, envFile);
        using var builder = SlateDbUniffi.Call(() =>
            new NativeDbReaderBuilder(location.Path, location.ObjectStore));
        var metrics = new SlateDbNativeMetrics();

        try
        {
            SlateDbUniffi.Call(() => builder.WithMetricsRecorder(metrics.Adapter));

            if (checkpointId is not null)
            {
                SlateDbUniffi.Call(() =>
                    builder.WithReaderMode(new NativeReaderMode.Checkpoint(checkpointId)));
            }

            if (options is not null)
                SlateDbUniffi.Call(() => builder.WithOptions(SlateDbUniffi.ToNative(options)));

            var reader = SlateDbUniffi.Wait(() => builder.Build());
            return new SlateDbReader(reader, metrics);
        }
        catch
        {
            metrics.Dispose();
            throw;
        }
    }

    public byte[]? Get(string key) => Get(SlateDbConvert.ToBytes(key));
    public byte[]? Get(string key, ReadOptions options) => Get(SlateDbConvert.ToBytes(key), options);

    public T? Get<T>(string key) where T : struct
    {
        var bytes = Get(SlateDbConvert.ToBytes(key));
        return bytes is null ? null : SlateDbConvert.FromBytes<T>(bytes);
    }

    public string? GetString(string key)
    {
        var bytes = Get(SlateDbConvert.ToBytes(key));
        return bytes is null ? null : SlateDbConvert.FromBytes<string>(bytes);
    }

    public byte[]? Get(byte[] key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var value = SlateDbUniffi.Wait(() => Reader.GetKeyValue(key));
        return SlateDbUniffi.ToValueOrNull(value);
    }

    public byte[]? Get(byte[] key, ReadOptions options)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var value = SlateDbUniffi.Wait(() => Reader.GetKeyValueWithOptions(
            key,
            SlateDbUniffi.ToNative(options)));
        return SlateDbUniffi.ToValueOrNull(value);
    }

    public SlateDbScanIterator Scan(string? startKey, string? endKey) =>
        Scan(startKey is null ? null : SlateDbConvert.ToBytes(startKey),
             endKey is null ? null : SlateDbConvert.ToBytes(endKey));

    public SlateDbScanIterator Scan(string? startKey, string? endKey, ScanOptions options) =>
        Scan(startKey is null ? null : SlateDbConvert.ToBytes(startKey),
             endKey is null ? null : SlateDbConvert.ToBytes(endKey), options);

    public SlateDbScanIterator ScanPrefix(string prefix) =>
        ScanPrefix(SlateDbConvert.ToBytes(prefix));

    public SlateDbScanIterator ScanPrefix(string prefix, ScanOptions options) =>
        ScanPrefix(SlateDbConvert.ToBytes(prefix), options);

    public SlateDbScanIterator Scan(byte[]? startKey, byte[]? endKey)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var iterator = SlateDbUniffi.Wait(() => Reader.Scan(
            SlateDbUniffi.ToKeyRange(startKey, endKey)));
        return new SlateDbScanIterator(iterator);
    }

    public SlateDbScanIterator Scan(byte[]? startKey, byte[]? endKey, ScanOptions options)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var iterator = SlateDbUniffi.Wait(() => Reader.ScanWithOptions(
            SlateDbUniffi.ToKeyRange(startKey, endKey),
            SlateDbUniffi.ToNative(options)));
        return new SlateDbScanIterator(iterator);
    }

    public SlateDbScanIterator ScanPrefix(byte[] prefix)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var iterator = SlateDbUniffi.Wait(() => Reader.ScanPrefix(
            prefix,
            SlateDbUniffi.UnboundedKeyRange()));
        return new SlateDbScanIterator(iterator);
    }

    public SlateDbScanIterator ScanPrefix(byte[] prefix, ScanOptions options)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var iterator = SlateDbUniffi.Wait(() => Reader.ScanPrefixWithOptions(
            prefix,
            SlateDbUniffi.UnboundedKeyRange(),
            SlateDbUniffi.ToNative(options)));
        return new SlateDbScanIterator(iterator);
    }

    public IReadOnlyList<SlateDbMetric> GetMetrics()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return MetricsRecorder.Snapshot();
    }

    /// <summary>
    /// Returns every metric with the requested name.
    /// </summary>
    public IReadOnlyList<SlateDbMetric> GetMetrics(string name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return MetricsRecorder.ByName(name);
    }

    /// <summary>
    /// Returns the metric matching the name and exact label set.
    /// </summary>
    public SlateDbMetric? GetMetric(
        string name,
        params SlateDbMetricLabel[] labels)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(labels);
        return MetricsRecorder.ByNameAndLabels(name, labels);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        var reader = _reader;
        _reader = null;
        try
        {
            if (reader is not null)
                SlateDbUniffi.Wait(() => reader.Shutdown());
        }
        finally
        {
            reader?.Dispose();
            _metrics?.Dispose();
            _metrics = null;
        }
    }

    private NativeDbReader Reader =>
        _reader ?? throw new ObjectDisposedException(nameof(SlateDbReader));

    private SlateDbNativeMetrics MetricsRecorder =>
        _metrics ?? throw new ObjectDisposedException(nameof(SlateDbReader));
}
