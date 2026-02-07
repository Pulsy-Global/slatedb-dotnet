using FluentAssertions;
using Pulsy.SlateDB.Options;
using Xunit;

namespace Pulsy.SlateDB.Tests;

[Collection("SlateDb")]
public class SlateDbBuilderTests
{
    private readonly SlateDbFixture _fixture;

    public SlateDbBuilderTests(SlateDbFixture fixture) => _fixture = fixture;

    [Fact]
    public void Builder_Build_OpensDb()
    {
        var url = $"file:///{_fixture.TempDir}/builder_1";
        using var db = SlateDb.Builder("test", url).Build();

        db.Put("bk", "bv");
        db.GetString("bk").Should().Be("bv");
    }

    [Fact]
    public void Builder_WithSstBlockSize_OpensDb()
    {
        var url = $"file:///{_fixture.TempDir}/builder_2";
        using var db = SlateDb.Builder("test", url)
            .WithSstBlockSize(SstBlockSize.Block4KB)
            .Build();

        db.Put("sk", "sv");
        db.GetString("sk").Should().Be("sv");
    }

    [Fact]
    public void Builder_WithTypedSettings_OpensDb()
    {
        var url = $"file:///{_fixture.TempDir}/builder_3";
        using var db = SlateDb.Builder("test", url)
            .WithSettings(new SlateDbSettings
            {
                CompressionCodec = CompressionCodec.Lz4,
            })
            .Build();

        db.Put("tk", "tv");
        db.GetString("tk").Should().Be("tv");
    }

    [Fact]
    public void Builder_WithTypedSettingsDefaults_OpensDb()
    {
        var url = $"file:///{_fixture.TempDir}/builder_4";
        using var db = SlateDb.Builder("test", url)
            .WithSettings(new SlateDbSettings())
            .Build();

        db.Put("dk", "dv");
        db.GetString("dk").Should().Be("dv");
    }
}
