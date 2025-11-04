using System.Text.RegularExpressions;

namespace Terminus.Generator.Examples.Web;

public class RouteEntry
{
    private readonly Regex _pattern;
    private readonly List<string> _paramNames;

    public RequestDelegate Handler { get; }

    public RouteEntry(string pattern, RequestDelegate handler)
    {
        Handler = handler;
        _paramNames = new List<string>();

        // Convert "/users/{id}/posts/{postId}" to regex
        var regexPattern = Regex.Replace(pattern, @"\{(\w+)\}", match =>
        {
            _paramNames.Add(match.Groups[1].Value);
            return @"([^/]+)";
        });

        _pattern = new Regex($"^{regexPattern}$");
    }

    public RouteMatch Match(string path)
    {
        var match = _pattern.Match(path);
        if (!match.Success)
            return RouteMatch.NoMatch;

        var parameters = new Dictionary<string, string>();
        for (int i = 0; i < _paramNames.Count; i++)
        {
            parameters[_paramNames[i]] = match.Groups[i + 1].Value;
        }

        return new RouteMatch(true, parameters);
    }
}