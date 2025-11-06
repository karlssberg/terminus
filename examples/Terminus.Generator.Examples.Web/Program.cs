using Terminus;
using Terminus.Attributes;
using Terminus.Generated;
using Terminus.Generator.Examples.Web;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTransient<CustomRouter>();
builder.Services.AddEntryPoints<MyHttpPostAttribute>();

var app = builder.Build();

app.Use(async (HttpContext context, RequestDelegate _) =>
{
    var entryPoints = context.RequestServices.GetServices<EntryPointDescriptor<MyHttpPostAttribute>>();
    var dispatcher = context.RequestServices.GetRequiredService<IAsyncDispatcher<MyHttpPostAttribute>>();
    var router = context.RequestServices.GetRequiredService<CustomRouter>();
    
    foreach (var entryPoint in entryPoints)
    foreach (var attribute in entryPoint.Attributes)
    {
        router.AddRoute(attribute.Path, httpContext =>
        {
            var routeValues = httpContext.Request.RouteValues.ToDictionary(x => x.Key, x => x.Value);
            return dispatcher.PublishAsync(new ParameterBindingContext(httpContext.RequestServices, routeValues), CancellationToken.None);
        });
    }
    await router.RouteAsync(context);
});

await app.RunAsync().WaitAsync(CancellationToken.None);

namespace Terminus.Generator.Examples.Web
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MyHttpPostAttribute(string path) : EntryPointAttribute
    {
        public string Path { get; } = path;
    }

    public class MyController
    {
        [MyHttpPost("/users/{id}/posts/{postId}")]
        public void GetPost(string id, string postId)
        {
            Console.WriteLine($"GetPost: {id} {postId}");
        }
    }
}