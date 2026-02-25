using asec.Models;
using asec.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;

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

        public UploadController(AsecDBContext dbContext, IConfiguration config, IMinioClient minioClient)
        {
            _dbContext = dbContext;
            _minioClient = minioClient;
            _minioArtefactBucket = config.GetSection("ObjectStorage").GetValue<string>("ArtefactBucket");
            CreateDirectory(config.GetSection("Digitalization").GetValue<string>("ProcessBaseDir"));
        }

        /// <summary>
        /// Uploads new artefact.
        /// </summary>
        /// <param name="artefact">Details of the artefact</param>
        /// <param name="file">File to be uploaded</param>
        /// <returns>The uploaded artefact</returns>
        [HttpPost("artefact")]
        [Produces(typeof(ViewModels.Artefact))]
        public async Task<IActionResult> UploadArtefact([FromBody] ViewModels.Artefact artefact, [FromForm] IFormFile file)
        {
            Guid objectId = Guid.NewGuid();
            string filePath = Path.Combine(_uploadPath, objectId.ToString());
            Directory.CreateDirectory(filePath);
            using (FileStream fileStream = new FileStream(Path.Combine(filePath, file.Name), FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            var args = new PutObjectArgs()
                .WithFileName(file.Name)
                .WithBucket(_minioArtefactBucket)
                .WithObject(objectId.ToString());
            var artefactObject = await _minioClient.PutObjectAsync(args);

            var dbArtefact = await artefact.ToDBEntity(_dbContext);
            dbArtefact.ObjectId = objectId;
            dbArtefact.Type = Models.Digitalization.ArtefactType.ZipArchive;
            dbArtefact.FileName = file.Name;
            dbArtefact.ArchivationDate = DateTime.Now;
            dbArtefact.PhysicalMediaType = Models.Digitalization.PhysicalMediaType.None;
            await _dbContext.DigitalObjects.AddAsync(dbArtefact);
            await _dbContext.SaveChangesAsync();

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
