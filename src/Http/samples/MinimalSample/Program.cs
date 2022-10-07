// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
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

((IEndpointRouteBuilder)app).DataSources.Add(new DynamicEndpointDataSource());

app.Run();

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
    private CancellationTokenSource _cts = new();
    private Endpoint[] _endpoints = new[]
    {
        new RouteEndpoint(ListEndpoints, RoutePatternFactory.Parse("/dynamic"), 0, null, "Endpoint list."),
    };
    private readonly PeriodicTimer _timer;
    private readonly Task _timerTask;

    public DynamicEndpointDataSource()
    {
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        _timerTask = TimerLoop();
    }

    public override IReadOnlyList<Endpoint> Endpoints => _endpoints;

    public async Task TimerLoop()
    {
        while (await _timer.WaitForNextTickAsync())
        {
            Console.WriteLine("Ticking");

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

    private static Task ListEndpoints(HttpContext context)
    {
        context.Response.Headers["Refresh"] = "1";
        return context.Response.WriteAsync(GetDebuggerDisplayStringForEndpoints(context.RequestServices.GetRequiredService<EndpointDataSource>().Endpoints));
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
