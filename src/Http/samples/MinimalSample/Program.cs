// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

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

inner.AddRouteHandlerFilter((routeContext, next) =>
{
    var tags = routeContext.EndpointMetadata.OfType<ITagsMetadata>().FirstOrDefault();

    return async invocationContext =>
    {
        Console.WriteLine("Running filter!");
        var result = await next(invocationContext);
        return ((string)result) + " | /inner filter! Tags:" + tags is null ? "(null)" : string.Join(", ", tags.Tags);
    };
});

outer.MapGet("/outerget", () => "I'm nested.");
inner.MapGet("/innerget", () => "I'm more nested.");

inner.AddRouteHandlerFilter((routeContext, next) =>
{
    Console.WriteLine($"Building filter! Num args: {routeContext.MethodInfo.GetParameters().Length}"); ;
    return async invocationContext =>
    {
        Console.WriteLine("Running filter!");
        var result = await next(invocationContext);
        return ((string)result) + "| nested filter!";
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

app.MapGet("/null-result", IResult () => null);

app.MapGet("/todo/{id}", Results<Ok<Todo>, NotFound, BadRequest> (int id) => id switch
    {
        <= 0 => TypedResults.BadRequest(),
        >= 1 and <= 10 => TypedResults.Ok(new Todo(id, "Walk the dog")),
        _ => TypedResults.NotFound()
    });

var extensions = new Dictionary<string, object>() { { "traceId", "traceId123" } };

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

app.Run();

internal record Todo(int Id, string Title);
public class TodoBindable : IBindableFromHttpContext<TodoBindable>
{
    public int Id { get; set; }
    public string Title { get; set; }
    public bool IsComplete { get; set; }

    public static ValueTask<TodoBindable> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        return ValueTask.FromResult(new TodoBindable { Id = 1, Title = "I was bound from IBindableFromHttpContext<TodoBindable>.BindAsync!" });
    }
}
