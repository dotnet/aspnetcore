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
        schemas.MapPost("/category", (Category category) => { });
        schemas.MapPost("/container", (ContainerType container) => { });
        schemas.MapPost("/root", (Root root) => { });
        schemas.MapPost("/location", (LocationContainer location) => { });
        schemas.MapPost("/parent", (ParentObject parent) => Results.Ok(parent));
        schemas.MapPost("/child", (ChildObject child) => Results.Ok(child));

        return endpointRouteBuilder;
    }

    public sealed class Category
    {
        public required string Name { get; set; }

        public required Category Parent { get; set; }

        public IEnumerable<Tag> Tags { get; set; } = [];
    }

    public sealed class Tag
    {
        public required string Name { get; set; }
    }

    public sealed class ContainerType
    {
        public List<List<string>> Seq1 { get; set; } = [];
        public List<List<string>> Seq2 { get; set; } = [];
    }

    public sealed class Root
    {
        public Item Item1 { get; set; } = null!;
        public Item Item2 { get; set; } = null!;
    }

    public sealed class Item
    {
        public string[] Name { get; set; } = null!;
        public int value { get; set; }
    }

    public sealed class LocationContainer
    {
        public required LocationDto Location { get; set; }
    }

    public sealed class LocationDto
    {
        public required AddressDto Address { get; set; }
    }

    public sealed class AddressDto
    {
        public required LocationDto RelatedLocation { get; set; }
    }

    public sealed class ParentObject
    {
        public int Id { get; set; }
        public List<ChildObject> Children { get; set; } = [];
    }

    public sealed class ChildObject
    {
        public int Id { get; set; }
        public required ParentObject Parent { get; set; }
    }
}
