// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
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
                InferredAlternativeSource.Polymorphism,
                InferredAlternativeCompositionKind.AnyOf,
                InferredAlternativeReason.DuplicateDiscriminator,
                "kind",
                [
                    new(new(typeof(DuplicateEmitterOne)), "same", null),
                    new(new(typeof(DuplicateEmitterTwo)), "same", null),
                ]),
            new(InferredObjectContractKind.NotObject, null, null));

        schema.ApplyCompositionDecision(decision, static (_, branchType) => branchType.Name);

        Assert.Same(alternatives, schema[OpenApiSchemaKeywords.AnyOfKeyword]);
        Assert.Null(schema[OpenApiSchemaKeywords.OneOfKeyword]);
        Assert.Null(schema[OpenApiSchemaKeywords.DiscriminatorKeyword]);
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_CSharpUnionUsesOneOfWhenDomainsAreDisjoint()
    {
        var builder = CreateBuilder();
        builder.MapGet("/api", () => new UnionIntString(42));

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var schema = document.Components.Schemas[nameof(UnionIntString)];
            Assert.Null(schema.AnyOf);
            Assert.Collection(
                schema.OneOf,
                branch => Assert.Equal(JsonSchemaType.Integer, branch.Type),
                branch => Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, branch.Type));
            Assert.Null(schema.Discriminator);
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
            Assert.Collection(
                employee.Properties["manager"].OneOf,
                branch => Assert.Equal(JsonSchemaType.Null, branch.Type),
                branch => Assert.Equal("Employee", Assert.IsType<OpenApiSchemaReference>(branch).Reference.Id));
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_UsesAllOfForSafeInheritance()
    {
        Assert.True(BuildCompositionShape<EmitterDerived>().CompositionDecisions[typeof(EmitterDerived)].Inheritance.IsEligible);
        var builder = CreateBuilder();
        builder.MapPost("/api", (EmitterDerived value) => { });

        var document = await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var derived = document.Components.Schemas[nameof(EmitterDerived)];
            Assert.Null(derived.Properties);
            Assert.Collection(
                derived.AllOf,
                branch => Assert.Equal(nameof(EmitterBase), Assert.IsType<OpenApiSchemaReference>(branch).Reference.Id),
                branch =>
                {
                    var local = Assert.IsType<OpenApiSchema>(branch);
                    Assert.Equal(["derived_value"], local.Properties.Keys);
                    Assert.Equal(["derived_value"], local.Required);
                    Assert.Equal(12, local.Properties["derived_value"].MaxLength);
                    Assert.Equal("derived-default", local.Properties["derived_value"].Default.GetValue<string>());
                });

            var baseSchema = document.Components.Schemas[nameof(EmitterBase)];
            Assert.Equal(["base_value"], baseSchema.Properties.Keys);
            Assert.Equal(["base_value"], baseSchema.Required);
            Assert.Equal(8, baseSchema.Properties["base_value"].MaxLength);
        });

        foreach (var version in new[] { OpenApiSpecVersion.OpenApi3_0, OpenApiSpecVersion.OpenApi3_1, OpenApiSpecVersion.OpenApi3_2 })
        {
            var serialized = JsonNode.Parse(await document.SerializeAsJsonAsync(version));
            var allOf = serialized?["components"]?["schemas"]?[nameof(EmitterDerived)]?["allOf"]?.AsArray();
            Assert.Equal(2, allOf?.Count);
            Assert.Equal($"#/components/schemas/{nameof(EmitterBase)}", allOf?[0]?["$ref"]?.GetValue<string>());
        }
    }

    [Fact]
    public async Task SchemaGenerationMode_Legacy_KeepsInheritanceFlattened()
    {
        var builder = CreateBuilder();
        builder.MapPost("/api", (EmitterDerived value) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var derived = document.Components.Schemas[nameof(EmitterDerived)];
            Assert.Null(derived.AllOf);
            Assert.Equal(["derived_value", "base_value"], derived.Properties.Keys);
            Assert.DoesNotContain(nameof(EmitterBase), document.Components.Schemas.Keys);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_ComposesMultiLevelInheritance()
    {
        var builder = CreateBuilder();
        builder.MapPost("/api", (EmitterLeaf value) => { });

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var leaf = document.Components.Schemas[nameof(EmitterLeaf)];
            Assert.Equal(nameof(EmitterMiddle), Assert.IsType<OpenApiSchemaReference>(leaf.AllOf[0]).Reference.Id);
            Assert.Equal(["leaf"], Assert.IsType<OpenApiSchema>(leaf.AllOf[1]).Properties.Keys);

            var middle = document.Components.Schemas[nameof(EmitterMiddle)];
            Assert.Equal(nameof(EmitterBase), Assert.IsType<OpenApiSchemaReference>(middle.AllOf[0]).Reference.Id);
            Assert.Equal(["middle"], Assert.IsType<OpenApiSchema>(middle.AllOf[1]).Properties.Keys);
            Assert.Equal(["base_value"], document.Components.Schemas[nameof(EmitterBase)].Properties.Keys);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_NullableDerivedPropertyWrapsSharedComponent()
    {
        var builder = CreateBuilder();
        builder.MapGet("/api", () => new NullableEmitterDerivedContainer());

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var property = document.Components.Schemas[nameof(NullableEmitterDerivedContainer)].Properties["value"];
            Assert.Collection(
                property.OneOf,
                branch => Assert.Equal(JsonSchemaType.Null, branch.Type),
                branch => Assert.Equal(nameof(EmitterDerived), Assert.IsType<OpenApiSchemaReference>(branch).Reference.Id));

            var derived = document.Components.Schemas[nameof(EmitterDerived)];
            Assert.Equal(2, derived.AllOf.Count);
            Assert.False(derived.Type?.HasFlag(JsonSchemaType.Null) ?? false);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_PolymorphicBaseDoesNotUseAllOf()
    {
        var builder = CreateBuilder();
        builder.MapPost("/api", (Shape shape) => { });

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var shape = document.Paths["/api"].Operations[HttpMethod.Post].RequestBody.Content["application/json"].Schema;
            Assert.Collection(
                shape.OneOf,
                branch =>
                {
                    var triangle = Assert.IsType<OpenApiSchemaReference>(branch);
                    Assert.Null(document.Components.Schemas[triangle.Reference.Id].AllOf);
                },
                branch =>
                {
                    var square = Assert.IsType<OpenApiSchemaReference>(branch);
                    Assert.Null(document.Components.Schemas[square.Reference.Id].AllOf);
                });
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_ReusesCustomNamedBaseIndependentOfEndpointOrder()
    {
        var first = await CreateDocument(reverseEndpoints: false);
        var second = await CreateDocument(reverseEndpoints: true);

        var firstSchemas = JsonNode.Parse(await first.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1))?["components"]?["schemas"];
        var secondSchemas = JsonNode.Parse(await second.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1))?["components"]?["schemas"];
        Assert.True(JsonNode.DeepEquals(firstSchemas, secondSchemas));
        Assert.Contains("CustomEmitterBase", first.Components.Schemas.Keys);
        Assert.Equal(
            "CustomEmitterBase",
            Assert.IsType<OpenApiSchemaReference>(first.Components.Schemas["CustomEmitterDerived"].AllOf[0]).Reference.Id);
        Assert.Equal(
            "CustomEmitterBase",
            Assert.IsType<OpenApiSchemaReference>(first.Components.Schemas["CustomEmitterSibling"].AllOf[0]).Reference.Id);

        async Task<OpenApiDocument> CreateDocument(bool reverseEndpoints)
        {
            var builder = CreateBuilder();
            if (reverseEndpoints)
            {
                builder.MapPost("/sibling", (EmitterSibling value) => { });
                builder.MapPost("/derived", (EmitterDerived value) => { });
            }
            else
            {
                builder.MapPost("/derived", (EmitterDerived value) => { });
                builder.MapPost("/sibling", (EmitterSibling value) => { });
            }

            var options = CreateInferredOptions();
            options.CreateSchemaReferenceId = typeInfo => $"Custom{typeInfo.Type.Name}";
            return await VerifyOpenApiDocument(builder, options, _ => { });
        }
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_RealBaseReplacesPlaceholderIndependentOfEndpointOrder()
    {
        var derivedFirst = await CreateDocument(reverseEndpoints: false);
        var baseFirst = await CreateDocument(reverseEndpoints: true);

        var derivedFirstSchemas = JsonNode.Parse(await derivedFirst.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1))?["components"]?["schemas"];
        var baseFirstSchemas = JsonNode.Parse(await baseFirst.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1))?["components"]?["schemas"];
        Assert.True(JsonNode.DeepEquals(derivedFirstSchemas, baseFirstSchemas));
        Assert.Equal("Emitter base", derivedFirst.Components.Schemas[nameof(EmitterBase)].Description);

        async Task<OpenApiDocument> CreateDocument(bool reverseEndpoints)
        {
            var builder = CreateBuilder();
            if (reverseEndpoints)
            {
                builder.MapPost("/base", (EmitterBase value) => { });
                builder.MapPost("/derived", (EmitterDerived value) => { });
            }
            else
            {
                builder.MapPost("/derived", (EmitterDerived value) => { });
                builder.MapPost("/base", (EmitterBase value) => { });
            }

            return await VerifyOpenApiDocument(builder, CreateInferredOptions(), _ => { });
        }
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_RecursiveInheritanceIsFinite()
    {
        var builder = CreateBuilder();
        builder.MapPost("/api", (RecursiveEmitterDerived value) => { });

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var derived = document.Components.Schemas[nameof(RecursiveEmitterDerived)];
            Assert.Equal(nameof(EmitterBase), Assert.IsType<OpenApiSchemaReference>(derived.AllOf[0]).Reference.Id);
            var local = Assert.IsType<OpenApiSchema>(derived.AllOf[1]);
            Assert.Collection(
                local.Properties["next"].OneOf,
                branch => Assert.Equal(JsonSchemaType.Null, branch.Type),
                branch => Assert.Equal(
                    nameof(RecursiveEmitterDerived),
                    Assert.IsType<OpenApiSchemaReference>(branch).Reference.Id));
        });
    }

    [Theory]
    [InlineData((int)InferredInheritanceReason.BaseShapeUnavailable)]
    [InlineData((int)InferredInheritanceReason.PropertyNameCollision)]
    [InlineData((int)InferredInheritanceReason.PropertyHiding)]
    [InlineData((int)InferredInheritanceReason.CustomConverter)]
    [InlineData((int)InferredInheritanceReason.ExtensionData)]
    [InlineData((int)InferredInheritanceReason.AdditionalProperties)]
    [InlineData((int)InferredInheritanceReason.BaseContractMismatch)]
    [InlineData((int)InferredInheritanceReason.PolymorphicHierarchy)]
    public void SchemaGenerationMode_Inferred_RejectedInheritanceDoesNotModifySchema(int reasonValue)
    {
        var schema = new JsonObject
        {
            [OpenApiSchemaKeywords.TypeKeyword] = "object",
            [OpenApiSchemaKeywords.PropertiesKeyword] = new JsonObject
            {
                ["value"] = new JsonObject
                {
                    [OpenApiSchemaKeywords.TypeKeyword] = "string",
                },
            },
        };
        var original = schema.DeepClone();
        var inferredSchema = BuildCompositionShape<EmitterDerived>();
        var decision = new InferredSchemaCompositionDecision(
            new(typeof(EmitterDerived)),
            new(false, new(typeof(EmitterBase)), (InferredInheritanceReason)reasonValue),
            inferredSchema.CompositionDecisions[typeof(EmitterDerived)].Alternatives,
            inferredSchema.CompositionDecisions[typeof(EmitterDerived)].ObjectContract);

        schema.ApplyInheritanceCompositionDecision(
            inferredSchema,
            decision,
            typeInfo => typeInfo.Type.Name,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.True(JsonNode.DeepEquals(original, schema));
    }

    [Fact]
    public void SchemaGenerationMode_Inferred_InheritanceMismatchThrows()
    {
        var inferredSchema = BuildCompositionShape<EmitterDerived>();
        var schema = new JsonObject
        {
            [OpenApiSchemaKeywords.TypeKeyword] = "object",
            [OpenApiSchemaKeywords.PropertiesKeyword] = new JsonObject(),
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            schema.ApplyInheritanceCompositionDecision(
                inferredSchema,
                inferredSchema.CompositionDecisions[typeof(EmitterDerived)],
                typeInfo => typeInfo.Type.Name,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        Assert.Contains("do not match the exported schema", exception.Message);
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_RejectedContractsRemainFlattened()
    {
        var builder = CreateBuilder();
        builder.MapPost("/hidden", (HiddenDerived value) => { });
        builder.MapPost("/extension-data", (ExtensionDataDerived value) => { });
        builder.MapPost("/additional-properties", (DisallowingDerived value) => { });

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            Assert.Null(document.Components.Schemas[nameof(HiddenDerived)].AllOf);
            Assert.Null(document.Components.Schemas[nameof(ExtensionDataDerived)].AllOf);
            Assert.Null(document.Components.Schemas[nameof(DisallowingDerived)].AllOf);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_InlineNullableDerivedRetainsNullability()
    {
        var builder = CreateBuilder();
        builder.MapGet("/api", () => new NullableEmitterDerivedContainer());
        var options = CreateInferredOptions();
        options.CreateSchemaReferenceId = typeInfo =>
            typeInfo.Type == typeof(EmitterDerived) ? null : typeInfo.Type.Name;

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var property = document.Components.Schemas[nameof(NullableEmitterDerivedContainer)].Properties["value"];
            Assert.NotNull(property.AllOf);
            Assert.True(property.Type?.HasFlag(JsonSchemaType.Null) ?? false);
            Assert.True(property.Type?.HasFlag(JsonSchemaType.Object) ?? false);
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

    [Description("Emitter base")]
    private class EmitterBase
    {
        [JsonRequired]
        [JsonPropertyName("base_value")]
        [StringLength(8)]
        public string BaseValue { get; set; } = string.Empty;
    }

    private class EmitterDerived : EmitterBase
    {
        [JsonRequired]
        [JsonPropertyName("derived_value")]
        [StringLength(12)]
        [DefaultValue("derived-default")]
        public string DerivedValue { get; set; } = string.Empty;
    }

    private class EmitterMiddle : EmitterBase
    {
        public int Middle { get; set; }
    }

    private sealed class EmitterLeaf : EmitterMiddle
    {
        public bool Leaf { get; set; }
    }

    private sealed class EmitterSibling : EmitterBase
    {
        public decimal Sibling { get; set; }
    }

    private sealed class RecursiveEmitterDerived : EmitterBase
    {
        public RecursiveEmitterDerived Next { get; set; }
    }

#nullable enable
    private sealed class NullableEmitterDerivedContainer
    {
        public EmitterDerived? Value { get; set; }
    }
#nullable disable
}
