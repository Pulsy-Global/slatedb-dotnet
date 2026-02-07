namespace Pulsy.SlateDB.Options;

public record ObjectStoreConfig
{
    public required string Bucket { get; init; }
    public string? Region { get; init; }
    public string? Endpoint { get; init; }
    public string? AccessKeyId { get; init; }
    public string? SecretAccessKey { get; init; }
    public bool AllowHttp { get; init; }

    internal string ToEnvFileContent()
    {
        var lines = new List<string> { "CLOUD_PROVIDER=aws", $"AWS_BUCKET={Bucket}" };

        if (Region != null) lines.Add($"AWS_REGION={Region}");
        if (Endpoint != null) lines.Add($"AWS_ENDPOINT_URL={Endpoint}");
        if (AccessKeyId != null) lines.Add($"AWS_ACCESS_KEY_ID={AccessKeyId}");
        if (SecretAccessKey != null) lines.Add($"AWS_SECRET_ACCESS_KEY={SecretAccessKey}");
        if (AllowHttp) lines.Add("AWS_ALLOW_HTTP=true");

        return string.Join('\n', lines);
    }
}
