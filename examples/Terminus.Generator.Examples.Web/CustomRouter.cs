namespace Terminus.Generator.Examples.Web;

public class CustomRouter
{
    private readonly List<RouteEntry> _routes = new();

    public void AddRoute(string pattern, RequestDelegate handler)
    {
            
        _routes.Add(new RouteEntry(pattern, handler));
    }

    public async Task RouteAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        foreach (var route in _routes)
        {
            var match = route.Match(path);
            if (!match.IsMatch) continue;
            
            // Populate route values
            foreach (var param in match.Parameters)
            {
                context.Request.RouteValues[param.Key] = param.Value;
            }

            await route.Handler(context);
            return;
        }

        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("Not Found");
    }
}