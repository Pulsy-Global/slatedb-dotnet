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

    internal SlateDbException(int errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
