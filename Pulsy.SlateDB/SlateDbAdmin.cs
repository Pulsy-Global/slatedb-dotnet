using Pulsy.SlateDB.Native;
using Pulsy.SlateDB.Options;
using NativeAdmin = uniffi.slatedb.Admin;
using NativeAdminBuilder = uniffi.slatedb.AdminBuilder;

namespace Pulsy.SlateDB;

/// <summary>
/// Administrative handle for running maintenance operations against a SlateDB database.
/// </summary>
public sealed class SlateDbAdmin : IDisposable
{
    private NativeAdmin? _admin;
    private bool _disposed;

    private SlateDbAdmin(SlateDbObjectStoreLocation location)
    {
        using (location)
        using (var builder = SlateDbUniffi.Call(
                   () => new NativeAdminBuilder(location.Path, location.ObjectStore)))
        {
            _admin = SlateDbUniffi.Call(builder.Build);
        }
    }

    public static SlateDbAdmin Open(string path, string? url = null, string? envFile = null)
    {
        NativeLibraryLoader.Initialize();
        return new SlateDbAdmin(SlateDbUniffi.ResolveObjectStore(path, url, envFile));
    }

    public static SlateDbAdmin Open(string path, ObjectStoreConfig objectStore)
    {
        NativeLibraryLoader.Initialize();
        return new SlateDbAdmin(SlateDbBuilder.ResolveObjectStore(path, objectStore));
    }

    /// <summary>
    /// Runs the garbage collector once and waits for it to finish.
    /// </summary>
    /// <remarks>
    /// Passing <see langword="null"/> uses SlateDB's native defaults. When explicit
    /// directory options are supplied, <see cref="GcDirectoryOptions.MinAge"/> and
    /// <see cref="GcDirectoryOptions.DryRun"/> must both be set.
    /// </remarks>
    public void RunGcOnce(GarbageCollectorOptions? options = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var nativeOptions = options is null ? null : SlateDbUniffi.ToNative(options);
        SlateDbUniffi.Wait(() => Admin.RunGcOnce(nativeOptions));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _admin?.Dispose();
        _admin = null;
    }

    private NativeAdmin Admin =>
        _admin ?? throw new ObjectDisposedException(nameof(SlateDbAdmin));
}
