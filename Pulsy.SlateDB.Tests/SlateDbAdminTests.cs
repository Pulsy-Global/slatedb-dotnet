using FluentAssertions;
using Pulsy.SlateDB.Options;
using Xunit;

namespace Pulsy.SlateDB.Tests;

[Collection("SlateDb")]
public class SlateDbAdminTests
{
    private readonly SlateDbFixture _fixture;

    public SlateDbAdminTests(SlateDbFixture fixture) => _fixture = fixture;

    [Fact]
    public void RunGcOnce_DeletesEligibleWalFiles()
    {
        const string path = "admin_gc";
        var storeRoot = Path.Combine(_fixture.TempDir, $"store_{Guid.NewGuid():N}");
        var url = $"file:///{storeRoot}";

        using (var db = SlateDb.Open(path, url))
        {
            db.Put("key", "value");
            db.FlushMemTable();
        }

        for (var i = 0; i < 5; i++)
        {
            using var db = SlateDb.Open(path, url);
            db.GetString("key").Should().Be("value");
        }

        var walDirectory = Path.Combine(storeRoot, path, "wal");
        var filesBeforeGc = Directory.GetFiles(walDirectory);
        filesBeforeGc.Should().HaveCountGreaterThan(1);

        using (var admin = SlateDbAdmin.Open(path, url))
        {
            admin.RunGcOnce(new GarbageCollectorOptions
            {
                WalOptions = DeleteImmediately,
                WalFenceOptions = DeleteImmediately,
            });
        }

        Directory.GetFiles(walDirectory).Should().HaveCountLessThan(filesBeforeGc.Length);

        using var reopened = SlateDb.Open(path, url);
        reopened.GetString("key").Should().Be("value");
    }

    [Fact]
    public void RunGcOnce_RequiresCompleteDirectoryOptions()
    {
        const string path = "admin_gc_validation";
        var url = $"file:///{Path.Combine(_fixture.TempDir, $"store_{Guid.NewGuid():N}")}";
        using var admin = SlateDbAdmin.Open(path, url);

        var act = () => admin.RunGcOnce(new GarbageCollectorOptions
        {
            WalOptions = new GcDirectoryOptions { MinAge = TimeSpan.Zero },
        });

        act.Should().Throw<ArgumentException>()
            .WithParameterName("WalOptions");
    }

    private static GcDirectoryOptions DeleteImmediately => new()
    {
        MinAge = TimeSpan.Zero,
        DryRun = false,
    };
}
