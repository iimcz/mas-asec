using asec.Models.Digitalization;

namespace asec.Digitalization;

public record DigitalizationResult(
    string Filename,
    ArtefactType Type
);