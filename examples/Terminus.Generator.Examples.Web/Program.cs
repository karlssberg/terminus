using Terminus;
using Terminus.Generator.Examples.Web;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTransient<CustomRouter>();
builder.Services.AddEntryPoints<IDispatcher>();

var app = builder.Build();

app.Use(async (HttpContext context, RequestDelegate _) =>
{
    var entryPoints = context.RequestServices.GetServices<EntryPointDescriptor<MyHttpPostAttribute>>();
    var dispatcher = context.RequestServices.GetRequiredService<IDispatcher>();
    var router = context.RequestServices.GetRequiredService<CustomRouter>();
    
    foreach (var entryPoint in entryPoints)
    {
        router.AddRoute(entryPoint.Attribute.Path, httpContext =>
        {
            var routeValues = httpContext.Request.RouteValues.ToDictionary(x => x.Key, x => x.Value);
            dispatcher.Publish(new ParameterBindingContext(routeValues), CancellationToken.None);
            return Task.CompletedTask;
        });
    }
    await router.RouteAsync(context);
});

await app.RunAsync().WaitAsync(CancellationToken.None);

namespace Terminus.Generator.Examples.Web
{
    [ScopedEntryPointMediator(EntryPointAttributes = [typeof(MyHttpPostAttribute)])]
    public partial interface IDispatcher;
    
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