using asec.Compatibility.CollectiveAccess.Models;

namespace asec.Compatibility.CollectiveAccess;

public class ItemClient : BaseCollectiveAccessClient
{
    private const string GET_QUERY = "query get_workversion_relations($jwt:String,$id:Int,$table:String,$bundles:[String]){get(jwt:$jwt,id:$id,table:$table,bundles:$bundles){id,idno,bundles{code,dataType,name,values{id,locale,value}}}}";
    private const string RELATIONSHIPS_QUERY = "query get_rels($jwt:String,$table:String,$id:Int,$target:String,$tgTypes:[String],$relTypes:[String]){getRelationships(jwt:$jwt,id:$id,table:$table,target:$target,restrictToTypes:$tgTypes,restrictToRelationshipTypes:$relTypes){id,relationships{id}}}";

    private const string ENDPOINT = "service.php/item";

    public ItemClient(IConfiguration configuration, IHttpClientFactory clientFactory, CollectiveAccessAuth auth) : base(configuration, clientFactory, auth)
    {
    }

    private async Task<IList<MinRelationship>> GetWorkVersionRelationships(int id, CancellationToken cancellationToken = default(CancellationToken))
    {
        var request = new GraphQLRequest<GetRelationshipsArgs>() {
            Query = RELATIONSHIPS_QUERY,
            Variables = new() {
                Id = id,
                Table = "ca_occurrences",
                Target = "ca_occurrences",
                RelTypes = new() {"work_manifestation_workversion"},
                TgTypes = new() {"work_version"}
            }
        };

        var response = await PostAuthenticatedAsync<GetRelationshipsArgs, GetRelationshipsRoot>(ENDPOINT, request, cancellationToken);
        if (response == null || !response.Ok)
        {
            // TODO: better error handling and logging
            throw new ApplicationException(response?.Errors?.ToString());
        }

        return response.Data.GetRelationships.Relationships;
    }

    public async Task<int> GetVersionCountForWork(Work work, CancellationToken cancellationToken = default(CancellationToken))
        => await GetVersionCountForWork(work.Id, cancellationToken);
    public async Task<int> GetVersionCountForWork(int id, CancellationToken cancellationToken = default(CancellationToken))
    {
        var relationships = await GetWorkVersionRelationships(id, cancellationToken);

        return relationships.Count;
    }

    public async Task<Work> GetWork(int id, CancellationToken cancellationToken = default(CancellationToken))
    {
        var request = new GraphQLRequest<GetArgs>() {
            Query = GET_QUERY,
            Variables = new() {
                Id = id,
                Table = "ca_occurrences",
                Bundles = new() {
                    "ca_occurrences.preferred_labels"
                }
            }
        };

        var response = await PostAuthenticatedAsync<GetArgs,GetRoot<Work>>(ENDPOINT, request, cancellationToken);
        if (response == null || !response.Ok)
        {
            // TODO: better error handling and logging
            throw new ApplicationException(response?.Errors?.ToString());
        }

        return response.Data.Get;
    }

    public async Task<IList<WorkVersion>> GetVersionsForWork(Work work, CancellationToken cancellationToken = default(CancellationToken))
        => await GetVersionsForWork(work.Id);
    public async Task<IList<WorkVersion>> GetVersionsForWork(int id, CancellationToken cancellationToken = default(CancellationToken))
    {
        var relationships = await GetWorkVersionRelationships(id, cancellationToken);

        var relDataRequest = new GraphQLRequest<GetArgs>() {
            Query = GET_QUERY,
            Variables = new() {
                Table = "ca_occurrences_x_occurrences",
                Bundles = new() {"ca_occurrences_x_occurrences.occurrence_left_id"}
            }
        };

        var versionIds = new List<int>();
        foreach (MinRelationship rel in relationships)
        {
            relDataRequest.Variables.Id = rel.Id;
            var response = await PostAuthenticatedAsync<GetArgs, GetRoot<Relationship>>(ENDPOINT, relDataRequest, cancellationToken);

            // TODO: better error handling and logging
            if (response == null || !response.Ok)
            {
                // skip for now
                continue;
            }

            if(!int.TryParse(response.Data.Get.Bundles[0].Values[0].Value, out int otherId))
            {
                // invalid ID, skip
                continue;
            }

            versionIds.Add(otherId);
        }

        var versionRequest = new GraphQLRequest<GetArgs>()
        {
            Query = GET_QUERY,
            Variables = new() {
                Table = "ca_occurrences",
                Bundles = new() {

                }
            }
        };

        var versions = new List<WorkVersion>();
        foreach (int verId in versionIds)
        {
            versionRequest.Variables.Id = verId;
            var response = await PostAuthenticatedAsync<GetArgs, GetRoot<WorkVersion>>(ENDPOINT, versionRequest, cancellationToken);

            // TODO: better error handling and logging
            if (response == null || !response.Ok)
            {
                // skip for now
                continue;
            }

            versions.Add(response.Data.Get);
        }

        return versions;
    }

    public class GetRelationshipsArgs : GraphQLAuthVars
    {
        public string Table {get; set;}
        public string Target {get; set;}
        public int Id {get; set;}
        public List<string> RelTypes {get; set;}
        public List<string> TgTypes {get; set;}
    }

    public class GetRelationshipsRoot
    {
        public GetRelationshipsItem GetRelationships {get; set;}
    }

    public class GetRelationshipsItem
    {
        public int Id {get; set;}
        public IList<MinRelationship> Relationships {get; set;}
    }

    public class MinRelationship
    {
        public int Id {get; set;}
    }

    public class GetArgs : GraphQLAuthVars
    {
        public int Id {get; set;}
        public string Table {get; set;}
        public List<string> Bundles {get; set;}
    }

    public class GetRoot<T>
    {
        public T Get {get; set;}
    }
}
