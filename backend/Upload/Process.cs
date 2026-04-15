using asec.LongRunning;
using Microsoft.AspNetCore.WebUtilities;

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

            Status = ProcessStatus.Running;
            var path = await SaveMultipartStream();
            Status = ProcessStatus.Success;

            return new(path);
        }

        private async Task<string> SaveMultipartStream()
        {
            var reader = new MultipartReader(Boundary, FileStream);
            var path = "";
            MultipartSection section;

            while ((section = await reader.ReadNextSectionAsync(CancellationToken)) != null)
            {
                var fileSection = section.AsFileSection();
                if (fileSection == null) continue;

                path = Path.Combine(BaseDir, fileSection.FileName);

                using FileStream output = new(
                    path: path,
                    mode: FileMode.Create,
                    access: FileAccess.Write,
                    share: FileShare.None,
                    bufferSize: BufferSize,
                    useAsync: true
                );

                await section.Body.CopyToAsync(output, CancellationToken);
            }

            return path;
        }

        public Task Cleanup(CancellationToken cancellationToken = default)
        {
            Directory.Delete(BaseDir, true);
            return Task.CompletedTask;
        }
    }
}
