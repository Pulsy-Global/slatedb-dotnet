using Pulsy.SlateDB.Native;
using Pulsy.SlateDB.Options;

namespace Pulsy.SlateDB;

public sealed partial class SlateDb
{
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
        var value = SlateDbUniffi.Wait(() => Db.GetKeyValue(key));
        return SlateDbUniffi.ToValueOrNull(value);
    }

    public byte[]? Get(byte[] key, ReadOptions options)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var value = SlateDbUniffi.Wait(() => Db.GetKeyValueWithOptions(
            key,
            SlateDbUniffi.ToNative(options)));
        return SlateDbUniffi.ToValueOrNull(value);
    }

    public void Put<T>(string key, T value) =>
        Put(SlateDbConvert.ToBytes(key), SlateDbConvert.ToBytes(value));

    public void Put<T>(string key, T value, PutOptions putOptions, WriteOptions writeOptions) =>
        Put(SlateDbConvert.ToBytes(key), SlateDbConvert.ToBytes(value), putOptions, writeOptions);

    public void Put(byte[] key, byte[] value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _ = SlateDbUniffi.Wait(() => Db.Put(key, value));
    }

    public void Put(byte[] key, byte[] value, PutOptions putOptions, WriteOptions writeOptions)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _ = SlateDbUniffi.Wait(() => Db.PutWithOptions(
            key,
            value,
            SlateDbUniffi.ToNative(putOptions),
            SlateDbUniffi.ToNative(writeOptions)));
    }

    public void Delete(string key) => Delete(SlateDbConvert.ToBytes(key));
    public void Delete(string key, WriteOptions options) => Delete(SlateDbConvert.ToBytes(key), options);

    public void Delete(byte[] key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _ = SlateDbUniffi.Wait(() => Db.Delete(key));
    }

    public void Delete(byte[] key, WriteOptions options)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _ = SlateDbUniffi.Wait(() => Db.DeleteWithOptions(key, SlateDbUniffi.ToNative(options)));
    }

    public void Write(SlateDbWriteBatch batch)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var nativeBatch = batch.NativeBatch;
        try
        {
            _ = SlateDbUniffi.Wait(() => Db.Write(nativeBatch));
        }
        finally
        {
            batch.MarkConsumed();
        }
    }

    public void Write(SlateDbWriteBatch batch, WriteOptions options)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var nativeBatch = batch.NativeBatch;
        try
        {
            _ = SlateDbUniffi.Wait(() => Db.WriteWithOptions(
                nativeBatch,
                SlateDbUniffi.ToNative(options)));
        }
        finally
        {
            batch.MarkConsumed();
        }
    }
}
