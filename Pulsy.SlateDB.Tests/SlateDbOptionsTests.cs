using FluentAssertions;
using Pulsy.SlateDB.Native;
using Pulsy.SlateDB.Options;
using Xunit;

namespace Pulsy.SlateDB.Tests;

public class SlateDbOptionsTests
{
    [Fact]
    public void ScanOptions_Defaults_AreExplicit()
    {
        var options = ScanOptions.Default;

        options.ReadAheadBytes.Should().Be(1);
        options.MaxFetchTasks.Should().Be(1);
        SlateDbUniffi.ToNative(options).Should().Match<uniffi.slatedb.ScanOptions>(
            native => native.ReadAheadBytes == 1 &&
                      native.MaxFetchTasks == 1);
    }

    [Fact]
    public void ScanOptions_MapsNewNativeFields()
    {
        var options = new ScanOptions
        {
            Order = IterationOrder.Descending,
            FilterContext = [1, 2, 3],
        };

        var native = SlateDbUniffi.ToNative(options);

        native.Order.Should().Be(uniffi.slatedb.IterationOrder.Descending);
        native.FilterContext.Should().BeOfType<uniffi.slatedb.FilterContext.Bytes>()
            .Which.Payload.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void ReaderOptions_MapsObjectStoreRetryLimit()
    {
        var native = SlateDbUniffi.ToNative(new ReaderOptions
        {
            ObjectStoreMaxRetries = 5,
        });

        native.MaxMemtableBytes.Should().Be(64 * 1024 * 1024);
        native.ObjectStoreMaxRetries.Should().Be(5);
    }

    [Fact]
    public void ReadOptions_MapsFilterContext()
    {
        var native = SlateDbUniffi.ToNative(new ReadOptions
        {
            FilterContext = [4, 5, 6],
        });

        native.FilterContext.Should().BeOfType<uniffi.slatedb.FilterContext.Bytes>()
            .Which.Payload.Should().Equal(4, 5, 6);
    }
}
