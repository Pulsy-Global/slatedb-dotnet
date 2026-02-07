namespace Pulsy.SlateDB.Options;

public record PutOptions
{
    public static PutOptions DefaultTtl => new() { TtlType = TtlType.Default };
    public static PutOptions NoExpiry => new() { TtlType = TtlType.NoExpiry };

    public static PutOptions ExpireAfter(TimeSpan ttl) =>
        new() { TtlType = TtlType.ExpireAfter, TtlValue = ttl };

    public TtlType TtlType { get; init; }
    public TimeSpan TtlValue { get; init; }
}
