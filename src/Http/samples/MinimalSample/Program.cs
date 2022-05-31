// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

string Plaintext() => "Hello, World!";
app.MapGet("/plaintext", Plaintext);

var message =
    $"""
    Operating System: {Environment.OSVersion}
    .NET version: {Environment.Version}
    """;

app.MapGet("/", () => message);

var nestedGroup = app.MapGroup("/group/{groupName}")
   .MapGroup("/nested/{nestedName}")
   .WithMetadata(new TagsAttribute("nested"));

nestedGroup
   .MapGet("/", (string groupName, string nestedName) =>
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

app.Run();

internal record Todo(int Id, string Title);
