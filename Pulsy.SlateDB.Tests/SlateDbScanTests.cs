using FluentAssertions;
using Xunit;

namespace Pulsy.SlateDB.Tests;

[Collection("SlateDb")]
public class SlateDbScanTests
{
    private readonly SlateDbFixture _fixture;

    public SlateDbScanTests(SlateDbFixture fixture) => _fixture = fixture;

    [Fact]
    public void Scan_Range_ReturnsSubset()
    {
        using var db = _fixture.CreateDb();
        foreach (var c in "abcdefghij")
            db.Put(c.ToString(), c.ToString());

        // Scan [c, f) — keys c, d, e
        using var iter = db.Scan("c", "f");
        var results = iter.ToList();

        results.Select(kv => kv.KeyString).Should().Equal("c", "d", "e");
    }

    [Fact]
    public void ScanPrefix_ReturnsMatchingKeys()
    {
        using var db = _fixture.CreateDb();
        db.Put("user:1", "alice");
        db.Put("user:2", "bob");
        db.Put("order:1", "pizza");

        using var iter = db.ScanPrefix("user:");
        var results = iter.ToList();

        results.Should().HaveCount(2);
        results.Should().AllSatisfy(kv => kv.KeyString.Should().StartWith("user:"));
    }

    [Fact]
    public void Iterator_Next_ReturnsNull_AtEnd()
    {
        using var db = _fixture.CreateDb();

        using var iter = db.ScanPrefix("zzz_empty:");
        var kv = iter.Next();

        kv.Should().BeNull();
    }
}
