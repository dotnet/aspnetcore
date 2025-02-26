// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.ComponentModel;
using Microsoft.AspNetCore.Http.HttpResults;

public static class SchemasEndpointsExtensions
{
    public static IEndpointRouteBuilder MapSchemasEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        var schemas = endpointRouteBuilder.MapGroup("schemas-by-ref")
            .WithGroupName("schemas-by-ref");

        schemas.MapGet("/typed-results", () => TypedResults.Ok(new Triangle { Color = "red", Sides = 3, Hypotenuse = 5.0 }));
        schemas.MapGet("/multiple-results", Results<Ok<Triangle>, NotFound<string>> () => Random.Shared.Next(0, 2) == 0
            ? TypedResults.Ok(new Triangle { Color = "red", Sides = 3, Hypotenuse = 5.0 })
            : TypedResults.NotFound<string>("Item not found."));
        schemas.MapGet("/iresult-no-produces", () => Results.Ok(new Triangle { Color = "red", Sides = 3, Hypotenuse = 5.0 }));
        schemas.MapGet("/iresult-with-produces", () => Results.Ok(new Triangle { Color = "red", Sides = 3, Hypotenuse = 5.0 }))
            .Produces<Triangle>(200, "text/xml");
        schemas.MapGet("/primitives", ([Description("The ID associated with the Todo item.")] int id, [Description("The number of Todos to fetch")] int size) => { });
        schemas.MapGet("/product", (Product product) => TypedResults.Ok(product));
        schemas.MapGet("/account", (Account account) => TypedResults.Ok(account));
        schemas.MapPost("/array-of-ints", (int[] values) => values.Sum());
        schemas.MapPost("/list-of-ints", (List<int> values) => values.Count);
        schemas.MapPost("/ienumerable-of-ints", (IEnumerable<int> values) => values.Count());
        schemas.MapGet("/dictionary-of-ints", () => new Dictionary<string, int> { { "one", 1 }, { "two", 2 } });
        schemas.MapGet("/frozen-dictionary-of-ints", () => ImmutableDictionary.CreateRange(new Dictionary<string, int> { { "one", 1 }, { "two", 2 } }));
        schemas.MapPost("/shape", (Shape shape) => { });
        schemas.MapPost("/weatherforecastbase", (WeatherForecastBase forecast) => { });
        schemas.MapPost("/person", (Person person) => { });

        return endpointRouteBuilder;
    }
}
