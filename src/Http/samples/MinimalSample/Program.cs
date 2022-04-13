// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

string Plaintext() => "Hello, World!";
app.MapGet("/plaintext", Plaintext);

object Json() => new { message = "Hello, World!" };
app.MapGet("/json", Json);

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

app.MapGet("/problem", () =>
    Results.Problem(statusCode: 500, extensions: extensions));

app.MapGet("/problem-object", () =>
    Results.Problem(new ProblemDetails() { Status = 500, Extensions = { { "traceId", "traceId123" } } }));

var errors = new Dictionary<string, string[]>() { { "Title", new[] { "The Title field is required." } } };

app.MapGet("/validation-problem", () =>
    Results.ValidationProblem(errors, statusCode: 400, extensions: extensions));

app.MapGet("/validation-problem-object", () =>
    Results.Problem(new HttpValidationProblemDetails(errors) { Status = 400, Extensions = { { "traceId", "traceId123" } } }));

app.MapGet("/validation-problem-typed", () =>
    TypedResults.ValidationProblem(errors, extensions: extensions));

app.Run();

internal record Todo(int Id, string Title);
