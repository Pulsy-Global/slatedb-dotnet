using System.Runtime.InteropServices;

namespace Pulsy.SlateDB.Native;

internal enum CSdbError : int
{
    Success = 0,
    InvalidArgument = 1,
    NotFound = 2,
    AlreadyExists = 3,
    IOError = 4,
    InternalError = 5,
    NullPointer = 6,
    InvalidHandle = 7,
    InvalidProvider = 8,
}

[StructLayout(LayoutKind.Sequential)]
internal struct CSdbResult
{
    public CSdbError Error;
    public nint Message;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CSdbHandle
{
    public nint Ptr;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CSdbHandleResult
{
    public CSdbHandle Handle;
    public CSdbResult Result;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CSdbValue
{
    public nint Data;
    public nuint Len;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CSdbKeyValue
{
    public CSdbValue Key;
    public CSdbValue Value;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CSdbPutOptions
{
    public uint TtlType;
    public ulong TtlValue;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CSdbWriteOptions
{
    public byte AwaitDurable; // bool in C = 1 byte; P/Invoke bool defaults to 4 bytes
}

[StructLayout(LayoutKind.Sequential)]
internal struct CSdbReadOptions
{
    public uint DurabilityFilter;
    public byte Dirty;
    public byte CacheBlocks;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CSdbScanOptions
{
    public int DurabilityFilter;
    public byte Dirty;
    public ulong ReadAheadBytes;
    public byte CacheBlocks;
    public ulong MaxFetchTasks;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CSdbReaderOptions
{
    public ulong ManifestPollIntervalMs;
    public ulong CheckpointLifetimeMs;
    public ulong MaxMemtableBytes;
    public byte SkipWalReplay;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CSdbBuilderResult
{
    public nint Builder;
    public CSdbResult Result;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CSdbReaderHandle
{
    public nint Ptr;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CSdbReaderHandleResult
{
    public CSdbReaderHandle Handle;
    public CSdbResult Result;
}
