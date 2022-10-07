// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string Plaintext() => "Hello, World!";
app.MapGet("/plaintext", Plaintext);

app.MapGet("/", () => $"""
    Operating System: {Environment.OSVersion}
    .NET version: {Environment.Version}
    Username: {Environment.UserName}
    Date and Time: {DateTime.Now}
    """);
var outer = app.MapGroup("/outer");
var inner = outer.MapGroup("/inner");

inner.AddEndpointFilterFactory((routeContext, next) =>
{
    IReadOnlyList<string>? tags = null;

    return async invocationContext =>
    {
        tags ??= invocationContext.HttpContext.GetEndpoint()?.Metadata.GetMetadata<ITagsMetadata>()?.Tags ?? Array.Empty<string>();

        Console.WriteLine("Running filter!");
        var result = await next(invocationContext);
        return $"{result} | /inner filter! Tags: {(tags.Count == 0 ? "(null)" : string.Join(", ", tags))}";
    };
});

outer.MapGet("/outerget", () => "I'm nested.");
inner.MapGet("/innerget", () => "I'm more nested.");

inner.AddEndpointFilterFactory((routeContext, next) =>
{
    Console.WriteLine($"Building filter! Num args: {routeContext.MethodInfo.GetParameters().Length}"); ;
    return async invocationContext =>
    {
        Console.WriteLine("Running filter!");
        var result = await next(invocationContext);
        return $"{result} | nested filter!";
    };
});

var superNested = inner.MapGroup("/group/{groupName}")
   .MapGroup("/nested/{nestedName}")
   .WithTags("nested", "more", "tags");

superNested.MapGet("/", (string groupName, string nestedName) =>
{
   return $"Hello from {groupName}:{nestedName}!";
});

object Json() => new { message = "Hello, World!" };
app.MapGet("/json", Json).WithTags("json");

string SayHello(string name) => $"Hello, {name}!";
app.MapGet("/hello/{name}", SayHello);

app.MapGet("/null-result", IResult () => null!);

app.MapGet("/todo/{id}", Results<Ok<Todo>, NotFound, BadRequest> (int id) => id switch
    {
        <= 0 => TypedResults.BadRequest(),
        >= 1 and <= 10 => TypedResults.Ok(new Todo(id, "Walk the dog")),
        _ => TypedResults.NotFound()
    });

var extensions = new Dictionary<string, object?>() { { "traceId", "traceId123" } };

var errors = new Dictionary<string, string[]>() { { "Title", new[] { "The Title field is required." } } };

app.MapGet("/problem/{problemType}", (string problemType) => problemType switch
    {
        "plain" => Results.Problem(statusCode: 500, extensions: extensions),
        "object" => Results.Problem(new ProblemDetails() { Status = 500, Extensions = { { "traceId", "traceId123" } } }),
        "validation" => Results.ValidationProblem(errors, statusCode: 400, extensions: extensions),
        "objectValidation" => Results.Problem(new HttpValidationProblemDetails(errors) { Status = 400, Extensions = { { "traceId", "traceId123" } } }),
        "validationTyped" => TypedResults.ValidationProblem(errors, extensions: extensions),
        _ => TypedResults.NotFound()

    });

app.MapPost("/todos", (TodoBindable todo) => todo);

app.MapGet("/list", (HttpResponse response, EndpointDataSource endpointSource) =>
{
    response.Headers["Refresh"] = "1";
    return GetDebuggerDisplayStringForEndpoints(endpointSource.Endpoints);
});

((IEndpointRouteBuilder)app).DataSources.Add(new DynamicEndpointDataSource());

app.Run();

