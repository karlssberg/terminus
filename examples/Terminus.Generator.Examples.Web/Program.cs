using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Terminus;
using Terminus.Generator.Examples.Web;
using Terminus.Strategies;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<QueryStringBinderStrategy>();
builder.Services.AddEntryPoints<IDispatcher>(strategies =>
    strategies.AddStrategy<QueryStringBinderStrategy>());

var app = builder.Build();

app.Use(async (HttpContext context, RequestDelegate _) =>
{
    var dispatcher = context.RequestServices.GetRequiredService<IDispatcher>();
    var response = await dispatcher.Route(context.GetRouteData().DataTokens, CancellationToken.None);

    if (!response.EntryPointExists)
    {
        context.Response.StatusCode = 404;
        return;
    }
    
    context.Response.StatusCode = 200;
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(response);
});

await app.RunAsync().WaitAsync(CancellationToken.None);

namespace Terminus.Generator.Examples.Web
{
    public class MyFromBodyAttribute : ParameterBinderAttribute<HttpBodyBinder>;
    public class HttpBodyBinder(HttpContext httpContext) : IParameterBinder
    {
        public TParameter BindParameter<TParameter>(ParameterBindingContext context)
        {
            using var reader = new StreamReader(httpContext.Request.Body);
            var body = reader.ReadToEndAsync().Result;
            return JsonSerializer.Deserialize<TParameter>(body)
                ?? throw new InvalidOperationException($"Failed to deserialize request body to type {typeof(TParameter).FullName}");
        }
    }

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
        public void GetPost([MyFromBody]MyModel model, string id, string postId)
        {
            Console.WriteLine($"POST querystring parameters: {id} {postId}");
            Console.WriteLine($"POST body: {JsonSerializer.Serialize(model)}");
        }
    }

    public class MyModel
    {
        public required string FirstName { get; init; }
        public required string LastName { get; init; }
    }
    
    public class QueryStringBinderStrategy(HttpContext httpContext) : IParameterBindingStrategy
    {
        public bool CanBind(ParameterBindingContext context) =>
            httpContext.Request.Query.ContainsKey(context.ParameterName);

        public object? BindParameter(ParameterBindingContext context)
        {
            var value = httpContext.Request.Query[context.ParameterName];

            return value switch
            {
                [var item] =>
                    Convert.ChangeType(item, context.ParameterType),
                _ => value
                        .Select(item => item switch
                        {
                            null => null,
                            _ => Convert.ChangeType(item, context.ParameterType.GetGenericArguments()[0])
                        })
            };

        }
    }
}