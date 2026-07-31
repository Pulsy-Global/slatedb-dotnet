using NativeObjectStore = uniffi.slatedb.ObjectStore;

namespace Pulsy.SlateDB.Native;

internal sealed record SlateDbObjectStoreLocation(
    NativeObjectStore ObjectStore,
    string Path) : IDisposable
{
    public void Dispose() => ObjectStore.Dispose();
}
