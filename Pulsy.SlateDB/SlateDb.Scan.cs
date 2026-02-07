using Pulsy.SlateDB.Native;
using Pulsy.SlateDB.Options;

namespace Pulsy.SlateDB;

public sealed partial class SlateDb
{
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
                var result = NativeMethods.slatedb_scan_with_options(
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

        var nativeOpts = ToNativeScanOptions(options);

        unsafe
        {
            nint iterPtr;
            fixed (byte* startPtr = startKey)
            fixed (byte* endPtr = endKey)
            {
                var result = NativeMethods.slatedb_scan_with_options(
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
                var result = NativeMethods.slatedb_scan_prefix_with_options(
                    _handle, prefixPtr, (nuint)prefix.Length, null, &iterPtr);
                SlateDbException.CheckResult(result);
            }

            return new SlateDbScanIterator(iterPtr);
        }
    }

    public SlateDbScanIterator ScanPrefix(byte[] prefix, ScanOptions options)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var nativeOpts = ToNativeScanOptions(options);

        unsafe
        {
            nint iterPtr;
            fixed (byte* prefixPtr = prefix)
            {
                var result = NativeMethods.slatedb_scan_prefix_with_options(
                    _handle, prefixPtr, (nuint)prefix.Length, &nativeOpts, &iterPtr);
                SlateDbException.CheckResult(result);
            }

            return new SlateDbScanIterator(iterPtr);
        }
    }

    internal static CSdbScanOptions ToNativeScanOptions(ScanOptions options) => new()
    {
        DurabilityFilter = (int)options.DurabilityFilter,
        Dirty = (byte)(options.Dirty ? 1 : 0),
        ReadAheadBytes = options.ReadAheadBytes,
        CacheBlocks = (byte)(options.CacheBlocks ? 1 : 0),
        MaxFetchTasks = options.MaxFetchTasks,
    };
}
