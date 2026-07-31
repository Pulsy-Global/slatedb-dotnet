using Pulsy.SlateDB.Options;
using NativeDurabilityLevel = uniffi.slatedb.DurabilityLevel;
using NativeFilterContext = uniffi.slatedb.FilterContext;
using NativeGarbageCollectorDirectoryOptions = uniffi.slatedb.GarbageCollectorDirectoryOptions;
using NativeGarbageCollectorOptions = uniffi.slatedb.GarbageCollectorOptions;
using NativeGarbageCollectorScheduleOptions = uniffi.slatedb.GarbageCollectorScheduleOptions;
using NativeIterationOrder = uniffi.slatedb.IterationOrder;
using NativeKeyRange = uniffi.slatedb.KeyRange;
using NativeKeyValue = uniffi.slatedb.KeyValue;
using NativeLogLevel = uniffi.slatedb.LogLevel;
using NativeObjectStore = uniffi.slatedb.ObjectStore;
using NativePutOptions = uniffi.slatedb.PutOptions;
using NativeReadOptions = uniffi.slatedb.ReadOptions;
using NativeReaderOptions = uniffi.slatedb.ReaderOptions;
using NativeScanOptions = uniffi.slatedb.ScanOptions;
using NativeSettings = uniffi.slatedb.Settings;
using NativeSstBlockSize = uniffi.slatedb.SstBlockSize;
using NativeTtl = uniffi.slatedb.Ttl;
using NativeWriteOptions = uniffi.slatedb.WriteOptions;

namespace Pulsy.SlateDB.Native;

internal static class SlateDbUniffi
{
    internal static T Call<T>(Func<T> call)
    {
        try
        {
            return call();
        }
        catch (uniffi.slatedb.Exception ex)
        {
            throw ToSlateDbException(ex);
        }
        catch (uniffi.slatedb.UniffiException ex)
        {
            throw new SlateDbException(0, ex.Message, ex);
        }
    }

    internal static void Call(Action call)
    {
        try
        {
            call();
        }
        catch (uniffi.slatedb.Exception ex)
        {
            throw ToSlateDbException(ex);
        }
        catch (uniffi.slatedb.UniffiException ex)
        {
            throw new SlateDbException(0, ex.Message, ex);
        }
    }

    internal static T Wait<T>(Func<Task<T>> call) =>
        Call(() => Task.Run(call).GetAwaiter().GetResult());

    internal static void Wait(Func<Task> call) =>
        Call(() => Task.Run(call).GetAwaiter().GetResult());

    internal static SlateDbObjectStoreLocation ResolveObjectStore(
        string path,
        string? url,
        string? envFile)
    {
        NativeLibraryLoader.Initialize();
        if (url is null)
        {
            return new SlateDbObjectStoreLocation(
                Call(() => NativeObjectStore.FromEnv(envFile)),
                path);
        }

        if (TryGetLocalFilePath(url, out var localPath))
        {
            return new SlateDbObjectStoreLocation(
                Call(() => NativeObjectStore.Resolve("file:///")),
                CombineObjectPath(localPath.Replace('\\', '/').Trim('/'), path));
        }

        var (objectStoreUrl, urlPath) = SplitObjectStoreUrl(url);
        return new SlateDbObjectStoreLocation(
            Call(() => NativeObjectStore.Resolve(objectStoreUrl)),
            CombineObjectPath(urlPath, path));
    }

    internal static string SettingsDefaultJson()
    {
        NativeLibraryLoader.Initialize();
        using var settings = Call(NativeSettings.Default);
        return Call(settings.ToJsonString);
    }

    internal static NativeReadOptions ToNative(ReadOptions options) => new(
        ToNative(options.DurabilityFilter),
        options.Dirty,
        options.CacheBlocks,
        ToNativeFilterContext(options.FilterContext));

    internal static NativeScanOptions ToNative(ScanOptions options) => new(
        ToNative(options.DurabilityFilter),
        options.Dirty,
        RequirePositive(options.ReadAheadBytes, nameof(options.ReadAheadBytes)),
        options.CacheBlocks,
        RequirePositive(options.MaxFetchTasks, nameof(options.MaxFetchTasks)),
        options.Order is { } order ? ToNative(order) : null,
        ToNativeFilterContext(options.FilterContext));

    internal static NativeWriteOptions ToNative(WriteOptions options) =>
        new(options.AwaitDurable);

