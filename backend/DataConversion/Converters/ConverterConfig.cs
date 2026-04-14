using System.Text.Json.Serialization;

namespace asec.DataConversion.Converters;

[JsonDerivedType(typeof(FloppyConverterConfig), "floppy")]
[JsonDerivedType(typeof(AudioTapeConverterConfig), "audiotape")]
[JsonDerivedType(typeof(CopyConverterConfig), "copy")]
public abstract class ConverterConfig
{
    public abstract IConverter ConstructConverter();
}
