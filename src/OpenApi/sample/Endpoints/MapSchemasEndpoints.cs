// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.ComponentModel;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;

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
        schemas.MapPatch("/json-patch", (JsonPatchDocument patchDoc) => Results.NoContent());
        schemas.MapPatch("/json-patch-generic", (JsonPatchDocument<ParentObject> patchDoc) => Results.NoContent());
        schemas.MapGet("/custom-iresult", () => new CustomIResultImplementor { Content = "Hello world!" })
            .Produces<CustomIResultImplementor>(200);

        // Tests for validating scenarios related to https://github.com/dotnet/aspnetcore/issues/61194
        schemas.MapPost("/config-with-generic-lists", (Config config) => Results.Ok(config));
        schemas.MapPost("/project-response", (ProjectResponse project) => Results.Ok(project));
        schemas.MapPost("/subscription", (Subscription subscription) => Results.Ok(subscription));

        // Tests for oneOf nullable behavior on responses and request bodies
        schemas.MapGet("/nullable-response", () => TypedResults.Ok(new NullableResponseModel
        {
            RequiredProperty = "required",
            NullableProperty = null,
            NullableComplexProperty = null
        }));
        schemas.MapGet("/nullable-return-type", NullableResponseModel? () => new NullableResponseModel
        {
            RequiredProperty = "required",
            NullableProperty = null,
            NullableComplexProperty = null
        });
        schemas.MapPost("/nullable-request", (NullableRequestModel? request) => Results.Ok(request));
        schemas.MapPost("/complex-nullable-hierarchy", (ComplexHierarchyModel model) => Results.Ok(model));

        // Additional edge cases for nullable testing
        schemas.MapPost("/nullable-array-elements", (NullableArrayModel model) => Results.Ok(model));
        schemas.MapGet("/optional-with-default", () => TypedResults.Ok(new ModelWithDefaults()));
        schemas.MapGet("/nullable-enum-response", () => TypedResults.Ok(new EnumNullableModel
        {
            RequiredEnum = TestEnum.Value1,
            NullableEnum = null
        }));

        return endpointRouteBuilder;
    }

    public class CustomIResultImplementor : IResult
    {
        public required string Content { get; set; }
        public Task ExecuteAsync(HttpContext httpContext)
        {
            return Task.CompletedTask;
        }
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

    // Example types for GitHub issue 61194: Generic types referenced multiple times
    public sealed class Config
    {
        public List<ConfigItem> Items1 { get; set; } = [];
        public List<ConfigItem> Items2 { get; set; } = [];
    }

    public sealed class ConfigItem
    {
        public int? Id { get; set; }
        public string? Lang { get; set; }
        public Dictionary<string, object?>? Words { get; set; }
        public List<string>? Break { get; set; }
        public string? WillBeGood { get; set; }
    }

    // Example types for GitHub issue 63054: Reused types across different hierarchies
    public sealed class ProjectResponse
    {
        public required ProjectAddressResponse Address { get; init; }
        public required ProjectBuilderResponse Builder { get; init; }
    }

    public sealed class ProjectAddressResponse
    {
        public required CityResponse City { get; init; }
    }

    public sealed class ProjectBuilderResponse
    {
        public required CityResponse City { get; init; }
    }

    public sealed class CityResponse
    {
        public string Name { get; set; } = "";
    }

    // Example types for GitHub issue 63211: Nullable reference types
    public sealed class Subscription
    {
        public required string Id { get; set; }
        public required RefProfile PrimaryUser { get; set; }
        public RefProfile? SecondaryUser { get; set; }
    }

    public sealed class RefProfile
    {
        public required RefUser User { get; init; }
    }

    public sealed class RefUser
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
    }

    // Models for testing oneOf nullable behavior
    public sealed class NullableResponseModel
    {
        public required string RequiredProperty { get; set; }
        public string? NullableProperty { get; set; }
        public ComplexType? NullableComplexProperty { get; set; }
    }

    public sealed class NullableRequestModel
    {
        public required string RequiredField { get; set; }
        public string? OptionalField { get; set; }
        public List<string>? NullableList { get; set; }
        public Dictionary<string, string?>? NullableDictionary { get; set; }
    }

    // Complex hierarchy model for testing nested nullable properties
    public sealed class ComplexHierarchyModel
    {
        public required string Id { get; set; }
        public NestedModel? OptionalNested { get; set; }
        public required NestedModel RequiredNested { get; set; }
        public List<NestedModel?>? NullableListWithNullableItems { get; set; }
    }

    public sealed class NestedModel
    {
        public required string Name { get; set; }
        public int? OptionalValue { get; set; }
        public ComplexType? DeepNested { get; set; }
    }

    public sealed class ComplexType
    {
        public string? Description { get; set; }
        public DateTime? Timestamp { get; set; }
    }

    // Additional models for edge case testing
    public sealed class NullableArrayModel
    {
        public string[]? NullableArray { get; set; }
        public List<string?> ListWithNullableElements { get; set; } = [];
        public Dictionary<string, string?>? NullableDictionaryWithNullableValues { get; set; }
    }

    public sealed class ModelWithDefaults
    {
        public string PropertyWithDefault { get; set; } = "default";
        public string? NullableWithNull { get; set; }
        public int NumberWithDefault { get; set; } = 42;
        public bool BoolWithDefault { get; set; } = true;
    }

    // Enum testing with nullable
    public enum TestEnum
    {
        Value1,
        Value2,
        Value3
    }

    public sealed class EnumNullableModel
    {
        public required TestEnum RequiredEnum { get; set; }
        public TestEnum? NullableEnum { get; set; }
        public List<TestEnum?> ListOfNullableEnums { get; set; } = [];
    }
}
