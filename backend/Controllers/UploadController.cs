using asec.LongRunning;
using asec.Models;
using asec.Upload;
using asec.ViewModels;
using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Minio;
using Minio.DataModel.Args;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace asec.Controllers
{
    [ApiController]
    [Route("/api/v1/upload")]
    public class UploadController : Controller
    {
        private readonly AsecDBContext _dbContext;
        private string _uploadPath;
        private readonly IMinioClient _minioClient;
        private readonly string _minioArtefactBucket;
        private readonly IProcessManager<Process, UploadResult> _processManager;

        public UploadController(AsecDBContext dbContext, IConfiguration config, IMinioClient minioClient, IProcessManager<Process, UploadResult> processManager)
        {
            _processManager = processManager;
            _dbContext = dbContext;
            _minioClient = minioClient;
            _minioArtefactBucket = config.GetSection("ObjectStorage").GetValue<string>("ArtefactBucket");
            CreateDirectory(config.GetSection("Digitalization").GetValue<string>("ProcessBaseDir"));
        }

        /// <summary>
        /// Start the process of uploading new artefact.
        /// </summary>
        /// <param name="VersionId">Get VersionId from request header</param>
        /// <param name="ParatextId">Get ParatextId from request header</param>
        /// <param name="file">File to be uploaded</param>
        /// <returns>Details of the started upload process</returns>
        [HttpPost("start")]
        [Produces(typeof(UploadProcess))]
        public async Task<IActionResult> StartUploadProcess()
        {
            Request.Headers.TryGetValue("VersionId", out var versionId);
            Models.Archive.WorkVersion version = null;
            if (!versionId.IsNullOrEmpty()) version = await _dbContext.WorkVersions.FindAsync(Guid.Parse(versionId.ToString()));
            Request.Headers.TryGetValue("ParatextId", out var paratextId);
            Models.Archive.Paratext paratext = null;
            if (!paratextId.IsNullOrEmpty()) paratext = await _dbContext.Paratexts.FindAsync(Guid.Parse(paratextId.ToString()));
            if (version == null && paratext == null) return NotFound();

            var boundary = HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue.Parse(Request.ContentType).Boundary).Value;
            var process = new Process(version, paratext, _uploadPath, boundary, Request.Body);
            _processManager.StartProcess(process);

            return Ok(UploadProcess.FromProcess(process));
        }


        /// <summary>
        /// Finalize upload process to save the results and create the artefact <see cref="Artefact"/>.
        /// </summary>
        /// <param name="processId">ID of the process to finalize</param>
        /// <param name="artefact">Details to include in the created artefact</param>
        /// <returns>The created artefact</returns>
        [HttpPost("{processId}/finalize")]
        [Produces(typeof(Artefact))]
        public async Task<IActionResult> FInalizeArtefactUpload(string processId, [FromBody] Artefact artefact)
        {
            var id = Guid.Parse(processId);
            var process = _processManager.GetProcess(id);
            if (process == null) return NotFound();

            var processResult = await _processManager.FinishProcessAsync(id);

            var objectId = Guid.NewGuid();
            var args = new PutObjectArgs()
                .WithFileName(processResult.Filename)
                .WithBucket(_minioArtefactBucket)
                .WithObject(objectId.ToString());
            var artefactObject = await _minioClient.PutObjectAsync(args);

            var dbArtefact = await artefact.ToDBEntity(_dbContext);
            dbArtefact.ObjectId = objectId;
            dbArtefact.Type = processResult.Type;
            dbArtefact.FileName = Path.GetFileName(processResult.Filename);
            dbArtefact.ArchivationDate = process.StartTime;
            dbArtefact.PhysicalMediaType = Models.Digitalization.PhysicalMediaType.None;

            var paratext = await _dbContext.Paratexts.FindAsync(process.ParatextId);
            var version = await _dbContext.WorkVersions.FindAsync(process.VersionId);
            dbArtefact.Paratexts = new List<Models.Archive.Paratext>();
            dbArtefact.Versions = new List<Models.Archive.WorkVersion>();
            if (paratext is not null) dbArtefact.Paratexts.Append(paratext);
            if (version is not null) dbArtefact.Versions.Append(version);

            await _dbContext.DigitalObjects.AddAsync(dbArtefact);
            await _dbContext.SaveChangesAsync();
            _processManager.RemoveProcess(process);

            return Ok(Artefact.FromDBEntity(dbArtefact));
        }

        private void CreateDirectory(string path)
        {
            var uploadPath = Path.Combine(path, "artefacts");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
            _uploadPath = uploadPath;
        }
    }
}
