using FluentAssertions;
using Pulsy.SlateDB.Metrics;
using Pulsy.SlateDB.Options;
using Xunit;

namespace Pulsy.SlateDB.Tests;

[Collection("SlateDb")]
public class SlateDbTests
{
    private readonly SlateDbFixture _fixture;

    public SlateDbTests(SlateDbFixture fixture) => _fixture = fixture;

    [Fact]
    public void Open_Close_NoError()
    {
        using var db = _fixture.CreateDb();
    }

    [Fact]
    public void Put_Get_Roundtrip()
    {
        using var db = _fixture.CreateDb();

        db.Put("best_console", "steam deck");

        db.GetString("best_console").Should().Be("steam deck");
    }

    [Fact]
    public void Put_GetTyped_Roundtrip()
    {
        using var db = _fixture.CreateDb();

        db.Put("counter", 42);

        db.Get<int>("counter").Should().Be(42);
    }

    [Fact]
    public void Get_NonExistent_ReturnsNull()
    {
        using var db = _fixture.CreateDb();

        db.GetString("missing").Should().BeNull();
    }

    [Fact]
    public void Delete_RemovesKey()
    {
        using var db = _fixture.CreateDb();

        db.Put("del", "value");
        db.Delete("del");

        db.GetString("del").Should().BeNull();
    }

    [Fact]
    public void Put_ExpireAfter_HidesExpiredValue()
    {
        using var db = _fixture.CreateDb();
        db.Put(
            "ttl",
            "value",
            PutOptions.ExpireAfter(TimeSpan.FromMilliseconds(50)),
            WriteOptions.Default);

        Thread.Sleep(100);

        db.GetString("ttl").Should().BeNull();
    }

    [Fact]
    public void FlushMemTable_FlushesImmutableMemtable()
    {
        using var db = _fixture.CreateDb();
        db.Put("flush-memtable", "value");

        db.FlushMemTable();

        db.GetMetrics("slatedb.db.immutable_memtable_flushes")
            .Select(metric => metric.Value)
            .OfType<SlateDbCounterMetricValue>()
            .Should()
            .ContainSingle()
            .Which.Value.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Dispose_Idempotent()
    {
        var db = _fixture.CreateDb();

        db.Dispose();
        var act = () => db.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void GetMetrics_ReturnsTypedSnapshot()
    {
        using var db = _fixture.CreateDb();
        db.Put("metric", "value");
        db.GetString("metric");
        db.Flush();

        var metrics = db.GetMetrics();

        metrics.Should().NotBeEmpty();
        metrics.Should().OnlyContain(metric =>
            !string.IsNullOrWhiteSpace(metric.Name));
        metrics.Should().Contain(metric =>
            metric.Value.Kind == SlateDbMetricKind.Counter);
        metrics.Should().Contain(metric =>
            metric.Value.Kind == SlateDbMetricKind.Gauge);
        metrics.Should().Contain(metric =>
            metric.Value.Kind == SlateDbMetricKind.Histogram);
        metrics.Should().Contain(metric =>
            metric.Name == "slatedb.wal.wal_buffer_flushes");
    }

    [Fact]
    public void GetMetric_ReturnsMetricByExactLabels()
    {
        using var db = _fixture.CreateDb();
        db.GetString("missing");

        var metric = db.GetMetric(
            "slatedb.db.request_count",
            new SlateDbMetricLabel("op", "get"));

        metric.Should().NotBeNull();
        metric!.Labels.Should().Equal(new SlateDbMetricLabel("op", "get"));
        metric.Value.Should().BeOfType<SlateDbCounterMetricValue>()
            .Which.Value.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void GetMetrics_PreservesHistogramSummaryAndBuckets()
    {
        using var db = _fixture.CreateDb();
        db.Put("metric", "value");
        db.Flush();

        var histogram = db
            .GetMetrics("slatedb.object_store.request_duration_seconds")
            .Select(metric => metric.Value)
            .OfType<SlateDbHistogramMetricValue>()
            .Select(metric => metric.Value)
            .First(value => value.Count > 0);

        histogram.Sum.Should().BeGreaterThanOrEqualTo(0);
        histogram.Min.Should().BeGreaterThanOrEqualTo(0);
        histogram.Max.Should().BeGreaterThanOrEqualTo(histogram.Min);
        histogram.Boundaries.Should().NotBeEmpty();
        histogram.BucketCounts.Should().HaveCount(histogram.Boundaries.Count + 1);
        histogram.BucketCounts.Aggregate(0UL, (sum, count) => sum + count)
            .Should().Be(histogram.Count);
    }
}
