// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public static class ResponseEndpoints
{
    public static IEndpointRouteBuilder MapResponseEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        var responses = endpointRouteBuilder.MapGroup("/responses")
            .WithGroupName("responses");

        responses.MapGet("/200-add-xml", () => new TodoWithDueDate(1, "Test todo", false, DateTime.Now.AddDays(1), DateTime.Now))
            .Produces<Todo>(additionalContentTypes: "text/xml");

        responses.MapGet("/200-only-xml", () => new TodoWithDueDate(1, "Test todo", false, DateTime.Now.AddDays(1), DateTime.Now))
            .Produces<Todo>(contentType: "text/xml");

        responses.MapGet("/triangle", () => new Triangle { Color = "red", Sides = 3, Hypotenuse = 5.0 });
        responses.MapGet("/shape", Shape () => new Triangle { Color = "blue", Sides = 4 });

        return endpointRouteBuilder;
    }
}
