using asec.DataConversion.Converters;
using asec.Models.Digitalization;
using asec.Models.Emulation;

namespace asec.Emulation;

public interface IEmulatorRepository : IHostedService
{
    Task<Emulator> GetEmulatorAsync(Guid id);
    Task<EmulationEnvironment> GetEnvironmentAsync(Guid id);
    Task<IEnumerable<IConverter>> GetEnvironmentConvertersAsync(Guid environmentId);
    Task<IConverter> GetEnvironmentConverterAsync(Guid environmentId, ArtefactType sourceType);
}