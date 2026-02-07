using Pulsy.SlateDB.Options;

namespace Pulsy.SlateDB;

public interface ISlateDbReadable : IDisposable
{
    byte[]? Get(byte[] key);
    byte[]? Get(byte[] key, ReadOptions options);

    byte[]? Get(string key);
    byte[]? Get(string key, ReadOptions options);
    T? Get<T>(string key) where T : struct;
    string? GetString(string key);

    SlateDbScanIterator Scan(byte[]? startKey, byte[]? endKey);
    SlateDbScanIterator Scan(byte[]? startKey, byte[]? endKey, ScanOptions options);
    SlateDbScanIterator Scan(string? startKey, string? endKey);
    SlateDbScanIterator Scan(string? startKey, string? endKey, ScanOptions options);

    SlateDbScanIterator ScanPrefix(byte[] prefix);
    SlateDbScanIterator ScanPrefix(byte[] prefix, ScanOptions options);
    SlateDbScanIterator ScanPrefix(string prefix);
    SlateDbScanIterator ScanPrefix(string prefix, ScanOptions options);
}
