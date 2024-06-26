namespace asec.Compatibility.RemoteLLMApi.Models;

public record ScrapeRequest(
    string name,
    string[] sources
);