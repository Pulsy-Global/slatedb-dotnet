namespace Pulsy.SlateDB.Options;

public record WriteOptions
{
    public static WriteOptions Default => new() { AwaitDurable = true };

    public bool AwaitDurable { get; init; } = true;
}
