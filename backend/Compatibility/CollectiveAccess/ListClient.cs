using asec.Compatibility.CollectiveAccess.Models;

namespace asec.Compatibility.CollectiveAccess;

public class ListClient : BaseCollectiveAccessClient
{
    private const string FIND_QUERY = """
        query find($jwt: String, $table: String, $criteria: [Criterion], $bundles: [String], $limit: Int)
        {
            find(jwt: $jwt, table: $table, criteria: $criteria, limit: $limit, bundles: $bundles)
            {
                count
                result
                {
                    id
                    idno
                    bundles { code, dataType, name, values { id, locale, value } }
                }
            }
        }
    """;
    private const string ENDPOINT = "service.php/search";

    public ListClient(IConfiguration configuration, IHttpClientFactory clientFactory, CollectiveAccessAuth auth)
        : base(configuration, clientFactory, auth)
    {
    }

    /// <summary>
    /// Fetches all list items for a given CA list by its numeric list_id.
    /// The root node (idno starting with "Root node for") is excluded.
    /// </summary>
    public async Task<IList<ListItem>> GetListItems(
        int listId,
        CancellationToken cancellationToken = default)
    {
        var request = new GraphQLRequest<FindVars>()
        {
            Query = FIND_QUERY,
            Variables = new()
            {
                Table = "ca_list_items",
                Criteria = new()
                {
                    new() { Name = "list_id", Value = listId.ToString() }
                },
                Bundles = new()
                {
                    BundleCodes.ListItemsPreferredLabel,
                    BundleCodes.ListItemsItemValue
                }
            }
        };

        var response = await PostAuthenticatedAsync<FindVars, FindRoot<ListItem>>(
            ENDPOINT, request, cancellationToken);

        // Filter out the root node
        return response.Data.Find.Result
            .Where(item => !item.Idno.StartsWith("Root node for"))
            .ToList();
    }

    public class Criterion
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class FindVars : GraphQLAuthVars
    {
        public string Table { get; set; }
        public List<Criterion> Criteria { get; set; }
        public List<string> Bundles { get; set; }
        public int? Limit { get; set; }
    }

    public class FindResults<T>
    {
        public int Count { get; set; }
        public IList<T> Result { get; set; }
    }

    public class FindRoot<T>
    {
        public FindResults<T> Find { get; set; }
    }
}
