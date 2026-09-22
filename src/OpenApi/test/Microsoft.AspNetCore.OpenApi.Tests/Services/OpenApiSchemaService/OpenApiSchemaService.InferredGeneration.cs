// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;

public partial class OpenApiSchemaServiceTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public void SchemaGenerationMode_DefaultsToLegacy()
    {
        var options = new OpenApiOptions();

#pragma warning disable ASP0040
        Assert.Equal(OpenApiSchemaGenerationMode.Legacy, options.SchemaGenerationMode);
#pragma warning restore ASP0040
    }

    [Fact]
    public async Task SchemaGenerationMode_Legacy_PreservesPolymorphicAnyOf()
    {
        var builder = CreateBuilder();
        builder.MapPost("/api", (Shape shape) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var schema = document.Paths["/api"].Operations[HttpMethod.Post].RequestBody.Content["application/json"].Schema;
            Assert.Collection(
                schema.AnyOf,
                branch => Assert.Equal("ShapeTriangle", Assert.IsType<OpenApiSchemaReference>(branch).Reference.Id),
                branch => Assert.Equal("ShapeSquare", Assert.IsType<OpenApiSchemaReference>(branch).Reference.Id));
            Assert.Null(schema.OneOf);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_UsesOrderedOneOfAndDiscriminatorMappings()
    {
        var builder = CreateBuilder();
        builder.MapPost("/api", (Shape shape) => { });

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var schema = document.Paths["/api"].Operations[HttpMethod.Post].RequestBody.Content["application/json"].Schema;
            Assert.Null(schema.AnyOf);
            Assert.Collection(
                schema.OneOf,
                branch => Assert.Equal("ShapeTriangle", Assert.IsType<OpenApiSchemaReference>(branch).Reference.Id),
                branch => Assert.Equal("ShapeSquare", Assert.IsType<OpenApiSchemaReference>(branch).Reference.Id));
            Assert.Equal("$type", schema.Discriminator.PropertyName);
            Assert.Collection(
                schema.Discriminator.Mapping,
                mapping =>
                {
                    Assert.Equal("triangle", mapping.Key);
                    Assert.Equal("#/components/schemas/ShapeTriangle", mapping.Value.Reference.ReferenceV3);
                },
                mapping =>
                {
                    Assert.Equal("square", mapping.Key);
                    Assert.Equal("#/components/schemas/ShapeSquare", mapping.Value.Reference.ReferenceV3);
                });
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_MissingDiscriminatorRemainsAnyOf()
    {
        var builder = CreateBuilder();
        builder.MapPost("/api", (Organism organism) => { });

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var schema = document.Paths["/api"].Operations[HttpMethod.Post].RequestBody.Content["application/json"].Schema;
            Assert.NotEmpty(schema.AnyOf);
            Assert.Null(schema.OneOf);
            Assert.Null(schema.Discriminator);
        });
    }

    [Fact]
    public void SchemaGenerationMode_Inferred_DuplicateDiscriminatorRemainsAnyOf()
    {
        var alternatives = new JsonArray(new JsonObject(), new JsonObject());
        var schema = new JsonObject
        {
            [OpenApiSchemaKeywords.AnyOfKeyword] = alternatives,
        };
        var decision = new InferredSchemaCompositionDecision(
            new(typeof(DuplicateEmitterBase)),
            new(false, null, InferredInheritanceReason.NoBaseType),
            new(
                InferredAlternativeCompositionKind.AnyOf,
                InferredAlternativeReason.DuplicateDiscriminator,
                "kind",
                [
                    new(new(typeof(DuplicateEmitterOne)), "same"),
                    new(new(typeof(DuplicateEmitterTwo)), "same"),
                ]));

        schema.ApplyCompositionDecision(decision, _ => null, new JsonSerializerOptions());

        Assert.Same(alternatives, schema[OpenApiSchemaKeywords.AnyOfKeyword]);
        Assert.Null(schema[OpenApiSchemaKeywords.OneOfKeyword]);
        Assert.Null(schema[OpenApiSchemaKeywords.DiscriminatorKeyword]);
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_CSharpUnionRemainsAnyOf()
    {
        var builder = CreateBuilder();
        builder.MapGet("/api", () => new UnionIntString(42));

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var schema = document.Components.Schemas[nameof(UnionIntString)];
            Assert.Equal(2, schema.AnyOf.Count);
            Assert.Null(schema.OneOf);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_NullablePolymorphicPropertyWrapsComponentReference()
    {
        var builder = CreateBuilder();
        builder.MapGet("/api", () => new NullableShapeContainer());

        var document = await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var container = document.Components.Schemas[nameof(NullableShapeContainer)];
            var property = container.Properties["shape"];
            Assert.Collection(
                property.OneOf,
                branch => Assert.Equal(JsonSchemaType.Null, branch.Type),
                branch => Assert.Equal("Shape", Assert.IsType<OpenApiSchemaReference>(branch).Reference.Id));

            var shape = document.Components.Schemas["Shape"];
            Assert.Equal(2, shape.OneOf.Count);
            Assert.Null(shape.AnyOf);
            Assert.False(shape.Type?.HasFlag(JsonSchemaType.Null) ?? false);
        });

        foreach (var version in new[] { OpenApiSpecVersion.OpenApi3_0, OpenApiSpecVersion.OpenApi3_1, OpenApiSpecVersion.OpenApi3_2 })
        {
            var serialized = JsonNode.Parse(await document.SerializeAsJsonAsync(version));
            Assert.NotNull(serialized?["components"]?["schemas"]?["Shape"]?["oneOf"]);
            Assert.NotNull(serialized?["components"]?["schemas"]?[nameof(NullableShapeContainer)]?["properties"]?["shape"]?["oneOf"]);
        }
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_RequiresReferenceIdsForDiscriminatorMappings()
    {
        var builder = CreateBuilder();
        builder.MapPost("/api", (Shape shape) => { });
        var options = CreateInferredOptions();
        options.CreateSchemaReferenceId = _ => null;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => VerifyOpenApiDocument(builder, options, _ => { }));
        Assert.Contains("A schema reference ID is required", exception.Message);
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_RecursivePolymorphismIsStable()
    {
        var builder = CreateBuilder();
        builder.MapPost("/api", (Employee employee) => { });

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var schema = document.Paths["/api"].Operations[HttpMethod.Post].RequestBody.Content["application/json"].Schema;
            Assert.Collection(
                schema.OneOf,
                branch => Assert.Equal("EmployeeManager", Assert.IsType<OpenApiSchemaReference>(branch).Reference.Id),
                branch => Assert.Equal("EmployeeEmployee", Assert.IsType<OpenApiSchemaReference>(branch).Reference.Id));
            var employee = document.Components.Schemas["EmployeeEmployee"];
            Assert.Equal("Employee", Assert.IsType<OpenApiSchemaReference>(employee.Properties["manager"]).Reference.Id);
        });
    }

    private static OpenApiOptions CreateInferredOptions()
    {
        var options = new OpenApiOptions();
#pragma warning disable ASP0040
        options.SchemaGenerationMode = OpenApiSchemaGenerationMode.Inferred;
#pragma warning restore ASP0040
        return options;
    }

#nullable enable
    private sealed class NullableShapeContainer
    {
        public Shape? Shape { get; set; }
    }
#nullable disable

    private abstract class DuplicateEmitterBase;

    private sealed class DuplicateEmitterOne : DuplicateEmitterBase;

    private sealed class DuplicateEmitterTwo : DuplicateEmitterBase;
}
