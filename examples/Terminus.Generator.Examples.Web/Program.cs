using System.Text.RegularExpressions;
using Terminus.Generator.Examples.Web;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTransient<CustomRouter>();
var app = builder.Build();

app.Use(async (HttpContext context, RequestDelegate _) =>
{
    var router = context.RequestServices.GetRequiredService<CustomRouter>();
    await router.RouteAsync(context);
});


app.Run();

namespace Terminus.Generator.Examples.Web
{
    public class CustomRouter
    {
        private readonly List<RouteEntry> _routes = new();

        public void AddRoute(string pattern, RequestDelegate handler)
        {
            _routes.Add(new RouteEntry(pattern, handler));
        }

        public async Task RouteAsync(HttpContext context)
        {
            var path = context.Request.Path.Value;

            foreach (var route in _routes)
            {
                var match = route.Match(path);
                if (match.IsMatch)
                {
                    // Populate route values
                    foreach (var param in match.Parameters)
                    {
                        context.Request.RouteValues[param.Key] = param.Value;
                    }

                    await route.Handler(context);
                    return;
                }
            }

            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("Not Found");
        }
    }
    
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
}