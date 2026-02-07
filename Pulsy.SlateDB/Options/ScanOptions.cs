namespace Pulsy.SlateDB.Options;

public record ScanOptions
{
    public static ScanOptions Default => new();

    public Durability DurabilityFilter { get; init; } = Durability.Memory;
    public bool Dirty { get; init; }
    public ulong ReadAheadBytes { get; init; }
    public bool CacheBlocks { get; init; } = true;
    public ulong MaxFetchTasks { get; init; }
}
