using Pulsy.SlateDB.Native;
using Pulsy.SlateDB.Options;
using NativeWriteBatch = uniffi.slatedb.WriteBatch;

namespace Pulsy.SlateDB;

public sealed class SlateDbWriteBatch : IDisposable
{
    private NativeWriteBatch? _batch;
    private bool _disposed;
    private bool _consumed;

    internal NativeWriteBatch NativeBatch
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_consumed)
                throw new InvalidOperationException("Write batch has already been consumed.");

            return _batch ?? throw new ObjectDisposedException(nameof(SlateDbWriteBatch));
        }
    }

    internal SlateDbWriteBatch()
    {
        _batch = SlateDbUniffi.Call(() => new NativeWriteBatch());
    }

    public void Put<T>(string key, T value) =>
        Put(SlateDbConvert.ToBytes(key), SlateDbConvert.ToBytes(value));

    public void Put<T>(string key, T value, PutOptions options) =>
        Put(SlateDbConvert.ToBytes(key), SlateDbConvert.ToBytes(value), options);

    public void Delete(string key) => Delete(SlateDbConvert.ToBytes(key));

    public void Put(byte[] key, byte[] value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        SlateDbUniffi.Call(() => NativeBatch.Put(key, value));
    }

    public void Put(byte[] key, byte[] value, PutOptions options)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        SlateDbUniffi.Call(() => NativeBatch.PutWithOptions(
            key,
            value,
            SlateDbUniffi.ToNative(options)));
    }

    public void Delete(byte[] key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        SlateDbUniffi.Call(() => NativeBatch.Delete(key));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _batch?.Dispose();
        _batch = null;
    }

    internal void MarkConsumed()
    {
        _consumed = true;
        Dispose();
    }
}
