using Pulsy.SlateDB.Native;
using Pulsy.SlateDB.Options;

namespace Pulsy.SlateDB;

public sealed class SlateDbWriteBatch : IDisposable
{
    private nint _batch;
    private bool _disposed;

    internal nint NativeHandle
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _batch;
        }
    }

    internal SlateDbWriteBatch()
    {
        unsafe
        {
            nint batchPtr;
            var result = NativeMethods.slatedb_write_batch_new(&batchPtr);
            SlateDbException.CheckResult(result);
            _batch = batchPtr;
        }
    }

    public void Put<T>(string key, T value) =>
        Put(SlateDbConvert.ToBytes(key), SlateDbConvert.ToBytes(value));

    public void Put<T>(string key, T value, PutOptions options) =>
        Put(SlateDbConvert.ToBytes(key), SlateDbConvert.ToBytes(value), options);

    public void Delete(string key) => Delete(SlateDbConvert.ToBytes(key));

    public void Put(byte[] key, byte[] value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        unsafe
        {
            fixed (byte* keyPtr = key)
            fixed (byte* valuePtr = value)
            {
                var result = NativeMethods.slatedb_write_batch_put(
                    _batch, keyPtr, (nuint)key.Length, valuePtr, (nuint)value.Length);
                SlateDbException.CheckResult(result);
            }
        }
    }

    public void Put(byte[] key, byte[] value, PutOptions options)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var nativeOpts = new CSdbPutOptions
        {
            TtlType = (uint)options.TtlType,
            TtlValue = (ulong)options.TtlValue.TotalMilliseconds,
        };

        unsafe
        {
            fixed (byte* keyPtr = key)
            fixed (byte* valuePtr = value)
            {
                var result = NativeMethods.slatedb_write_batch_put_with_options(
                    _batch, keyPtr, (nuint)key.Length,
                    valuePtr, (nuint)value.Length, &nativeOpts);
                SlateDbException.CheckResult(result);
            }
        }
    }

    public void Delete(byte[] key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        unsafe
        {
            fixed (byte* keyPtr = key)
            {
                var result = NativeMethods.slatedb_write_batch_delete(
                    _batch, keyPtr, (nuint)key.Length);
                SlateDbException.CheckResult(result);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_batch != nint.Zero)
        {
            NativeMethods.slatedb_write_batch_close(_batch);
            _batch = nint.Zero;
        }
    }
}
