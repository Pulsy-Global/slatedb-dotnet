using FluentAssertions;
using Xunit;

namespace Pulsy.SlateDB.Tests;

[Collection("SlateDb")]
public class SlateDbWriteBatchTests
{
    private readonly SlateDbFixture _fixture;

    public SlateDbWriteBatchTests(SlateDbFixture fixture) => _fixture = fixture;

    [Fact]
    public void Batch_PutMultiple_AllReadable()
    {
        using var db = _fixture.CreateDb();
        using var batch = SlateDb.NewWriteBatch();

        batch.Put("b1", "v1");
        batch.Put("b2", "v2");
        batch.Put("b3", "v3");
        db.Write(batch);

        db.GetString("b1").Should().Be("v1");
        db.GetString("b2").Should().Be("v2");
        db.GetString("b3").Should().Be("v3");
    }
}
