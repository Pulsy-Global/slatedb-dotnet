namespace Pulsy.SlateDB.Options;

public record ReadOptions
{
    public static ReadOptions Default => new();

    public Durability DurabilityFilter { get; init; } = Durability.Memory;
    public bool Dirty { get; init; }
    public bool CacheBlocks { get; init; } = true;
}
