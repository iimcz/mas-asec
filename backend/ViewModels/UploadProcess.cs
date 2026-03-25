namespace asec.ViewModels
{
    public record UploadProcess
    {
        public string ProcessId { get; set; } = "";
        public string Status { get; set; } = "";
        public string StatusDetail { get; set; } = "";
        public string StartTime { get; set; } = "";

        public static UploadProcess FromProcess(Upload.Process process)
        {
            return new UploadProcess
            {
                ProcessId = process.Id.ToString(),
                Status = process.Status.ToString(),
                StatusDetail = process.StatusDetail,
                StartTime = process.StartTime.ToString()
            };
        }
    }
}
