using FluentAssertions;
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
    public void Put_Get_LargeValue()
    {
        using var db = _fixture.CreateDb();
        var value = new string('x', 1024 * 1024);

        db.Put("large", value);

        db.GetString("large").Should().Be(value);
    }

    [Fact]
    public void Get_NonExistent_ReturnsNull()
    {
        using var db = _fixture.CreateDb();

        db.GetString("missing").Should().BeNull();
    }

    [Fact]
    public void Put_Overwrite_ReturnsLatest()
    {
        using var db = _fixture.CreateDb();

        db.Put("key", "v1");
        db.Put("key", "v2");

        db.GetString("key").Should().Be("v2");
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
    public void Delete_NonExistent_NoError()
    {
        using var db = _fixture.CreateDb();

        var act = () => db.Delete("nope");

        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_Idempotent()
    {
        var db = _fixture.CreateDb();

        db.Dispose();
        var act = () => db.Dispose();

        act.Should().NotThrow();
    }
}
