using System.Runtime.InteropServices;

namespace Pulsy.SlateDB.Native;

internal static partial class NativeMethods
{
    // --- Logging & Settings ---

    [LibraryImport("slatedb_c")]
    internal static partial CSdbResult slatedb_init_logging(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? level);

    [LibraryImport("slatedb_c")]
    internal static partial nint slatedb_settings_default();

    [LibraryImport("slatedb_c")]
    internal static partial nint slatedb_settings_from_file(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path);

    [LibraryImport("slatedb_c")]
    internal static partial nint slatedb_settings_from_env(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string prefix);

    [LibraryImport("slatedb_c")]
    internal static partial nint slatedb_settings_load();

    // --- Database lifecycle ---

    [LibraryImport("slatedb_c")]
    internal static partial CSdbHandleResult slatedb_open(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? url,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? envFile);

    [LibraryImport("slatedb_c")]
    internal static partial CSdbResult slatedb_close(CSdbHandle handle);

    [LibraryImport("slatedb_c")]
    internal static partial CSdbResult slatedb_flush(CSdbHandle handle);

    // --- Put / Delete / Get ---

    [LibraryImport("slatedb_c")]
    internal static unsafe partial CSdbResult slatedb_put_with_options(
        CSdbHandle handle,
        byte* key, nuint keyLen,
        byte* value, nuint valueLen,
        CSdbPutOptions* putOptions,
        CSdbWriteOptions* writeOptions);

    [LibraryImport("slatedb_c")]
    internal static unsafe partial CSdbResult slatedb_delete_with_options(
        CSdbHandle handle,
        byte* key, nuint keyLen,
        CSdbWriteOptions* writeOptions);

    [LibraryImport("slatedb_c")]
    internal static unsafe partial CSdbResult slatedb_get_with_options(
        CSdbHandle handle,
        byte* key, nuint keyLen,
        CSdbReadOptions* readOptions,
        CSdbValue* valueOut);

    // --- Scan ---

    [LibraryImport("slatedb_c")]
    internal static unsafe partial CSdbResult slatedb_scan_with_options(
        CSdbHandle handle,
        byte* startKey, nuint startKeyLen,
        byte* endKey, nuint endKeyLen,
        CSdbScanOptions* scanOptions,
        nint* iteratorPtr);

    [LibraryImport("slatedb_c")]
    internal static unsafe partial CSdbResult slatedb_scan_prefix_with_options(
        CSdbHandle handle,
        byte* prefix, nuint prefixLen,
        CSdbScanOptions* scanOptions,
        nint* iteratorPtr);

    // --- Metrics ---

    [LibraryImport("slatedb_c")]
    internal static unsafe partial CSdbResult slatedb_metrics(
        CSdbHandle handle,
        CSdbValue* valueOut);

    // --- WriteBatch ---

    [LibraryImport("slatedb_c")]
    internal static unsafe partial CSdbResult slatedb_write_batch_new(nint* batchOut);

    [LibraryImport("slatedb_c")]
    internal static unsafe partial CSdbResult slatedb_write_batch_put(
        nint batch,
        byte* key, nuint keyLen,
        byte* value, nuint valueLen);

    [LibraryImport("slatedb_c")]
    internal static unsafe partial CSdbResult slatedb_write_batch_put_with_options(
        nint batch,
        byte* key, nuint keyLen,
        byte* value, nuint valueLen,
        CSdbPutOptions* options);

    [LibraryImport("slatedb_c")]
    internal static unsafe partial CSdbResult slatedb_write_batch_delete(
        nint batch,
        byte* key, nuint keyLen);

    [LibraryImport("slatedb_c")]
    internal static unsafe partial CSdbResult slatedb_write_batch_write(
        CSdbHandle handle,
        nint batch,
        CSdbWriteOptions* options);

    [LibraryImport("slatedb_c")]
    internal static partial CSdbResult slatedb_write_batch_close(nint batch);

    // --- Builder ---

    [LibraryImport("slatedb_c")]
    internal static partial CSdbBuilderResult slatedb_builder_new(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? url,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? envFile);

    [LibraryImport("slatedb_c")]
    internal static partial CSdbResult slatedb_builder_with_settings(
        nint builder,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string settingsJson);

    [LibraryImport("slatedb_c")]
    internal static partial CSdbResult slatedb_builder_with_sst_block_size(
        nint builder, byte size);

    [LibraryImport("slatedb_c")]
    internal static partial CSdbHandleResult slatedb_builder_build(nint builder);

    [LibraryImport("slatedb_c")]
    internal static partial void slatedb_builder_free(nint builder);

    // --- Reader ---

    [LibraryImport("slatedb_c")]
    internal static unsafe partial CSdbReaderHandleResult slatedb_reader_open(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string url,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? envFile,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? checkpointId,
        CSdbReaderOptions* readerOptions);

    [LibraryImport("slatedb_c")]
    internal static unsafe partial CSdbResult slatedb_reader_get_with_options(
        CSdbReaderHandle handle,
        byte* key, nuint keyLen,
        CSdbReadOptions* readOptions,
        CSdbValue* valueOut);

    [LibraryImport("slatedb_c")]
    internal static unsafe partial CSdbResult slatedb_reader_scan_with_options(
        CSdbReaderHandle handle,
        byte* startKey, nuint startKeyLen,
        byte* endKey, nuint endKeyLen,
        CSdbScanOptions* scanOptions,
        nint* iteratorPtr);

    [LibraryImport("slatedb_c")]
    internal static unsafe partial CSdbResult slatedb_reader_scan_prefix_with_options(
        CSdbReaderHandle handle,
        byte* prefix, nuint prefixLen,
        CSdbScanOptions* scanOptions,
        nint* iteratorPtr);

    [LibraryImport("slatedb_c")]
    internal static partial CSdbResult slatedb_reader_close(CSdbReaderHandle handle);

    // --- Iterator ---

    [LibraryImport("slatedb_c")]
    internal static unsafe partial CSdbResult slatedb_iterator_next(
        nint iter,
        CSdbKeyValue* kvOut);

    [LibraryImport("slatedb_c")]
    internal static unsafe partial CSdbResult slatedb_iterator_seek(
        nint iter,
        byte* key, nuint keyLen);

    [LibraryImport("slatedb_c")]
    internal static partial CSdbResult slatedb_iterator_close(nint iter);

    // --- Memory management ---

    [LibraryImport("slatedb_c")]
    internal static partial void slatedb_free_result(CSdbResult result);

    [LibraryImport("slatedb_c")]
    internal static partial void slatedb_free_value(CSdbValue value);
}
