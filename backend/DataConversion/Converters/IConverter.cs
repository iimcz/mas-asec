using asec.Models.Digitalization;
using asec.Models.Emulation;

namespace asec.DataConversion.Converters;

public interface IConverter
{
    Guid Id { get; set; }
    string Name { get; }
    string Version { get; }
    string Environment { get; }
    IList<ArtefactType> SupportedArtefactTypes { get; }

    Task<ConversionResult> Start(Process process, CancellationToken cancellationToken);
    bool EqualsToDB(Converter converter)
        => Name.Equals(converter.Name) && Version.Equals(converter.Version) && Environment.Equals(converter.Environment);
}