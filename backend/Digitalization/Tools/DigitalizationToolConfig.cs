using System.Text.Json.Serialization;

namespace asec.Digitalization.Tools;

[JsonDerivedType(typeof(GreaseweazleToolConfig), typeDiscriminator: "greaseweazle")]
public abstract class DigitalizationToolConfig
{
    public string Slug { get; set; }
    public abstract IDigitalizationTool ConstructTool();
}