    internal static NativeGarbageCollectorOptions ToNative(GarbageCollectorOptions options) => new(
        ManifestOptions: ToNative(options.ManifestOptions, nameof(options.ManifestOptions)),
        WalOptions: ToNative(options.WalOptions, nameof(options.WalOptions)),
        WalFenceOptions: ToNative(options.WalFenceOptions, nameof(options.WalFenceOptions)),
        CompactedOptions: ToNative(options.CompactedOptions, nameof(options.CompactedOptions)),
        CompactionsOptions: ToNative(options.CompactionsOptions, nameof(options.CompactionsOptions)),
        DetachOptions: ToNative(options.DetachOptions),
        DisableBoundaryFiles: options.BoundaryFilesEnabled == false,
        ObjectStoreMaxRetries: options.ObjectStoreMaxRetries);

    internal static NativePutOptions ToNative(PutOptions options) => new(options.TtlType switch
    {
        TtlType.Default => new NativeTtl.Default(),
        TtlType.NoExpiry => new NativeTtl.NoExpiry(),
        TtlType.ExpireAfter => new NativeTtl.ExpireAfterTicks(ToMilliseconds(options.TtlValue)),
        _ => throw new ArgumentOutOfRangeException(nameof(options), options.TtlType, "Unknown TTL type."),
    });

    internal static NativeReaderOptions ToNative(ReaderOptions options) => new(
        RequirePositive(
            ToMilliseconds(options.ManifestPollInterval),
            nameof(options.ManifestPollInterval)),
        RequirePositive(
            ToMilliseconds(options.CheckpointLifetime),
            nameof(options.CheckpointLifetime)),
        RequirePositive(options.MaxMemtableBytes, nameof(options.MaxMemtableBytes)),
        options.SkipWalReplay,
        options.ObjectStoreMaxRetries);

    internal static NativeKeyRange ToKeyRange(byte[]? startKey, byte[]? endKey) =>
        new(startKey, true, endKey, false);

    internal static NativeKeyRange UnboundedKeyRange() =>
        new(null, true, null, false);

    internal static NativeSstBlockSize ToNative(SstBlockSize size) =>
        (NativeSstBlockSize)size;

    internal static NativeLogLevel ToNative(LogLevel level) => level switch
    {
        LogLevel.Trace => NativeLogLevel.Trace,
        LogLevel.Debug => NativeLogLevel.Debug,
        LogLevel.Info => NativeLogLevel.Info,
        LogLevel.Warn => NativeLogLevel.Warn,
        LogLevel.Error => NativeLogLevel.Error,
        _ => NativeLogLevel.Info,
    };

    internal static SlateDbKeyValue ToPublic(NativeKeyValue value) =>
        new(value.Key, value.Value);

    internal static byte[]? ToValueOrNull(NativeKeyValue? value) =>
        value is null || IsExpired(value) ? null : value.Value;

    internal static bool IsExpired(NativeKeyValue value) =>
        value.ExpireTs is { } expireTs &&
        expireTs <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private static NativeDurabilityLevel ToNative(Durability durability) => durability switch
    {
        Durability.Memory => NativeDurabilityLevel.Memory,
        Durability.Remote => NativeDurabilityLevel.Remote,
        _ => throw new ArgumentOutOfRangeException(nameof(durability), durability, null),
    };

    private static NativeIterationOrder ToNative(IterationOrder order) => order switch
    {
        IterationOrder.Ascending => NativeIterationOrder.Ascending,
        IterationOrder.Descending => NativeIterationOrder.Descending,
        _ => throw new ArgumentOutOfRangeException(nameof(order), order, null),
    };

    private static NativeFilterContext? ToNativeFilterContext(byte[]? payload) =>
        payload is null ? null : new NativeFilterContext.Bytes(payload);

    private static NativeGarbageCollectorDirectoryOptions? ToNative(
        GcDirectoryOptions? options,
        string parameterName)
    {
        if (options is null)
            return null;

        if (options.MinAge is not { } minAge)
            throw new ArgumentException("MinAge must be set for one-shot garbage collection.", parameterName);

        if (options.DryRun is not { } dryRun)
            throw new ArgumentException("DryRun must be set for one-shot garbage collection.", parameterName);

        return new NativeGarbageCollectorDirectoryOptions(
            MinAgeMs: ToMilliseconds(minAge),
            DryRun: dryRun,
            IntervalMs: options.Interval is { } interval ? ToMilliseconds(interval) : null);
    }

    private static NativeGarbageCollectorScheduleOptions? ToNative(GcScheduleOptions? options) =>
        options is null
            ? null
            : new NativeGarbageCollectorScheduleOptions(
                options.Interval is { } interval ? ToMilliseconds(interval) : null);

