using System.Text.Json.Serialization;

namespace asec.DataConversion.Converters;

[JsonDerivedType(typeof(FloppyConverterConfig), "floppy")]
public abstract class ConverterConfig
{
    public abstract IConverter ConstructConverter();
}