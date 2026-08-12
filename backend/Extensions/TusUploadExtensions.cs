using asec.Configuration;
using asec.Models;
using asec.Models.Archive;
using asec.Models.Digitalization;
using asec.Platforms;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.DataModel.Tags;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using tusdotnet;
using tusdotnet.Interfaces;
using tusdotnet.Models;

namespace asec.Extensions;

public static class TusUploadExtensions
{
    private static JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(new PascalCaseNamingPolicy()) }
    };

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

            var logger = httpContext.RequestServices.GetRequiredService<ILogger<Program>>();

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
                            var digitalObjectInfo = JsonSerializer.Deserialize<DigitalObjectUploadMetadata>(metadata.GetString(Encoding.UTF8), _serializerOptions);
                            if (digitalObjectInfo == null)
                            {
                                context.FailRequest("Missing metadata for digitalObjectInfo");
                                return;
                            }

                            if (digitalObjectInfo.DigitalObjectType == DigitalObjectType.GameArtefact)
                            {
                                if (digitalObjectInfo.ArtefactMetadata == null)
                                {
                                    context.FailRequest("Missing required metadata: artefactMetadata for GameArtefact");
                                    return;
                                }

                                var workVersion = await services.DbContext.WorkVersions.FindAsync(digitalObjectInfo.ArtefactMetadata.WorkVersion);
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
                        var file = await context.GetFileAsync();
                        var metadata = await file.GetMetadataAsync(context.CancellationToken);
                        var digitalObjectInfo = JsonSerializer.Deserialize<DigitalObjectUploadMetadata>(metadata["digitalObjectInfo"].GetString(Encoding.UTF8), _serializerOptions)!;

                        if (digitalObjectInfo.DigitalObjectType == DigitalObjectType.GameArtefact)
                        {
                            var workVersion = await services.DbContext.WorkVersions.FindAsync(digitalObjectInfo.ArtefactMetadata.WorkVersion);
                            await CreateArtefact(file, digitalObjectInfo, workVersion, services);
                        }
                        else
                        {
                            await CreateDigitalObject(file, digitalObjectInfo, services);
                        }

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
        var filePath = Path.Combine(services.StorageConfiguration.Value.CacheDir, file.Id);

        var artefact = new Artefact
        {
            Label = digitalObjectInfo.Label,
            Version = digitalObjectInfo.Version,
            FileName = digitalObjectInfo.FileName,
            DigitalObjectType = DigitalObjectType.GameArtefact,
            Format = "", // TODO: Grab from CA?
            FileSize = fileContent.Length,
            MediaInfoReport = await Linux.MediaInfo(["--Output=JSON", filePath]),
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
        var filePath = Path.Combine(services.StorageConfiguration.Value.CacheDir, file.Id);

        var digitalObject = new DigitalObject
        {
            Label = digitalObjectInfo.Label,
            Version = digitalObjectInfo.Version,
            FileName = digitalObjectInfo.FileName,
            DigitalObjectType = digitalObjectInfo.DigitalObjectType,
            Format = "", // TODO: Grab from CA?
            FileSize = fileContent.Length,
            MediaInfoReport = await Linux.MediaInfo(["--Output=JSON", filePath]),
            ObjectId = await UploadToStorage(fileContent, digitalObjectInfo, services),
        };

        services.DbContext.DigitalObjects.Add(digitalObject);
        await services.DbContext.SaveChangesAsync();
    }

    private static async Task<string> UploadToStorage(Stream fileStream, DigitalObjectUploadMetadata digitalObjectInfo, Services services)
    {
        var objectId = Guid.NewGuid().ToString();
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
                .WithObjectSize(fileStream.Length)
                .WithBucket(services.StorageConfiguration.Value.DigitalObjectBucket)
                .WithTagging(new Tagging(tags, true))
                .WithObject($"{folderName}/{objectId}");

        await services.MinioClient.PutObjectAsync(args);
        return objectId;
    }
}