using System.Collections;
using System.Runtime.InteropServices;
using Pulsy.SlateDB.Native;

namespace Pulsy.SlateDB;

public sealed class SlateDbScanIterator : IDisposable, IEnumerable<SlateDbKeyValue>
{
    private nint _iterator;
    private bool _disposed;

    internal SlateDbScanIterator(nint iterator)
    {
        _iterator = iterator;
    }

    public SlateDbKeyValue? Next()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        unsafe
        {
            CSdbKeyValue kv;
            var result = NativeMethods.slatedb_iterator_next(_iterator, &kv);

            if (result.Error == CSdbError.NotFound)
            {
                NativeMethods.slatedb_free_result(result);
                return null;
            }

            SlateDbException.CheckResult(result);

            var key = new byte[(int)kv.Key.Len];
            Marshal.Copy(kv.Key.Data, key, 0, key.Length);

            var value = new byte[(int)kv.Value.Len];
            Marshal.Copy(kv.Value.Data, value, 0, value.Length);

            NativeMethods.slatedb_free_value(kv.Key);
            NativeMethods.slatedb_free_value(kv.Value);

            return new SlateDbKeyValue(key, value);
        }
    }

    public void Seek(string key) => Seek(SlateDbConvert.ToBytes(key));

    public void Seek(byte[] key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        unsafe
        {
            fixed (byte* keyPtr = key)
            {
                var result = NativeMethods.slatedb_iterator_seek(
                    _iterator, keyPtr, (nuint)key.Length);
                SlateDbException.CheckResult(result);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_iterator != nint.Zero)
        {
            NativeMethods.slatedb_iterator_close(_iterator);
            _iterator = nint.Zero;
        }
    }

    public IEnumerator<SlateDbKeyValue> GetEnumerator()
    {
        while (true)
        {
            var kv = Next();
            if (kv == null) yield break;
            yield return kv;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
