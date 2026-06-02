namespace asec.ViewModels;

public record Process<TDetail>
{
    public string Id { get; init; }
    public string Status { get; init; }
    public TDetail StatusDetail { get; init; }
    public string StartTime { get; init; }
}
