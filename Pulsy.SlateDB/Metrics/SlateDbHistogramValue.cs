namespace Pulsy.SlateDB.Metrics;

/// <summary>
/// A point-in-time histogram summary and its bucket counts.
/// </summary>
public sealed record SlateDbHistogramValue(
    ulong Count,
    double Sum,
    double Min,
    double Max,
    IReadOnlyList<double> Boundaries,
    IReadOnlyList<ulong> BucketCounts);
