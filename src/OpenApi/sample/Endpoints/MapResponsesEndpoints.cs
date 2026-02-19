// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

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

        // Test custom descriptions using ProducesResponseType attribute
        responses.MapGet("/custom-description-attribute",
            [ProducesResponseType(typeof(string), StatusCodes.Status200OK, "text/html",
                Description = "Custom description using attribute")]
        () => "Hello World");

        // Also test with .WithMetadata approach
        responses.MapGet("/custom-description-extension-method", () => "Hello World")
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, null, ["text/html"])
            {
                Description = "Custom description using extension method"
            });

        return endpointRouteBuilder;
    }
}
