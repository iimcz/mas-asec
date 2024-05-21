using asec.Models.Digitalization;

namespace asec.Digitalization;

public interface IDigitalizationToolRepo
{
    public List<DigitalizationToolDescription> GetDigitalizationTools();
    public IDigitalizationTool BuildDigitalizationTool(DigitalizationToolDescription description);
}