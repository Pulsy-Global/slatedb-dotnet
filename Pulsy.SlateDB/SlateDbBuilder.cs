using Pulsy.SlateDB.Native;
using Pulsy.SlateDB.Options;
using NativeDb = uniffi.slatedb.Db;
using NativeDbBuilder = uniffi.slatedb.DbBuilder;
using NativeFilterPolicy = uniffi.slatedb.FilterPolicy;
using NativeObjectStore = uniffi.slatedb.ObjectStore;
using NativeSettings = uniffi.slatedb.Settings;

namespace Pulsy.SlateDB;

public sealed class SlateDbBuilder : IDisposable
{
    private NativeObjectStore? _objectStore;
    private NativeDbBuilder? _builder;
    private NativeSettings? _settings;
    private SlateDbNativeMetrics? _metrics;
    private bool _disposed;

    internal SlateDbBuilder(string path, string? url, string? envFile)
        : this(SlateDbUniffi.ResolveObjectStore(path, url, envFile))
    {
    }

    internal SlateDbBuilder(string path, ObjectStoreConfig config)
        : this(ResolveObjectStore(path, config))
    {
    }

    private SlateDbBuilder(SlateDbObjectStoreLocation location)
    {
        _objectStore = location.ObjectStore;
        try
        {
            _builder = SlateDbUniffi.Call(() => new NativeDbBuilder(location.Path, _objectStore));
            _metrics = new SlateDbNativeMetrics();
            SlateDbUniffi.Call(() => _builder.WithMetricsRecorder(_metrics.Adapter));
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public SlateDbBuilder WithSettings(SlateDbSettings settings)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ApplySettings(SlateDbSettingsSerializer.ToJson(settings));

        // v0.15 moved bloom-filter configuration from Settings onto DbBuilder.
        // Resetting to 10 preserves the old typed-settings default.
        SetFilterBitsPerKey(settings.FilterBitsPerKey ?? 10);
        return this;
    }

    public SlateDbBuilder WithSettings(string settingsJson)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var normalizedJson = SlateDbSettingsSerializer.NormalizeForNative(
            settingsJson,
            out var filterBitsPerKey);
        ApplySettings(normalizedJson);

        if (filterBitsPerKey is { } bits)
            SetFilterBitsPerKey(bits);

        return this;
    }

    private void ApplySettings(string settingsJson)
    {
        var settings = SlateDbUniffi.Call(() => NativeSettings.FromJsonString(settingsJson));
        try
        {
            SlateDbUniffi.Call(() => Builder.WithSettings(settings));
        }
        catch
        {
            settings.Dispose();
            throw;
        }

        _settings?.Dispose();
        _settings = settings;
    }

    private void SetFilterBitsPerKey(uint bitsPerKey)
    {
        using var filterPolicy = SlateDbUniffi.Call(() => NativeFilterPolicy.Bloom(bitsPerKey));
        SlateDbUniffi.Call(() => Builder.WithFilterPolicies([filterPolicy]));
    }

    public SlateDbBuilder WithSstBlockSize(SstBlockSize size)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        SlateDbUniffi.Call(() => Builder.WithSstBlockSize(SlateDbUniffi.ToNative(size)));
        return this;
    }

    public SlateDb Build()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        NativeDb nativeDb;
        try
        {
            nativeDb = SlateDbUniffi.Wait(() => Builder.Build());
        }
        catch
        {
            Dispose();
            throw;
        }

        var metrics = _metrics ?? throw new ObjectDisposedException(nameof(SlateDbBuilder));
        _metrics = null;
        _disposed = true;
        DisposeBuilderResources();

        return SlateDb.FromNative(nativeDb, metrics);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        DisposeBuilderResources();
        _metrics?.Dispose();
        _metrics = null;
    }

    private NativeDbBuilder Builder =>
        _builder ?? throw new ObjectDisposedException(nameof(SlateDbBuilder));

    private void DisposeBuilderResources()
    {
        _settings?.Dispose();
        _settings = null;

        _builder?.Dispose();
        _builder = null;

        _objectStore?.Dispose();
        _objectStore = null;
    }

    private static SlateDbObjectStoreLocation ResolveObjectStore(
        string path,
        ObjectStoreConfig config)
    {
        var envFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(envFile, config.ToEnvFileContent());
            return SlateDbUniffi.ResolveObjectStore(path, null, envFile);
        }
        finally
        {
            try
            {
                File.Delete(envFile);
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }
}
