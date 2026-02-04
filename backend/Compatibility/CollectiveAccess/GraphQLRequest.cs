namespace asec.Compatibility.CollectiveAccess;

public class GraphQLRequest<T>
{
    public string Query { get; set; }
    public T Variables { get; set; }
}

public class GraphQLAuthVars
{
    public string Jwt { get; set; }
}
