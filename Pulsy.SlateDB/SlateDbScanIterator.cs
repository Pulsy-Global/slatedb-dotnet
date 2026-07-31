using System.Collections;
using Pulsy.SlateDB.Native;
using NativeDbIterator = uniffi.slatedb.DbIterator;

namespace Pulsy.SlateDB;

public sealed class SlateDbScanIterator : IDisposable, IEnumerable<SlateDbKeyValue>
{
    private NativeDbIterator? _iterator;
    private bool _disposed;

    internal SlateDbScanIterator(NativeDbIterator iterator)
    {
        _iterator = iterator;
    }

    public SlateDbKeyValue? Next()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        while (true)
        {
            var native = SlateDbUniffi.Wait(() => Iterator.Next());
            if (native is null)
                return null;

            if (!SlateDbUniffi.IsExpired(native))
                return SlateDbUniffi.ToPublic(native);
        }
    }

    public void Seek(string key) => Seek(SlateDbConvert.ToBytes(key));

    public void Seek(byte[] key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        SlateDbUniffi.Wait(() => Iterator.Seek(key));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _iterator?.Dispose();
        _iterator = null;
    }

    public IEnumerator<SlateDbKeyValue> GetEnumerator()
    {
        while (true)
        {
            var kv = Next();
            if (kv is null) yield break;
            yield return kv;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private NativeDbIterator Iterator =>
        _iterator ?? throw new ObjectDisposedException(nameof(SlateDbScanIterator));
}