static string GetDebuggerDisplayStringForEndpoints(IReadOnlyList<Endpoint> endpoints)
{
    if (endpoints is null)
    {
        return "No endpoints";
    }

    var sb = new StringBuilder();

    foreach (var endpoint in endpoints)
    {
        if (endpoint is RouteEndpoint routeEndpoint)
        {
            var template = routeEndpoint.RoutePattern.RawText;
            template = string.IsNullOrEmpty(template) ? "\"\"" : template;
            sb.Append(template);
            sb.Append(", Defaults: new { ");
            FormatValues(sb, routeEndpoint.RoutePattern.Defaults);
            sb.Append(" }");
            var routeNameMetadata = routeEndpoint.Metadata.GetMetadata<IRouteNameMetadata>();
            sb.Append(", Route Name: ");
            sb.Append(routeNameMetadata?.RouteName);
            var routeValues = routeEndpoint.RoutePattern.RequiredValues;

            if (routeValues.Count > 0)
            {
                sb.Append(", Required Values: new { ");
                FormatValues(sb, routeValues);
                sb.Append(" }");
            }

            sb.Append(", Order: ");
            sb.Append(routeEndpoint.Order);

            var httpMethodMetadata = routeEndpoint.Metadata.GetMetadata<IHttpMethodMetadata>();

            if (httpMethodMetadata is not null)
            {
                sb.Append(", Http Methods: ");
                sb.AppendJoin(", ", httpMethodMetadata.HttpMethods);
            }

            sb.Append(", Display Name: ");
        }
        else
        {
            sb.Append("Non-RouteEndpoint. DisplayName: ");
        }

        sb.AppendLine(endpoint.DisplayName);
    }

    return sb.ToString();

    static void FormatValues(StringBuilder sb, IEnumerable<KeyValuePair<string, object>> values)
    {
        var isFirst = true;

        foreach (var (key, value) in values)
        {
            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                sb.Append(", ");
            }

            sb.Append(key);
            sb.Append(" = ");

            if (value is null)
            {
                sb.Append("null");
            }
            else
            {
                sb.Append('\"');
                sb.Append(value);
                sb.Append('\"');
            }
        }
    }
}

internal record Todo(int Id, string Title);
public class TodoBindable : IBindableFromHttpContext<TodoBindable>
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsComplete { get; set; }

    public static ValueTask<TodoBindable?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        return ValueTask.FromResult<TodoBindable?>(new TodoBindable { Id = 1, Title = "I was bound from IBindableFromHttpContext<TodoBindable>.BindAsync!" });
    }
}

public sealed class DynamicEndpointDataSource : EndpointDataSource, IDisposable
{
    private readonly PeriodicTimer _timer;
    private readonly Task _timerTask;

    private Endpoint[] _endpoints = Array.Empty<Endpoint>();
    private CancellationTokenSource _cts = new();

    public DynamicEndpointDataSource()
    {
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        _timerTask = TimerLoop();
    }

    public override IReadOnlyList<Endpoint> Endpoints => _endpoints;

    public async Task TimerLoop()
    {
        while (await _timer.WaitForNextTickAsync())
        {
            var newEndpoints = new Endpoint[_endpoints.Length + 1];
            Array.Copy(_endpoints, 0, newEndpoints, 0, _endpoints.Length);

            newEndpoints[_endpoints.Length] = CreateDynamicRouteEndpoint(_endpoints.Length);

            _endpoints = newEndpoints;
            var oldCts = _cts;
            _cts = new CancellationTokenSource();
            oldCts.Cancel();
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
        _timerTask.GetAwaiter().GetResult();
    }

    public override IChangeToken GetChangeToken()
    {
        return new CancellationChangeToken(_cts.Token);
    }

    private static RouteEndpoint CreateDynamicRouteEndpoint(int id)
    {
        var displayName = $"Dynamic endpoint #{id}";
        var metadata = new EndpointMetadataCollection(new[] { new RouteNameMetadata(displayName) });

        return new RouteEndpoint(
            context => context.Response.WriteAsync(displayName),
            RoutePatternFactory.Parse($"/dynamic/{id}"),
            order: 0, metadata, displayName);
    }
}
