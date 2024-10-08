using System.Text.Json.Serialization;

namespace asec.Compatibility.EaasApi.Models;

public record MachineComponentRequest(
    string environment,
    // HACK: the commented out properties are optional, right now we don't need them.
    //EnvironmentCreateRequest environmentConfig,
    //[property: JsonPropertyName("object")] string object_,
    //string software,
    //bool lockEnvironment,
    //string nic,
    //bool headless,
    //int sessionLifetime,
    //LinuxRuntimeContainerReq linuxRuntimeData,
    //bool hasOutput,
    //string outputDriveId,
    //List<UserMedium> userMedia,
    List<Drive> drives,
    //string keyboardLayout = "us",
    //string keyboardModel = "pc105",
    string archive = "default"
    //string objectArchive = "default",
    //string emulatorVersion = "latest"
) : ComponentRequest("machine");