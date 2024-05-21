using asec.Models.Digitalization;

namespace asec.Digitalization;

public class DigitalizationToolRepo : IDigitalizationToolRepo
{
    private IConfiguration _config;
    private Dictionary<string, Action<IDigitalizationTool>> _toolFactories;

    public IDigitalizationTool BuildDigitalizationTool(DigitalizationToolDescription description)
    {
        throw new NotImplementedException();
    }

    public List<DigitalizationToolDescription> GetDigitalizationTools()
    {
        throw new NotImplementedException();
    }
}