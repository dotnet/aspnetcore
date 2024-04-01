// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi("v1");
builder.Services.AddOpenApi("v2");

var app = builder.Build();

app.MapOpenApi();
if (app.Environment.IsDevelopment())
{
    app.MapSwaggerUi();
}

var v1 = app.MapGroup("v1")
    .WithGroupName("v1");

var v2 = app.MapGroup("v2")
    .WithGroupName("v2");

v1.MapPost("/todos", (Todo todo) => Results.Created($"/todos/{todo.Id}", todo));
v1.MapGet("/todos/{id}", (int id) => new TodoWithDueDate(1, "Test todo", false, DateTime.Now.AddDays(1), DateTime.Now));

v2.MapPost("/users", () => Results.Created("/users/1", new { Id = 1, Name = "Test user" }));

app.Run();

public record Todo(int Id, string Title, bool Completed, DateTime CreatedAt);
public record TodoWithDueDate(int Id, string Title, bool Completed, DateTime CreatedAt, DateTime DueDate) : Todo(Id, Title, Completed, CreatedAt);
