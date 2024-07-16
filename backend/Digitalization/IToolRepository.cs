using asec.Digitalization.Tools;
using asec.Models.Digitalization;

namespace asec.Digitalization;

public interface IToolRepository : IHostedService
{
    public IEnumerable<IDigitalizationTool> GetDigitalizationTools();
    public IDigitalizationTool GetDigitalizationTool(string toolId);
}