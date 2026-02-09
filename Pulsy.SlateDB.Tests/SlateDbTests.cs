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
    public void Dispose_Idempotent()
    {
        var db = _fixture.CreateDb();

        db.Dispose();
        var act = () => db.Dispose();

        act.Should().NotThrow();
    }
}
