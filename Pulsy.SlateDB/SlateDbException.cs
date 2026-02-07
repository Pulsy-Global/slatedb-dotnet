using System.Runtime.InteropServices;
using Pulsy.SlateDB.Native;

namespace Pulsy.SlateDB;

public class SlateDbException : Exception
{
    public int ErrorCode { get; }

    public SlateDbException(string message)
        : base(message)
    {
    }

    public SlateDbException(int errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    internal static void CheckResult(CSdbResult result)
    {
        if (result.Error == CSdbError.Success)
            return;

        string? message = result.Message != nint.Zero
            ? Marshal.PtrToStringUTF8(result.Message)
            : null;

        NativeMethods.slatedb_free_result(result);

        throw new SlateDbException((int)result.Error, message ?? "Unknown error");
    }

    internal static void CheckResultAllowNotFound(CSdbResult result)
    {
        if (result.Error == CSdbError.Success || result.Error == CSdbError.NotFound)
            return;

        string? message = result.Message != nint.Zero
            ? Marshal.PtrToStringUTF8(result.Message)
            : null;

        NativeMethods.slatedb_free_result(result);

        throw new SlateDbException((int)result.Error, message ?? "Unknown error");
    }
}
