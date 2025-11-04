namespace Terminus.Generator.Examples.Web;

public class RouteMatch
{
    public bool IsMatch { get; }
    public Dictionary<string, string> Parameters { get; }

    public RouteMatch(bool isMatch, Dictionary<string, string> parameters)
    {
        IsMatch = isMatch;
        Parameters = parameters;
    }

    public static RouteMatch NoMatch => new(false, new Dictionary<string, string>());
}