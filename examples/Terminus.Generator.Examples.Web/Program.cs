using Terminus;
using Terminus.Generator.Examples.Web;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTransient<CustomRouter>();
builder.Services.AddEntryPoints<IDispatcher>();

var app = builder.Build();

app.Use(async (HttpContext context, RequestDelegate _) =>
{
    var dispatcher = context.RequestServices.GetRequiredService<IDispatcher>();
    
    var response = dispatcher.Router(new ParameterBindingContext(context.GetRouteData().DataTokens), CancellationToken.None);
    
    
    await router.RouteAsync(context);
});

await app.RunAsync().WaitAsync(CancellationToken.None);

namespace Terminus.Generator.Examples.Web
{
    [ScopedEntryPointRouter(EntryPointAttributes = [typeof(MyHttpPostAttribute)])]
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