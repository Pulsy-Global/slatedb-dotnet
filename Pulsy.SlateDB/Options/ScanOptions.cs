namespace Pulsy.SlateDB.Options;

public record ScanOptions
{
    public static ScanOptions Default => new();

    public Durability DurabilityFilter { get; init; } = Durability.Memory;
    public bool Dirty { get; init; }
    public ulong ReadAheadBytes { get; init; } = 1;
    public bool CacheBlocks { get; init; } = true;
    public ulong MaxFetchTasks { get; init; } = 1;
    public IterationOrder? Order { get; init; }
    public byte[]? FilterContext { get; init; }
}
