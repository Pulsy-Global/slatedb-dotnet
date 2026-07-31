using FluentAssertions;
using Pulsy.SlateDB.Metrics;
using Pulsy.SlateDB.Options;
using Xunit;

namespace Pulsy.SlateDB.Tests;

[Collection("SlateDb")]
public class SlateDbReaderTests
{
    private readonly SlateDbFixture _fixture;

    public SlateDbReaderTests(SlateDbFixture fixture) => _fixture = fixture;

    [Fact]
    public void Reader_OpensExistingDatabase()
    {
        var url = $"file:///{_fixture.TempDir}/reader";
        const string path = "test";

        using (var db = SlateDb.Open(path, url))
        {
            db.Put("reader-key", "reader-value");
            db.Flush();
        }

        using var reader = SlateDb.OpenReader(path, url, null, null, ReaderOptions.Default);

        reader.GetString("reader-key").Should().Be("reader-value");
    }

    [Fact]
    public void Reader_GetMetrics_ReturnsTypedSnapshot()
    {
        var url = $"file:///{_fixture.TempDir}/reader_metrics";
        const string path = "test";

        using (var db = SlateDb.Open(path, url))
        {
            db.Put("reader-key", "reader-value");
            db.Flush();
        }

        using var reader = SlateDb.OpenReader(path, url, null, null);
        reader.GetString("reader-key");

        reader.GetMetrics().Should().Contain(metric =>
            metric.Value.Kind == SlateDbMetricKind.Counter);
    }

    [Fact]
    public void Reader_HidesExpiredValue()
    {
        var url = $"file:///{_fixture.TempDir}/reader_ttl";
        const string path = "test";

        using (var db = SlateDb.Open(path, url))
        {
            db.Put(
                "reader-key",
                "reader-value",
                PutOptions.ExpireAfter(TimeSpan.FromMilliseconds(50)),
                WriteOptions.Default);
            db.FlushMemTable();
        }

        Thread.Sleep(100);

        using var reader = SlateDb.OpenReader(path, url, null, null);
        reader.GetString("reader-key").Should().BeNull();
    }
}