    private static ulong RequirePositive(ulong value, string parameterName) =>
        value > 0
            ? value
            : throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Value must be greater than zero.");

    private static ulong ToMilliseconds(TimeSpan value)
    {
        if (value < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Duration must be non-negative.");

        return checked((ulong)value.TotalMilliseconds);
    }

    internal static (string ObjectStoreUrl, string Path) SplitObjectStoreUrl(string url)
    {
        // object_store expects scheme-only URLs to have a root path
        // (for example, "memory:///" rather than "memory://").
        if (url.EndsWith("://", StringComparison.Ordinal))
            return ($"{url}/", "");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return (url, "");

        var escapedPath = uri.GetComponents(UriComponents.Path, UriFormat.UriEscaped).Trim('/');
        if (escapedPath.Length == 0)
            return (url, "");

        var separatorIndex = escapedPath.IndexOf('/');
        var storePathLength = UsesFirstPathSegmentAsStoreIdentity(uri)
            ? separatorIndex >= 0 ? separatorIndex : escapedPath.Length
            : 0;
        var storePath = storePathLength > 0 ? $"/{escapedPath[..storePathLength]}" : "";
        var databasePath = storePathLength switch
        {
            0 => escapedPath,
            _ when separatorIndex < 0 => "",
            _ => escapedPath[(separatorIndex + 1)..],
        };

        var baseUrl = uri.Scheme.Equals(Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase)
            ? "file:///"
            : $"{uri.Scheme}://{uri.Authority}{storePath}";

        if (uri.Query.Length > 0)
            baseUrl += uri.Query;

        return (baseUrl, Uri.UnescapeDataString(databasePath));
    }

    private static bool UsesFirstPathSegmentAsStoreIdentity(Uri uri)
    {
        if (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return false;

        var host = uri.Host;
        return host.EndsWith("dfs.core.windows.net", StringComparison.OrdinalIgnoreCase) ||
               host.EndsWith("blob.core.windows.net", StringComparison.OrdinalIgnoreCase) ||
               host.EndsWith("dfs.fabric.microsoft.com", StringComparison.OrdinalIgnoreCase) ||
               host.EndsWith("blob.fabric.microsoft.com", StringComparison.OrdinalIgnoreCase) ||
               host.EndsWith("r2.cloudflarestorage.com", StringComparison.OrdinalIgnoreCase) ||
               host.EndsWith("amazonaws.com", StringComparison.OrdinalIgnoreCase) &&
               host.StartsWith("s3", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetLocalFilePath(string url, out string path)
    {
        if (!url.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            path = "";
            return false;
        }

        if (OperatingSystem.IsWindows() &&
            Uri.TryCreate(url, UriKind.Absolute, out var windowsUri))
        {
            path = windowsUri.LocalPath;
            return true;
        }

        var escapedPath = url["file:".Length..];
        var suffixIndex = escapedPath.IndexOfAny(['?', '#']);
        if (suffixIndex >= 0)
            escapedPath = escapedPath[..suffixIndex];

        path = "/" + Uri.UnescapeDataString(escapedPath).TrimStart('/');
        return true;
    }

    private static string CombineObjectPath(string prefix, string path)
    {
        if (prefix.Length == 0)
            return path;

        var suffix = path.Trim('/');
        return suffix.Length == 0 ? prefix : $"{prefix}/{suffix}";
    }

    private static SlateDbException ToSlateDbException(uniffi.slatedb.Exception ex)
    {
        var code = ex switch
        {
            uniffi.slatedb.Exception.Invalid => 1,       // InvalidArgument
            uniffi.slatedb.Exception.Unavailable => 4,   // IOError
            uniffi.slatedb.Exception.Data => 4,          // IOError
            uniffi.slatedb.Exception.Transaction => 5,   // InternalError
            uniffi.slatedb.Exception.Internal => 5,      // InternalError
            uniffi.slatedb.Exception.Closed => 7,        // InvalidHandle
            _ => 0,
        };

        var message = ex switch
        {
            uniffi.slatedb.Exception.Transaction error => error.message,
            uniffi.slatedb.Exception.Closed error => error.message,
            uniffi.slatedb.Exception.Unavailable error => error.message,
            uniffi.slatedb.Exception.Invalid error => error.message,
            uniffi.slatedb.Exception.Data error => error.message,
            uniffi.slatedb.Exception.Internal error => error.message,
            _ => ex.Message,
        };

        return new SlateDbException(
            code,
            string.IsNullOrWhiteSpace(message) ? ex.Message : message,
            ex);
    }
}
