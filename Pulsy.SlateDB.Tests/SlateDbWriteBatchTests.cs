using FluentAssertions;
using Pulsy.SlateDB.Options;
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

    [Fact]
    public void Batch_Delete_RemovesKey()
    {
        using var db = _fixture.CreateDb();
        db.Put("bd", "val");

        using var batch = SlateDb.NewWriteBatch();
        batch.Delete("bd");
        db.Write(batch);

        db.GetString("bd").Should().BeNull();
    }

    [Fact]
    public void Batch_PutWithOptions_Works()
    {
        using var db = _fixture.CreateDb();
        using var batch = SlateDb.NewWriteBatch();

        var putOpts = PutOptions.ExpireAfter(TimeSpan.FromMinutes(5));
        batch.Put("ttl_key", "ttl_val", putOpts);
        db.Write(batch);

        db.GetString("ttl_key").Should().Be("ttl_val");
    }
}
