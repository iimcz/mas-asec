namespace asec.ViewModels;

public record UploadDetail(
);

public record UploadProcess : Process<UploadDetail>
{
    public static UploadProcess FromProcess(Upload.Process process)
    {
        return new UploadProcess
        {
            Id = process.Id.ToString(),
            Status = process.Status.ToString(),
            StartTime = process.StartTime.ToString(),
            StatusDetail = new()
        };
    }
}
