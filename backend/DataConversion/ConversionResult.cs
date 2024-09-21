namespace asec.DataConversion;

public record ConvertedFile(
    string Filename,
    string Type
);

public record ConversionResult(
    IList<ConvertedFile> Files
);