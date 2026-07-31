using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pulsy.SlateDB.Options;

internal sealed class DurationJsonConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) =>
        throw new NotSupportedException();

    public override void Write(
        Utf8JsonWriter writer,
        TimeSpan value,
        JsonSerializerOptions options)
    {
        var totalMs = value.TotalMilliseconds;
        var serialized = totalMs % 1000 == 0
            ? ((long)(totalMs / 1000)).ToString(CultureInfo.InvariantCulture) + "s"
            : ((long)totalMs).ToString(CultureInfo.InvariantCulture) + "ms";
        writer.WriteStringValue(serialized);
    }
}
