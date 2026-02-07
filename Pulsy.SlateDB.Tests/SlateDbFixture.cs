using Xunit;

namespace Pulsy.SlateDB.Tests;

public sealed class SlateDbFixture : IDisposable
{
    public string TempDir { get; }

    private int _dbCounter;

    public SlateDbFixture()
    {
        TempDir = Path.Combine(Path.GetTempPath(), $"slatedb_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(TempDir);
        SlateDb.LoadLibrary();
    }

    public SlateDb CreateDb()
    {
        var id = Interlocked.Increment(ref _dbCounter);
        var path = $"test_{id}";
        var url = $"file:///{TempDir}/store_{id}";
        return SlateDb.Open(path, url);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(TempDir))
                Directory.Delete(TempDir, recursive: true);
        }
        catch
        {
            // Best-effort cleanup
        }
    }
}

[CollectionDefinition("SlateDb")]
public class SlateDbCollection : ICollectionFixture<SlateDbFixture>;
