using System.Runtime.InteropServices;
using Pulsy.SlateDB.Native;
using Pulsy.SlateDB.Options;

namespace Pulsy.SlateDB;

public sealed partial class SlateDb : ISlateDbReadable
{
    private CSdbHandle _handle;
    private bool _disposed;

    private SlateDb(CSdbHandle handle)
    {
        _handle = handle;
    }

    // --- Static: Library loading ---

    public static void LoadLibrary()
    {
        NativeLibraryLoader.Initialize();
    }

    public static void LoadLibrary(string absolutePath)
    {
        NativeLibraryLoader.Initialize(absolutePath);
    }

    // --- Static: Logging ---

    public static void InitLogging(LogLevel level)
    {
        NativeLibraryLoader.Initialize();
        var levelStr = level switch
        {
            LogLevel.Trace => "trace",
            LogLevel.Debug => "debug",
            LogLevel.Info => "info",
            LogLevel.Warn => "warn",
            LogLevel.Error => "error",
            _ => "info",
        };
        var result = NativeMethods.slatedb_init_logging(levelStr);
        SlateDbException.CheckResult(result);
    }

    // --- Static: Settings ---

    public static string SettingsDefault()
    {
        NativeLibraryLoader.Initialize();
        var ptr = NativeMethods.slatedb_settings_default();
        return ConsumeString(ptr);
    }

    public static string SettingsFromFile(string path)
    {
        NativeLibraryLoader.Initialize();
        var ptr = NativeMethods.slatedb_settings_from_file(path);
        return ConsumeString(ptr);
    }

    public static string SettingsFromEnv(string prefix)
    {
        NativeLibraryLoader.Initialize();
        var ptr = NativeMethods.slatedb_settings_from_env(prefix);
        return ConsumeString(ptr);
    }

    public static string SettingsLoad()
    {
        NativeLibraryLoader.Initialize();
        var ptr = NativeMethods.slatedb_settings_load();
        return ConsumeString(ptr);
    }

    // --- Static: Open ---

    public static SlateDb Open(string path, string? url = null, string? envFile = null)
    {
        NativeLibraryLoader.Initialize();
        var handleResult = NativeMethods.slatedb_open(path, url, envFile);
        SlateDbException.CheckResult(handleResult.Result);
        return new SlateDb(handleResult.Handle);
    }

    // --- Static: Reader ---

    public static SlateDbReader OpenReader(string path, string url, string? envFile,
        string? checkpointId, ReaderOptions? options = null)
    {
        NativeLibraryLoader.Initialize();
        return SlateDbReader.Open(path, url, envFile, checkpointId, options);
    }

    // --- Static: Builder ---

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

    // --- Static: WriteBatch ---

    public static SlateDbWriteBatch NewWriteBatch()
    {
        NativeLibraryLoader.Initialize();
        return new SlateDbWriteBatch();
    }

    // --- Maintenance ---

    public void Flush()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var result = NativeMethods.slatedb_flush(_handle);
        SlateDbException.CheckResult(result);
    }

    public string Metrics()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        unsafe
        {
            CSdbValue nativeValue;
            var result = NativeMethods.slatedb_metrics(_handle, &nativeValue);
            SlateDbException.CheckResult(result);

            var json = Marshal.PtrToStringUTF8(nativeValue.Data, (int)nativeValue.Len) ?? "";
            NativeMethods.slatedb_free_value(nativeValue);
            return json;
        }
    }

    // --- Dispose ---

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        var result = NativeMethods.slatedb_close(_handle);
        _handle = default;
        SlateDbException.CheckResult(result);
    }

    // --- Internal ---

    internal static SlateDb FromHandle(CSdbHandle handle) => new(handle);

    private static byte[] ConsumeValue(CSdbValue nativeValue)
    {
        var managed = new byte[(int)nativeValue.Len];
        Marshal.Copy(nativeValue.Data, managed, 0, (int)nativeValue.Len);
        NativeMethods.slatedb_free_value(nativeValue);
        return managed;
    }

    private static string ConsumeString(nint ptr)
    {
        if (ptr == nint.Zero) return "";
        var str = Marshal.PtrToStringUTF8(ptr) ?? "";
        var val = new CSdbValue { Data = ptr, Len = (nuint)str.Length };
        NativeMethods.slatedb_free_value(val);
        return str;
    }
}
