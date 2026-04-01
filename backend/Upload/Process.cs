using asec.LongRunning;
using asec.Models.Digitalization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace asec.Upload
{
    public class Process : IProcess<UploadResult>
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid VersionId { get; private set; } = Guid.Empty;
        public Guid ParatextId { get; private set; } = Guid.Empty;
        public CancellationToken CancellationToken { get; private set; }
        public DateTime StartTime { get; private set; }
        public string BaseDir { get; private set; }
        public string LogPath { get; private set; }
        public ProcessStatus Status { get; set; }
        public string StatusDetail { get; set; }
        public Stream FileStream { get; set; }
        public string Boundary { get; set; }

        private const int BufferSize = 16 * 1024 * 1024;

        public Process(Models.Archive.WorkVersion version, Models.Archive.Paratext paratext, string dirsBase, string boundary, Stream fileStream)
        {
            VersionId = version?.Id ?? Guid.Empty;
            ParatextId = paratext?.Id ?? Guid.Empty;

            Boundary = boundary;

            var hackMem = new MemoryStream();
            fileStream.CopyToAsync(hackMem).Wait();
            hackMem.Position = 0;
            FileStream = hackMem;

            BaseDir = Path.Combine(dirsBase, Id.ToString());
            LogPath = Path.Combine(BaseDir, "log.txt");

            Directory.CreateDirectory(BaseDir);
        }

        public async Task<UploadResult> Start(CancellationToken cancellationToken)
        {
            StartTime = DateTime.Now;
            CancellationToken = cancellationToken;
            Status = ProcessStatus.Running;
            var path = await SaveMultipartStream();
            Status = ProcessStatus.Success;

            // TODO: The type is ignored for and set during finalize as it's user input
            return new(path, ArtefactType.ZipArchive);
        }

        private async Task<string> SaveMultipartStream()
        {
            var reader = new MultipartReader(Boundary, FileStream);
            var path = "";
            MultipartSection section;
            long totalBytesRead = 0;

            while ((section = await reader.ReadNextSectionAsync(CancellationToken)) != null)
            {
                var contentDisposition = section.GetContentDispositionHeader();
                if (contentDisposition != null && contentDisposition.IsFileDisposition())
                {
                    path = contentDisposition.FileName.ToString();
                    // TODO: Splitting files in multiple is not common, if we want to do this for some reason we need to discuss it
                    using FileStream output = new FileStream(
                        path: path,
                        mode: FileMode.Create,
                        access: FileAccess.Write,
                        share: FileShare.None,
                        bufferSize: BufferSize,
                        useAsync: true
                    );

                    await section.Body.CopyToAsync(output, CancellationToken);
                    totalBytesRead += section.Body.Length;
                }
                else if (contentDisposition != null && contentDisposition.IsFormDisposition())
                {
                    string key = contentDisposition.Name.Value!;
                    using var streamReader = new StreamReader(section.Body);
                    string value = await streamReader.ReadToEndAsync(CancellationToken);
                }
            }

            return path;
        }
    }
}
