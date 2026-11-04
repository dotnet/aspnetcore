// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public static class VersionedEndpointsExtensions
{
    public static IEndpointRouteBuilder MapV1Endpoints(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        var v1 = endpointRouteBuilder.MapGroup("/v1")
            .WithGroupName("v1");

        v1.MapGet("/array-of-guids", (Guid[] guids) => guids);

        v1.MapPost("/todos", (Todo todo) => Results.Created($"/todos/{todo.Id}", todo))
            .WithSummary("Creates a new todo item.");
        v1.MapGet("/todos/{id}", (int id) => new TodoWithDueDate(1, "Test todo", false, DateTime.Now.AddDays(1), DateTime.Now))
            .WithDescription("Returns a specific todo item.");

        return endpointRouteBuilder;
    }

    public static IEndpointRouteBuilder MapV2Endpoints(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        var v2 = endpointRouteBuilder.MapGroup("/v2")
            .WithGroupName("v2");

        v2.MapGet("/users", () => new[] { "alice", "bob" })
            .WithTags("users");

        v2.MapPost("/users", () => Results.Created("/users/1", new { Id = 1, Name = "Test user" }))
            .WithName("CreateUser");

        return endpointRouteBuilder;
    }
}
