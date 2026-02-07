using System.Buffers.Binary;
using System.Text;

namespace Pulsy.SlateDB;

internal static class SlateDbConvert
{
    public static byte[] ToBytes<T>(T value) => value switch
    {
        string s => Encoding.UTF8.GetBytes(s),
        int i => WriteInt(i),
        long l => WriteLong(l),
        bool b => [b ? (byte)1 : (byte)0],
        double d => WriteDouble(d),
        byte[] bytes => bytes,
        _ => throw new ArgumentException($"Unsupported type: {typeof(T).Name}. Supported: string, int, long, bool, double, byte[].")
    };

    public static T FromBytes<T>(byte[] bytes)
    {
        if (typeof(T) == typeof(string)) return (T)(object)Encoding.UTF8.GetString(bytes);
        if (typeof(T) == typeof(int)) return (T)(object)ReadInt(bytes);
        if (typeof(T) == typeof(long)) return (T)(object)ReadLong(bytes);
        if (typeof(T) == typeof(bool)) return (T)(object)ReadBool(bytes);
        if (typeof(T) == typeof(double)) return (T)(object)ReadDouble(bytes);
        if (typeof(T) == typeof(byte[])) return (T)(object)bytes;
        throw new ArgumentException($"Unsupported type: {typeof(T).Name}. Supported: string, int, long, bool, double, byte[].");
    }

    private static byte[] WriteInt(int value)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        return bytes;
    }

    private static byte[] WriteLong(long value)
    {
        var bytes = new byte[8];
        BinaryPrimitives.WriteInt64BigEndian(bytes, value);
        return bytes;
    }

    private static byte[] WriteDouble(double value)
    {
        var bytes = new byte[8];
        BinaryPrimitives.WriteDoubleBigEndian(bytes, value);
        return bytes;
    }

    private static int ReadInt(byte[] bytes)
    {
        if (bytes.Length != 4)
            throw new ArgumentException($"Expected 4 bytes for int, got {bytes.Length}.");
        return BinaryPrimitives.ReadInt32BigEndian(bytes);
    }

    private static long ReadLong(byte[] bytes)
    {
        if (bytes.Length != 8)
            throw new ArgumentException($"Expected 8 bytes for long, got {bytes.Length}.");
        return BinaryPrimitives.ReadInt64BigEndian(bytes);
    }

    private static bool ReadBool(byte[] bytes)
    {
        if (bytes.Length != 1)
            throw new ArgumentException($"Expected 1 byte for bool, got {bytes.Length}.");
        return bytes[0] != 0;
    }

    private static double ReadDouble(byte[] bytes)
    {
        if (bytes.Length != 8)
            throw new ArgumentException($"Expected 8 bytes for double, got {bytes.Length}.");
        return BinaryPrimitives.ReadDoubleBigEndian(bytes);
    }
}
