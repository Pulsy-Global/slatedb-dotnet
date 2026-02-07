using FluentAssertions;
using Xunit;

namespace Pulsy.SlateDB.Tests;

[Collection("SlateDb")]
public class SlateDbStringTests
{
    private readonly SlateDbFixture _fixture;

    public SlateDbStringTests(SlateDbFixture fixture) => _fixture = fixture;

    [Fact]
    public void PutString_GetString_Roundtrip()
    {
        using var db = _fixture.CreateDb();

        db.Put("greeting", "hello world");
        var result = db.GetString("greeting");

        result.Should().Be("hello world");
    }

    [Fact]
    public void PutInt_GetInt_Roundtrip()
    {
        using var db = _fixture.CreateDb();

        db.Put("counter", 42);
        var result = db.Get<int>("counter");

        result.Should().Be(42);
    }

    [Fact]
    public void PutLong_GetLong_Roundtrip()
    {
        using var db = _fixture.CreateDb();

        db.Put("bignum", 9_876_543_210L);
        var result = db.Get<long>("bignum");

        result.Should().Be(9_876_543_210L);
    }

    [Fact]
    public void PutBool_GetBool_Roundtrip()
    {
        using var db = _fixture.CreateDb();

        db.Put("flag_true", true);
        db.Put("flag_false", false);

        db.Get<bool>("flag_true").Should().Be(true);
        db.Get<bool>("flag_false").Should().Be(false);
    }

    [Fact]
    public void PutDouble_GetDouble_Roundtrip()
    {
        using var db = _fixture.CreateDb();

        db.Put("pi", 3.14159265);
        var result = db.Get<double>("pi");

        result.Should().Be(3.14159265);
    }

    [Fact]
    public void GetString_NonExistent_ReturnsNull()
    {
        using var db = _fixture.CreateDb();

        db.GetString("missing").Should().BeNull();
    }

    [Fact]
    public void GetInt_NonExistent_ReturnsNull()
    {
        using var db = _fixture.CreateDb();

        db.Get<int>("missing").Should().BeNull();
    }

    [Fact]
    public void Delete_StringKey_Works()
    {
        using var db = _fixture.CreateDb();

        db.Put("to_delete", "value");
        db.Delete("to_delete");

        db.GetString("to_delete").Should().BeNull();
    }

    [Fact]
    public void Scan_StringKeys_Works()
    {
        using var db = _fixture.CreateDb();
        db.Put("scan_a", "1");
        db.Put("scan_b", "2");
        db.Put("scan_c", "3");

        using var iter = db.Scan("scan_a", "scan_c");
        var results = iter.ToList();

        results.Should().HaveCount(2);
        results[0].KeyString.Should().Be("scan_a");
        results[1].KeyString.Should().Be("scan_b");
    }

    [Fact]
    public void ScanPrefix_StringKey_Works()
    {
        using var db = _fixture.CreateDb();
        db.Put("pfx:1", "a");
        db.Put("pfx:2", "b");
        db.Put("other:1", "c");

        using var iter = db.ScanPrefix("pfx:");
        var results = iter.ToList();

        results.Should().HaveCount(2);
        results.Should().AllSatisfy(kv => kv.KeyString.Should().StartWith("pfx:"));
    }

    [Fact]
    public void KeyValue_StringProperties_Work()
    {
        using var db = _fixture.CreateDb();
        db.Put("kv_key", "kv_value");

        using var iter = db.ScanPrefix("kv_key");
        var kv = iter.Next();

        kv.Should().NotBeNull();
        kv!.KeyString.Should().Be("kv_key");
        kv.ValueString.Should().Be("kv_value");
    }

    [Fact]
    public void WriteBatch_StringOverloads_Work()
    {
        using var db = _fixture.CreateDb();
        using var batch = SlateDb.NewWriteBatch();

        batch.Put("wb_str", "hello");
        batch.Put("wb_int", 99);
        batch.Put("wb_bool", true);
        db.Write(batch);

        db.GetString("wb_str").Should().Be("hello");
        db.Get<int>("wb_int").Should().Be(99);
        db.Get<bool>("wb_bool").Should().Be(true);
    }

    [Fact]
    public void WriteBatch_Delete_StringKey_Works()
    {
        using var db = _fixture.CreateDb();
        db.Put("wb_del", "value");

        using var batch = SlateDb.NewWriteBatch();
        batch.Delete("wb_del");
        db.Write(batch);

        db.GetString("wb_del").Should().BeNull();
    }

    [Fact]
    public void Iterator_Seek_StringKey_Works()
    {
        using var db = _fixture.CreateDb();
        db.Put("seek_01", "a");
        db.Put("seek_02", "b");
        db.Put("seek_03", "c");

        using var iter = db.Scan((string?)null, null);
        iter.Seek("seek_02");
        var kv = iter.Next();

        kv.Should().NotBeNull();
        kv!.KeyString.Should().Be("seek_02");
    }
}
