namespace Pulsy.SlateDB.Options;

public record ReaderOptions
{
    public static ReaderOptions Default => new();

    public TimeSpan ManifestPollInterval { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan CheckpointLifetime { get; init; } = TimeSpan.FromMinutes(10);
    public ulong MaxMemtableBytes { get; init; } = 64 * 1024 * 1024;
    public bool SkipWalReplay { get; init; }
    public uint? ObjectStoreMaxRetries { get; init; }
}
