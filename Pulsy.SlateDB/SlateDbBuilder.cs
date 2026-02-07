using Pulsy.SlateDB.Native;
using Pulsy.SlateDB.Options;

namespace Pulsy.SlateDB;

public sealed class SlateDbBuilder : IDisposable
{
    private nint _builder;
    private bool _disposed;
    private string? _tempEnvFile;

    internal SlateDbBuilder(string path, string? url, string? envFile)
    {
        var builderResult = NativeMethods.slatedb_builder_new(path, url, envFile);
        SlateDbException.CheckResult(builderResult.Result);
        _builder = builderResult.Builder;
    }

    internal SlateDbBuilder(string path, ObjectStoreConfig config)
    {
        _tempEnvFile = Path.GetTempFileName();
        File.WriteAllText(_tempEnvFile, config.ToEnvFileContent());

        var builderResult = NativeMethods.slatedb_builder_new(path, null, _tempEnvFile);
        SlateDbException.CheckResult(builderResult.Result);
        _builder = builderResult.Builder;
    }

    public SlateDbBuilder WithSettings(SlateDbSettings settings)
    {
        var json = SlateDbSettingsSerializer.ToJson(settings);
        return WithSettings(json);
    }

    public SlateDbBuilder WithSettings(string settingsJson)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var result = NativeMethods.slatedb_builder_with_settings(_builder, settingsJson);
        SlateDbException.CheckResult(result);
        return this;
    }

    public SlateDbBuilder WithSstBlockSize(SstBlockSize size)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var result = NativeMethods.slatedb_builder_with_sst_block_size(_builder, (byte)size);
        SlateDbException.CheckResult(result);
        return this;
    }

    public SlateDb Build()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var handleResult = NativeMethods.slatedb_builder_build(_builder);
        _builder = nint.Zero;
        _disposed = true;
        CleanupTempFile();
        SlateDbException.CheckResult(handleResult.Result);
        return SlateDb.FromHandle(handleResult.Handle);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        CleanupTempFile();

        if (_builder != nint.Zero)
        {
            NativeMethods.slatedb_builder_free(_builder);
            _builder = nint.Zero;
        }
    }

    private void CleanupTempFile()
    {
        if (_tempEnvFile == null) return;
        try { File.Delete(_tempEnvFile); } catch { /* best effort */ }
        _tempEnvFile = null;
    }
}
