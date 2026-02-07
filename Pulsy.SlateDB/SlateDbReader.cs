using System.Runtime.InteropServices;
using Pulsy.SlateDB.Native;
using Pulsy.SlateDB.Options;

namespace Pulsy.SlateDB;

public sealed class SlateDbReader : ISlateDbReadable
{
    private CSdbReaderHandle _handle;
    private bool _disposed;

    private SlateDbReader(CSdbReaderHandle handle)
    {
        _handle = handle;
    }

    internal static SlateDbReader Open(string path, string url, string? envFile,
        string? checkpointId, ReaderOptions? options)
    {
        unsafe
        {
            CSdbReaderHandleResult handleResult;
            if (options != null)
            {
                var nativeOpts = new CSdbReaderOptions
                {
                    ManifestPollIntervalMs = (ulong)options.ManifestPollInterval.TotalMilliseconds,
                    CheckpointLifetimeMs = (ulong)options.CheckpointLifetime.TotalMilliseconds,
                    MaxMemtableBytes = options.MaxMemtableBytes,
                    SkipWalReplay = (byte)(options.SkipWalReplay ? 1 : 0),
                };
                handleResult = NativeMethods.slatedb_reader_open(
                    path, url, envFile, checkpointId, &nativeOpts);
            }
            else
            {
                handleResult = NativeMethods.slatedb_reader_open(
                    path, url, envFile, checkpointId, null);
            }

            SlateDbException.CheckResult(handleResult.Result);
            return new SlateDbReader(handleResult.Handle);
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

        unsafe
        {
            CSdbValue nativeValue;
            CSdbResult result;
            fixed (byte* keyPtr = key)
            {
                result = NativeMethods.slatedb_reader_get_with_options(
                    _handle, keyPtr, (nuint)key.Length, null, &nativeValue);
            }

            if (result.Error == CSdbError.NotFound)
            {
                NativeMethods.slatedb_free_result(result);
                return null;
            }

            SlateDbException.CheckResult(result);
            return ConsumeValue(nativeValue);
        }
    }

    public byte[]? Get(byte[] key, ReadOptions options)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var nativeOpts = new CSdbReadOptions
        {
            DurabilityFilter = (uint)options.DurabilityFilter,
            Dirty = (byte)(options.Dirty ? 1 : 0),
            CacheBlocks = (byte)(options.CacheBlocks ? 1 : 0),
        };

        unsafe
        {
            CSdbValue nativeValue;
            CSdbResult result;
            fixed (byte* keyPtr = key)
            {
                result = NativeMethods.slatedb_reader_get_with_options(
                    _handle, keyPtr, (nuint)key.Length, &nativeOpts, &nativeValue);
            }

            if (result.Error == CSdbError.NotFound)
            {
                NativeMethods.slatedb_free_result(result);
                return null;
            }

            SlateDbException.CheckResult(result);
            return ConsumeValue(nativeValue);
        }
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

        unsafe
        {
            nint iterPtr;
            fixed (byte* startPtr = startKey)
            fixed (byte* endPtr = endKey)
            {
                var result = NativeMethods.slatedb_reader_scan_with_options(
                    _handle,
                    startPtr, startKey != null ? (nuint)startKey.Length : 0,
                    endPtr, endKey != null ? (nuint)endKey.Length : 0,
                    null, &iterPtr);
                SlateDbException.CheckResult(result);
            }

            return new SlateDbScanIterator(iterPtr);
        }
    }

    public SlateDbScanIterator Scan(byte[]? startKey, byte[]? endKey, ScanOptions options)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var nativeOpts = SlateDb.ToNativeScanOptions(options);

        unsafe
        {
            nint iterPtr;
            fixed (byte* startPtr = startKey)
            fixed (byte* endPtr = endKey)
            {
                var result = NativeMethods.slatedb_reader_scan_with_options(
                    _handle,
                    startPtr, startKey != null ? (nuint)startKey.Length : 0,
                    endPtr, endKey != null ? (nuint)endKey.Length : 0,
                    &nativeOpts, &iterPtr);
                SlateDbException.CheckResult(result);
            }

            return new SlateDbScanIterator(iterPtr);
        }
    }

    public SlateDbScanIterator ScanPrefix(byte[] prefix)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        unsafe
        {
            nint iterPtr;
            fixed (byte* prefixPtr = prefix)
            {
                var result = NativeMethods.slatedb_reader_scan_prefix_with_options(
                    _handle, prefixPtr, (nuint)prefix.Length, null, &iterPtr);
                SlateDbException.CheckResult(result);
            }

            return new SlateDbScanIterator(iterPtr);
        }
    }

    public SlateDbScanIterator ScanPrefix(byte[] prefix, ScanOptions options)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var nativeOpts = SlateDb.ToNativeScanOptions(options);

        unsafe
        {
            nint iterPtr;
            fixed (byte* prefixPtr = prefix)
            {
                var result = NativeMethods.slatedb_reader_scan_prefix_with_options(
                    _handle, prefixPtr, (nuint)prefix.Length, &nativeOpts, &iterPtr);
                SlateDbException.CheckResult(result);
            }

            return new SlateDbScanIterator(iterPtr);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        var result = NativeMethods.slatedb_reader_close(_handle);
        _handle = default;
        SlateDbException.CheckResult(result);
    }

    private static byte[] ConsumeValue(CSdbValue nativeValue)
    {
        var managed = new byte[(int)nativeValue.Len];
        Marshal.Copy(nativeValue.Data, managed, 0, (int)nativeValue.Len);
        NativeMethods.slatedb_free_value(nativeValue);
        return managed;
    }
}
