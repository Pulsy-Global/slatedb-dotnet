using Pulsy.SlateDB.Native;
using Pulsy.SlateDB.Metrics;
using Pulsy.SlateDB.Options;
using NativeDb = uniffi.slatedb.Db;
using NativeFlushOptions = uniffi.slatedb.FlushOptions;
using NativeFlushType = uniffi.slatedb.FlushType;
using NativeSettings = uniffi.slatedb.Settings;

namespace Pulsy.SlateDB;

public sealed partial class SlateDb : ISlateDbReadable
{
    private NativeDb? _db;
    private SlateDbNativeMetrics? _metrics;
    private bool _disposed;

    private SlateDb(NativeDb db, SlateDbNativeMetrics metrics)
    {
        _db = db;
        _metrics = metrics;
    }

    public static void LoadLibrary()
    {
        NativeLibraryLoader.Initialize();
    }

    public static void LoadLibrary(string absolutePath)
    {
        NativeLibraryLoader.Initialize(absolutePath);
    }

    public static void InitLogging(LogLevel level)
    {
        NativeLibraryLoader.Initialize();
        SlateDbUniffi.Call(() => uniffi.slatedb.SlatedbMethods.InitLogging(
            SlateDbUniffi.ToNative(level),
            null));
    }

    public static string SettingsDefault()
    {
        NativeLibraryLoader.Initialize();
        using var settings = SlateDbUniffi.Call(NativeSettings.Default);
        return SlateDbUniffi.Call(settings.ToJsonString);
    }

    public static string SettingsFromFile(string path)
    {
        NativeLibraryLoader.Initialize();
        using var settings = SlateDbUniffi.Call(() => NativeSettings.FromFile(path));
        return SlateDbUniffi.Call(settings.ToJsonString);
    }

    public static string SettingsFromEnv(string prefix)
    {
        NativeLibraryLoader.Initialize();
        using var settings = SlateDbUniffi.Call(() => NativeSettings.FromEnv(prefix));
        return SlateDbUniffi.Call(settings.ToJsonString);
    }

    public static string SettingsLoad()
    {
        NativeLibraryLoader.Initialize();
        using var settings = SlateDbUniffi.Call(NativeSettings.Load);
        return SlateDbUniffi.Call(settings.ToJsonString);
    }

    public static SlateDb Open(string path, string? url = null, string? envFile = null)
    {
        using var builder = Builder(path, url, envFile);
        return builder.Build();
    }

    public static SlateDbReader OpenReader(
        string path,
        string url,
        string? envFile,
        string? checkpointId,
        ReaderOptions? options = null)
    {
        NativeLibraryLoader.Initialize();
        return SlateDbReader.Open(path, url, envFile, checkpointId, options);
    }

    public static SlateDbBuilder Builder(string path, string? url = null, string? envFile = null)
    {
        NativeLibraryLoader.Initialize();
        return new SlateDbBuilder(path, url, envFile);
    }

    public static SlateDbBuilder Builder(string path, ObjectStoreConfig objectStore)
    {
        NativeLibraryLoader.Initialize();
        return new SlateDbBuilder(path, objectStore);
    }

    public static SlateDbWriteBatch NewWriteBatch()
    {
        NativeLibraryLoader.Initialize();
        return new SlateDbWriteBatch();
    }

    public void Flush()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        SlateDbUniffi.Wait(() => Db.Flush());
    }

    /// <summary>
    /// Flushes the active memtable to L0 and advances the WAL replay boundary.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="Flush"/>, this also makes older WAL files eligible for
    /// garbage collection when WAL is enabled.
    /// </remarks>
    public void FlushMemTable()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        SlateDbUniffi.Wait(() => Db.FlushWithOptions(
            new NativeFlushOptions(NativeFlushType.MemTable)));
    }

    /// <summary>
    /// Returns a point-in-time snapshot of every registered SlateDB metric.
    /// </summary>
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

        var db = _db;
        _db = null;
        try
        {
            if (db is not null)
                SlateDbUniffi.Wait(() => db.Shutdown());
        }
        finally
        {
            db?.Dispose();
            _metrics?.Dispose();
            _metrics = null;
        }
    }

    internal static SlateDb FromNative(NativeDb db, SlateDbNativeMetrics metrics) =>
        new(db, metrics);

    private NativeDb Db =>
        _db ?? throw new ObjectDisposedException(nameof(SlateDb));

    private SlateDbNativeMetrics MetricsRecorder =>
        _metrics ?? throw new ObjectDisposedException(nameof(SlateDb));
}
