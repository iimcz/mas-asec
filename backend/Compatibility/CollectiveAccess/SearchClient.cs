using asec.Compatibility.CollectiveAccess.Models;

namespace asec.Compatibility.CollectiveAccess;

public class SearchClient : BaseCollectiveAccessClient
{
    private const string SEARCH_WORKS_QUERY = "query search($jwt:String,$search:String,$table:String,$types:[String],$bundles:[String]){search(jwt:$jwt,search:$search,table:$table,restrictToTypes:$types,bundles:$bundles){result{id,idno,bundles{code,dataType,name,values{id,locale,value}}}}}";
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
                Table = Tables.Occurrences,
                Types = new() { Types.Work },
                Bundles = new() { BundleCodes.OccurrenceLabel, BundleCodes.OccurrenceId, BundleCodes.OccurrenceCuratorialDescription }
            }
        };

        var result = await PostAuthenticatedAsync<SearchVars,SearchRoot<Work>>(ENDPOINT, request, cancellationToken);
        
        return result.Data.Search.Result;
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
        public IList<T> Result {get; set;}
    }

    public class SearchRoot<T>
    {
        public SearchResults<T> Search {get; set;}
    }
}
