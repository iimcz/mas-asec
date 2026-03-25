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
            FileStream = fileStream;

            BaseDir = Path.Combine(dirsBase, Id.ToString());
            LogPath = Path.Combine(BaseDir, "log.txt");

            Directory.CreateDirectory(BaseDir);
        }

        public async Task<UploadResult> Start(CancellationToken cancellationToken)
        {
            StartTime = DateTime.Now;
            CancellationToken = cancellationToken;
            var path = Path.Combine(BaseDir, Guid.NewGuid().ToString());
            Status = ProcessStatus.Running;
            await SaveMultipartStream(path);
            Status = ProcessStatus.Success;
            return new(path, ArtefactType.ZipArchive);
        }

        private async Task SaveMultipartStream(string path)
        {
            using FileStream output = new FileStream(
                path: path,
                mode: FileMode.Create,
                access: FileAccess.Write,
                share: FileShare.None,
                bufferSize: BufferSize,
                useAsync: true
            );

            var reader = new MultipartReader(Boundary, FileStream);
            MultipartSection? section;
            long totalBytesRead = 0;

            while ((section = await reader.ReadNextSectionAsync(CancellationToken)) != null)
            {
                var contentDisposition = section.GetContentDispositionHeader();
                if (contentDisposition != null && contentDisposition.IsFileDisposition())
                {
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
        }
    }
}
