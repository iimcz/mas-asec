using asec.Configuration;
using asec.Models;
using asec.Models.Archive;
using asec.Models.Digitalization;
using asec.Platforms;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.DataModel.Tags;
using System.Text;
using System.Text.Json;
using tusdotnet;
using tusdotnet.Interfaces;
using tusdotnet.Models;

namespace asec.Extensions;

public static class TusUploadExtensions
{
    public sealed record DigitalObjectUploadMetadata
    {
        public string Label { get; init; }
        public string Version { get; init; }
        public string FileName { get; init; }
        public DigitalObjectType DigitalObjectType { get; init; }

        // Optional metadata for GameArtefact uploads
        public ArtefactUploadMetadata ArtefactMetadata { get; init; }
    }

    public sealed record ArtefactUploadMetadata
    {
        public ArtefactType ArtefactType { get; init; }
        public Guid WorkVersion { get; init; } // TODO: Multiple work versions (game collections)?
    }

    public sealed record Services(
        AsecDBContext DbContext,
        IMinioClient MinioClient,
        IOptions<LocalObjectStorageConfiguration> StorageConfiguration
    );

    public static WebApplication MapTusUpload(this WebApplication app)
    {
        app.MapTus("/api/v1/digitalobjects/upload", async httpContext =>
        {
            var services = new Services(
                httpContext.RequestServices.GetRequiredService<AsecDBContext>(),
                httpContext.RequestServices.GetRequiredKeyedService<IMinioClient>("LocalObjectStorage"),
                httpContext.RequestServices.GetRequiredService<IOptions<LocalObjectStorageConfiguration>>()
            );

            return new DefaultTusConfiguration
            {
                Store = new tusdotnet.Stores.TusDiskStore(services.StorageConfiguration.Value.CacheDir),
                AllowedExtensions = TusExtensions.All.Except(TusExtensions.Termination),
                Events = new()
                {
                    OnBeforeCreateAsync = async context =>
                    {
                        if (!context.Metadata.TryGetValue("digitalObjectInfo", out var metadata))
                        {
                            context.FailRequest("Missing required metadata: digitalObjectInfo");
                            return;
                        }

                        try
                        {
                            var digitalObjectInfo = JsonSerializer.Deserialize<DigitalObjectUploadMetadata>(metadata.GetString(Encoding.UTF8));
                            if (digitalObjectInfo == null)
                            {
                                context.FailRequest("Missing metadata for digitalObjectInfo");
                                return;
                            }

                            context.HttpContext.Items["digitalObjectInfo"] = digitalObjectInfo;

                            if (digitalObjectInfo.DigitalObjectType == DigitalObjectType.GameArtefact)
                            {
                                if (digitalObjectInfo.ArtefactMetadata == null)
                                {
                                    context.FailRequest("Missing required metadata: artefactMetadata for GameArtefact");
                                    return;
                                }

                                var workVersion = await services.DbContext.WorkVersions.FindAsync(digitalObjectInfo.ArtefactMetadata.WorkVersion);
                                context.HttpContext.Items["workVersion"] = workVersion;

                                if (workVersion == null)
                                {
                                    context.FailRequest($"WorkVersion with ID {digitalObjectInfo.ArtefactMetadata.WorkVersion} not found");
                                    return;
                                }
                            }
                        }
                        catch (JsonException)
                        {
                            context.FailRequest("Invalid metadata format for digitalObjectInfo");
                            return;
                        }
                    },
                    OnFileCompleteAsync = async context =>
                    {
                        var digitalObjectInfo = (DigitalObjectUploadMetadata)context.HttpContext.Items["digitalObjectInfo"]!;
                        var workVersion = (WorkVersion)context.HttpContext.Items["workVersion"]!;
                        services.DbContext.Attach(workVersion);

                        var file = await context.GetFileAsync();

                        if (digitalObjectInfo.DigitalObjectType == DigitalObjectType.GameArtefact) await CreateArtefact(file, digitalObjectInfo, workVersion, services);
                        else await CreateDigitalObject(file, digitalObjectInfo, services);

                        if (context.Store is ITusTerminationStore terminationStore)
                        {
                            await terminationStore.DeleteFileAsync(file.Id, context.CancellationToken);
                        }
                    }
                }
            };
        });

        return app;
    }

    private static async Task CreateArtefact(ITusFile file, DigitalObjectUploadMetadata digitalObjectInfo, WorkVersion workVersion, Services services)
    {
        var fileContent = await file.GetContentAsync(default);

        var artefact = new Artefact
        {
            Label = digitalObjectInfo.Label,
            Version = digitalObjectInfo.Version,
            FileName = digitalObjectInfo.FileName,
            DigitalObjectType = DigitalObjectType.GameArtefact,
            Format = "", // TODO: Grab from CA?
            FileSize = fileContent.Length,
            MediaInfoReport = await Linux.MediaInfo(["--Output=JSON", digitalObjectInfo.FileName]),
            ObjectId = await UploadToStorage(fileContent, digitalObjectInfo, services),
            ArchivationDate = DateTime.Now,
            Type = digitalObjectInfo.ArtefactMetadata.ArtefactType,
            PhysicalMediaType = PhysicalMediaType.None,
            WorkVersions = [workVersion]
        };

        services.DbContext.DigitalObjects.Add(artefact);
        await services.DbContext.SaveChangesAsync();
    }

    private static async Task CreateDigitalObject(ITusFile file, DigitalObjectUploadMetadata digitalObjectInfo, Services services)
    {
        var fileContent = await file.GetContentAsync(default);

        var digitalObject = new DigitalObject
        {
            Label = digitalObjectInfo.Label,
            Version = digitalObjectInfo.Version,
            FileName = digitalObjectInfo.FileName,
            DigitalObjectType = DigitalObjectType.GameArtefact,
            Format = "", // TODO: Grab from CA?
            FileSize = fileContent.Length,
            MediaInfoReport = await Linux.MediaInfo(["--Output=JSON", digitalObjectInfo.FileName]),
            ObjectId = await UploadToStorage(fileContent, digitalObjectInfo, services),
        };

        services.DbContext.DigitalObjects.Add(digitalObject);
        await services.DbContext.SaveChangesAsync();
    }

    private static async Task<Guid> UploadToStorage(Stream fileStream, DigitalObjectUploadMetadata digitalObjectInfo, Services services)
    {
        var objectId = Guid.NewGuid();
        var tags = new Dictionary<string, string>()
        {
            { "Tag", digitalObjectInfo.DigitalObjectType.ToString() },
            { "DataType", "File" }
        };

        var folderName = digitalObjectInfo.DigitalObjectType switch
        {
            DigitalObjectType.GameArtefact => services.StorageConfiguration.Value.ArtefactFolder,
            DigitalObjectType.PlayableObject => services.StorageConfiguration.Value.PlayableFolder,
            DigitalObjectType.Modification => services.StorageConfiguration.Value.ModificationFolder,
            DigitalObjectType.UnplayableParatext => services.StorageConfiguration.Value.ParatextFolder,
            _ => throw new NotImplementedException("Unknown DigitalObjectType: " + digitalObjectInfo.DigitalObjectType)
        };

        var args = new PutObjectArgs()
                .WithStreamData(fileStream)
                .WithBucket(services.StorageConfiguration.Value.DigitalObjectBucket)
                .WithTagging(new Tagging(tags, true))
                .WithObject($"{folderName}/{objectId}");

        await services.MinioClient.PutObjectAsync(args);
        return objectId;
    }
}