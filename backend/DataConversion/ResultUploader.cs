using asec.Compatibility.EaasApi;
using asec.Compatibility.EaasApi.Models;
using asec.Platforms;

namespace asec.DataConversion;

public class ResultUploader
{
    private readonly EaasUploadClient _eaasUploadClient;
    private readonly EnvironmentRepositoryClient _eaasEnvironmentRepoClient;
    private readonly ObjectRepositoryClient _eaasObjectRepoClient;
    private readonly ILogger _logger;
    private readonly string _baseDirectory;

    public ResultUploader(EaasUploadClient eaasUploadClient, ObjectRepositoryClient eaasObjectRepoClient, EnvironmentRepositoryClient eaasEnvironmentRepoClient, string baseDirectory, ILogger logger)
    {
        _eaasUploadClient = eaasUploadClient;
        _eaasEnvironmentRepoClient = eaasEnvironmentRepoClient;
        _eaasObjectRepoClient = eaasObjectRepoClient;
        _logger = logger;
        _baseDirectory = baseDirectory;
    }

    public async Task<string> UploadImageToEaaS(IList<ConvertedFile> files, string name, CancellationToken cancellationToken = default(CancellationToken))
    {
        var diskImagePath = await CreateDiskImageFromFiles(files, name, cancellationToken);
        return await UploadImageToEaaS(diskImagePath, name, cancellationToken);
    }

    public async Task<string> UploadImageToEaaS(string imgFile, string name, CancellationToken cancellationToken = default(CancellationToken))
    {
        _logger?.LogInformation("Uploading disk image to EaaS...");
        var eaasUploadResponse = await _eaasUploadClient.Upload(imgFile, cancellationToken);
        if (eaasUploadResponse.status != "0")
        {
            _logger?.LogError("Failed to upload disk image to EaaS:");
            _logger?.LogError("{}", eaasUploadResponse);
        }

        var importImage = new ImportImageRequest(
            eaasUploadResponse.uploadedItemList[0].url, name
        );

        _logger?.LogInformation("Marking the upload as disk image.");
        var eaasImageId = await _eaasEnvironmentRepoClient.ImportImage(importImage, cancellationToken);
        return eaasImageId;
    }

    public async Task<string> UploadFilesToEaaS(IList<ConvertedFile> files, string deviceId, string name, CancellationToken cancellationToken = default(CancellationToken))
    {
        // TODO: do better error checking
        var eaasUploadTasks = files.Select(f => _eaasUploadClient.Upload(f.Filename, cancellationToken));
        var eaasUploadResponses = await Task.WhenAll(eaasUploadTasks);
        if (!eaasUploadResponses.All(r => r.status == "0"))
        {
            _logger?.LogError("Failed to upload some objects to EaaS:");
            foreach (var resp in eaasUploadResponses)
                _logger?.LogError("{}", resp);
        }

        // TODO: use a proper file format (ImportFileInfo.fileFmt)
        var importObjects = eaasUploadResponses.SelectMany(
            r => r.uploadedItemList
        ).Select(
            uploaded => new ImportFileInfo(uploaded.url, deviceId, string.Empty, uploaded.filename)
        ).ToList();

        var eaasObjectId = await _eaasObjectRepoClient.ImportObjects(new(
            name,
            importObjects
        ), cancellationToken);

        return eaasObjectId;
    }

    private async Task<string> CreateDiskImageFromFiles(IList<ConvertedFile> files, string name, CancellationToken cancellationToken = default(CancellationToken))
    {
        // TODO: for now, this will only work on linux due to the filesystem used and the availability of the tools.
        // In the future, this could be extended to be at least configurable, maybe include some tool checks somewhere
        // and maybe have a version for other platforms as well.
        var imagePath = Path.Join(_baseDirectory, name + ".qcow2");
        var mountPath = Path.Join(_baseDirectory, name + "_mounted");
        Directory.CreateDirectory(mountPath);

        var imageSize = files.Select(f => new FileInfo(f.Filename).Length).Sum();
        var output = await Linux.MakeQcow2Image(imageSize, imagePath, FileSystem.Ext4, cancellationToken: cancellationToken);
        _logger?.LogInformation(output);

        output = await Linux.MountQcow2Image(imagePath, mountPath, cancellationToken);
        _logger?.LogInformation(output);

        foreach (var file in files)
        {
            System.IO.File.Move(file.Filename, Path.Join(mountPath, Path.GetFileName(file.Filename)));
        }

        output = await Linux.UnmountQcow2Image(mountPath, cancellationToken);
        _logger?.LogInformation(output);
        Directory.Delete(mountPath);

        return imagePath;
    }
}
