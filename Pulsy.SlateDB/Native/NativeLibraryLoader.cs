using System.Reflection;
using System.Runtime.InteropServices;

namespace Pulsy.SlateDB.Native;

internal static class NativeLibraryLoader
{
    private static readonly object InitializationLock = new();
    private static int _initialized;

    internal static void Initialize()
    {
        if (Volatile.Read(ref _initialized) == 1)
            return;

        lock (InitializationLock)
        {
            if (_initialized == 1)
                return;

            NativeLibrary.SetDllImportResolver(typeof(NativeLibraryLoader).Assembly, Resolve);
            Volatile.Write(ref _initialized, 1);
        }
    }

    internal static void Initialize(string absolutePath)
    {
        if (Volatile.Read(ref _initialized) == 1)
            return;

        lock (InitializationLock)
        {
            if (_initialized == 1)
                return;

            NativeLibrary.SetDllImportResolver(typeof(NativeLibraryLoader).Assembly, (name, _, _) =>
            {
                if (name != "slatedb_uniffi") return nint.Zero;
                return NativeLibrary.Load(absolutePath);
            });
            Volatile.Write(ref _initialized, 1);
        }
    }

    private static nint Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != "slatedb_uniffi")
            return nint.Zero;

        // Try default resolution first
        if (NativeLibrary.TryLoad(libraryName, assembly, searchPath, out var handle))
            return handle;

        // Try RID-specific path under runtimes/
        var rid = GetRuntimeIdentifier();
        var assemblyDir = Path.GetDirectoryName(assembly.Location) ?? ".";

        var candidates = new[]
        {
            Path.Combine(assemblyDir, "runtimes", rid, "native", GetLibraryFileName()),
            Path.Combine(assemblyDir, "..", "runtimes", rid, "native", GetLibraryFileName()),
            Path.Combine(assemblyDir, GetLibraryFileName()),
        };

        foreach (var candidate in candidates)
        {
            if (NativeLibrary.TryLoad(candidate, out handle))
                return handle;
        }

        return nint.Zero;
    }

    private static string GetRuntimeIdentifier()
    {
        var arch = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            _ => "x64",
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return $"osx-{arch}";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return $"linux-{arch}";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return $"win-{arch}";

        return $"linux-{arch}";
    }

    private static string GetLibraryFileName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "libslatedb_uniffi.dylib";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "slatedb_uniffi.dll";
        return "libslatedb_uniffi.so";
    }
}
