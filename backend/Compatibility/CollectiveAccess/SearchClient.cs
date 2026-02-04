using asec.Compatibility.CollectiveAccess.Models;

namespace asec.Compatibility.CollectiveAccess;

public class SearchClient : BaseCollectiveAccessClient
{
    private const string SEARCH_WORKS_QUERY = "";
    private const string ENDPOINT = "service.php/search";

    public SearchClient(IConfiguration configuration, IHttpClientFactory clientFactory, CollectiveAccessAuth auth) : base(configuration, clientFactory, auth)
    {
        
    }

    public async Task<IList<Work>> GetWorks(string searchTerm = null, CancellationToken cancellationToken = default(CancellationToken))
    {
        var request = new GraphQLRequest<SearchVars>() {
            Query = SEARCH_WORKS_QUERY,
            Variables = new() {
                Search = searchTerm ?? "*",
                Table = "ca_occurrences",
                Types = new() {"work"},
                Bundles = new() {"ca_occurrences.preferred_labels", "ca_occurrences.occurrence_id"}
            }
        };

        var result = await PostAuthenticatedAsync<SearchVars,SearchRoot<Work>>(ENDPOINT, request, cancellationToken);
        if (result == null || !result.Ok)
        {
            // TODO: better error handling and logging
            throw new ApplicationException(result?.Errors?.ToString());
        }
        
        return result.Data.Search.Results;
    }

    public class SearchVars : GraphQLAuthVars
    {
        // TODO: add start and count for pagination support
        public string Search {get; set;}
        public string Table {get; set;}
        public List<string> Types {get; set;}
        public List<string> Bundles {get; set;}
    }

    public class SearchResults<T>
    {
        public int Count {get; set;}
        public IList<T> Results {get; set;}
    }

    public class SearchRoot<T>
    {
        public SearchResults<T> Search {get; set;}
    }
}
