namespace asec.ViewModels
{
    public record Processes
    {
        public List<DigitalizationProcess> digitalizationProcesses { get; set; } = new();
        public List<ConversionProcess> conversionProcesses { get; set; } = new();
        public List<EmulationProcess> emulationProcesses { get; set; } = new();
        public List<UploadProcess> uploadProcesses { get; set; } = new();
        public List<ExplorationProcess> explorationProcesses { get; set; } = new();
    }
}
