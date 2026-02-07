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

        unsafe
        {
            CSdbValue nativeValue;
            CSdbResult result;
            fixed (byte* keyPtr = key)
            {
                result = NativeMethods.slatedb_get_with_options(
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
                result = NativeMethods.slatedb_get_with_options(
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

    public void Put<T>(string key, T value) =>
        Put(SlateDbConvert.ToBytes(key), SlateDbConvert.ToBytes(value));

    public void Put<T>(string key, T value, PutOptions putOptions, WriteOptions writeOptions) =>
        Put(SlateDbConvert.ToBytes(key), SlateDbConvert.ToBytes(value), putOptions, writeOptions);

    public void Put(byte[] key, byte[] value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        unsafe
        {
            fixed (byte* keyPtr = key)
            fixed (byte* valuePtr = value)
            {
                var result = NativeMethods.slatedb_put_with_options(
                    _handle, keyPtr, (nuint)key.Length,
                    valuePtr, (nuint)value.Length, null, null);
                SlateDbException.CheckResult(result);
            }
        }
    }

    public void Put(byte[] key, byte[] value, PutOptions putOptions, WriteOptions writeOptions)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var nativePut = new CSdbPutOptions
        {
            TtlType = (uint)putOptions.TtlType,
            TtlValue = (ulong)putOptions.TtlValue.TotalMilliseconds,
        };
        var nativeWrite = new CSdbWriteOptions
        {
            AwaitDurable = (byte)(writeOptions.AwaitDurable ? 1 : 0),
        };

        unsafe
        {
            fixed (byte* keyPtr = key)
            fixed (byte* valuePtr = value)
            {
                var result = NativeMethods.slatedb_put_with_options(
                    _handle, keyPtr, (nuint)key.Length,
                    valuePtr, (nuint)value.Length, &nativePut, &nativeWrite);
                SlateDbException.CheckResult(result);
            }
        }
    }

    public void Delete(string key) => Delete(SlateDbConvert.ToBytes(key));
    public void Delete(string key, WriteOptions options) => Delete(SlateDbConvert.ToBytes(key), options);

    public void Delete(byte[] key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        unsafe
        {
            fixed (byte* keyPtr = key)
            {
                var result = NativeMethods.slatedb_delete_with_options(
                    _handle, keyPtr, (nuint)key.Length, null);
                SlateDbException.CheckResult(result);
            }
        }
    }

    public void Delete(byte[] key, WriteOptions options)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var nativeWrite = new CSdbWriteOptions
        {
            AwaitDurable = (byte)(options.AwaitDurable ? 1 : 0),
        };

        unsafe
        {
            fixed (byte* keyPtr = key)
            {
                var result = NativeMethods.slatedb_delete_with_options(
                    _handle, keyPtr, (nuint)key.Length, &nativeWrite);
                SlateDbException.CheckResult(result);
            }
        }
    }

    public void Write(SlateDbWriteBatch batch)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        unsafe
        {
            var result = NativeMethods.slatedb_write_batch_write(_handle, batch.NativeHandle, null);
            SlateDbException.CheckResult(result);
        }
    }

    public void Write(SlateDbWriteBatch batch, WriteOptions options)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var nativeWrite = new CSdbWriteOptions
        {
            AwaitDurable = (byte)(options.AwaitDurable ? 1 : 0),
        };

        unsafe
        {
            var result = NativeMethods.slatedb_write_batch_write(_handle, batch.NativeHandle, &nativeWrite);
            SlateDbException.CheckResult(result);
        }
    }
}
