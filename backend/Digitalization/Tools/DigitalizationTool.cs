using asec.Models.Digitalization;

namespace asec.Digitalization.Tools;

public abstract class DigitalizationTool : IDigitalizationTool
{
    public static DigitalizationToolDescription ToolDescription
        => new();
}