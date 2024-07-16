using System.Text.Json.Serialization;

namespace asec.Digitalization.Tools;

[JsonDerivedType(typeof(GreaseweazleToolConfig), typeDiscriminator: "greaseweazle")]
public abstract class DigitalizationToolConfig
{
    public string Id { get; set; }
    public abstract IDigitalizationTool ConstructTool();
}