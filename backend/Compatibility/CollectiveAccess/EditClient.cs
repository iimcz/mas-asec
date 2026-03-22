using asec.Compatibility.CollectiveAccess.Models;
using System.Text.Json.Nodes;
using CSharpVitamins;

namespace asec.Compatibility.CollectiveAccess;

public class EditClient : BaseCollectiveAccessClient
{
    private const string ADD_QUERY = """
        mutation add_do($jwt: String, $idno: String, $table: String, $type: String,
                $bundles: [Bundle], $relationships: [SubjectRelationship], $erp: String,
                $matchOn: [String])
        {
            add(jwt: $jwt, idno: $idno, table: $table, type: $type, bundles: $bundles,
                    relationships: $relationships, existingRecordPolicy: $erp, matchOn:$matchOn)
            {
                id
                errors { bundle, code, idno, message }
                warnings { bundle, code, idno, message }
                info { bundle, code, idno, message }
            }
        }
    """;
    private const string ENDPOINT = "service.php/edit";
    private readonly ILogger<EditClient> _logger;

    public EditClient(IConfiguration configuration, IHttpClientFactory clientFactory, CollectiveAccessAuth auth, ILogger<EditClient> logger) : base(configuration, clientFactory, auth)
    {
        _logger = logger;
    }

    public async Task<int> AddOrUpdateDigitalObject(asec.Models.Archive.DigitalObject digitalObject, CancellationToken cancellationToken = default(CancellationToken))
    {
       
        var workVersionRelationships = digitalObject.WorkVersions?.Select(
            wv => new SubjectRelationship(
                SubjectRelationshipTypes.ManifestationOf,
                Tables.Occurrences,
                wv.RemoteId
            )
        ).ToList() ?? new List<SubjectRelationship>();

        // Since CollectiveAccess idno is only 30 chars long, modify our UUID to fit.
        var idno = new ShortGuid(digitalObject.Id).ToString();

        var request = new GraphQLRequest<AddArgs>() {
            Query = ADD_QUERY,
            Variables = new() {
                Idno = idno,
                Table = Tables.Objects,
                Type = Types.DigitalObject,
                Erp = "MERGE",
                MatchOn = new() { "idno" },
                Bundles = new() {
                    new(
                        Locales.Czech,
                        BundleNames.PreferredLabels,
                        digitalObject.Label
                    ),
                    new(
                        Locales.Czech,
                        BundleNames.FileName,
                        digitalObject.FileName
                    ),
                    new(
                        Locales.Czech,
                        BundleNames.Quality,
                        digitalObject.Quality
                    ),
                    new(
                        Locales.Czech,
                        BundleNames.FileSize,
                        digitalObject.FileSize.ToString()
                    ),
                    new(
                        Locales.Czech,
                        BundleNames.DigitalObjectType,
                        digitalObject.DigitalObjectType
                    ),
                    new(
                        Locales.Czech,
                        BundleNames.InternalNotes,
                        digitalObject.InternalNote
                    ),
                    new(
                        Locales.Czech,
                        BundleNames.Format,
                        digitalObject.Format
                    ),
                    new(
                        Locales.Czech,
                        BundleNames.FedoraUrl,
                        digitalObject.FedoraUrl
                    )
                },
                Relationships = workVersionRelationships
            }
        };

        var response = await PostAuthenticatedAsync<AddArgs, AddRoot>(ENDPOINT, request, cancellationToken);

        if (response.Data.Add.Info.Count > 0)
        {
            _logger.LogInformation(response.Data.Add.Info.ToString());
        }
        if (response.Data.Add.Warnings.Count > 0)
        {
            _logger.LogWarning(response.Data.Add.Warnings.ToString());
        }
        if (response.Data.Add.Errors.Count > 0)
        {
            _logger.LogError(response.Data.Add.Errors.ToString());
        }

        return response.Data.Add.Id.Single();
    }

    public class AddArgs : GraphQLAuthVars
    {
        public string Table { get; set; }
        public string Type { get; set; }
        public string Idno { get; set; }
        public string Erp { get; set; }
        public List<string> MatchOn { get; set; }
        public List<InputBundle> Bundles { get; set; }
        public List<SubjectRelationship> Relationships { get; set; }
    }

    public class AddRoot
    {
        public AddData Add { get; set; }
    }

    public class AddData
    {
        public List<int> Id { get; set; }
        public JsonArray Errors { get; set; }
        public JsonArray Warnings { get; set; }
        public JsonArray Info { get; set; }
    }
}
