using System.Text.Json.Nodes;

namespace asec.Compatibility.CollectiveAccess;

public class GraphQLResponse<T>
{
    public bool Ok {get; set;}
    public T Data {get; set;}
    public JsonArray Errors {get; set;}
}
