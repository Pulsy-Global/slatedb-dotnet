using FluentAssertions;
using Pulsy.SlateDB.Native;
using Xunit;

namespace Pulsy.SlateDB.Tests;

public class SlateDbObjectStoreUrlTests
{
    [Theory]
    [InlineData(
        "https://account.blob.core.windows.net/container/prefix/db",
        "https://account.blob.core.windows.net/container",
        "prefix/db")]
    [InlineData(
        "https://account.dfs.fabric.microsoft.com/container/prefix",
        "https://account.dfs.fabric.microsoft.com/container",
        "prefix")]
    [InlineData(
        "https://s3.eu-west-1.amazonaws.com/bucket/prefix",
        "https://s3.eu-west-1.amazonaws.com/bucket",
        "prefix")]
    [InlineData(
        "https://account.r2.cloudflarestorage.com/bucket/prefix",
        "https://account.r2.cloudflarestorage.com/bucket",
        "prefix")]
    public void SplitObjectStoreUrl_RetainsProviderBucketOrContainer(
        string url,
        string expectedObjectStoreUrl,
        string expectedPath)
    {
        SlateDbUniffi.SplitObjectStoreUrl(url)
            .Should()
            .Be((expectedObjectStoreUrl, expectedPath));
    }

    [Theory]
    [InlineData(
        "https://bucket.s3.eu-west-1.amazonaws.com/prefix/db",
        "https://bucket.s3.eu-west-1.amazonaws.com",
        "prefix/db")]
    [InlineData(
        "https://example.com/prefix/db",
        "https://example.com",
        "prefix/db")]
    [InlineData(
        "s3://bucket/prefix/db",
        "s3://bucket",
        "prefix/db")]
    public void SplitObjectStoreUrl_KeepsExistingAuthorityBasedBehavior(
        string url,
        string expectedObjectStoreUrl,
        string expectedPath)
    {
        SlateDbUniffi.SplitObjectStoreUrl(url)
            .Should()
            .Be((expectedObjectStoreUrl, expectedPath));
    }

    [Fact]
    public void SplitObjectStoreUrl_PreservesProviderQuery()
    {
        SlateDbUniffi.SplitObjectStoreUrl(
                "https://s3.amazonaws.com/bucket/prefix?region=eu-west-1")
            .Should()
            .Be(("https://s3.amazonaws.com/bucket?region=eu-west-1", "prefix"));
    }
}
