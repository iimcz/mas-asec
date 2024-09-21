namespace asec.ViewModels;

public record EmulationFinishRequest(
    bool KeepScreenRecording,
    bool KeepWebcamRecording,
    bool SaveMachineState
);