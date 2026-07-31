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
        var iterator = SlateDbUniffi.Wait(() => Db.Scan(
            SlateDbUniffi.ToKeyRange(startKey, endKey)));
        return new SlateDbScanIterator(iterator);
    }

    public SlateDbScanIterator Scan(byte[]? startKey, byte[]? endKey, ScanOptions options)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var iterator = SlateDbUniffi.Wait(() => Db.ScanWithOptions(
            SlateDbUniffi.ToKeyRange(startKey, endKey),
            SlateDbUniffi.ToNative(options)));
        return new SlateDbScanIterator(iterator);
    }

    public SlateDbScanIterator ScanPrefix(byte[] prefix)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var iterator = SlateDbUniffi.Wait(() => Db.ScanPrefix(
            prefix,
            SlateDbUniffi.UnboundedKeyRange()));
        return new SlateDbScanIterator(iterator);
    }

    public SlateDbScanIterator ScanPrefix(byte[] prefix, ScanOptions options)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var iterator = SlateDbUniffi.Wait(() => Db.ScanPrefixWithOptions(
            prefix,
            SlateDbUniffi.UnboundedKeyRange(),
            SlateDbUniffi.ToNative(options)));
        return new SlateDbScanIterator(iterator);
    }
}
