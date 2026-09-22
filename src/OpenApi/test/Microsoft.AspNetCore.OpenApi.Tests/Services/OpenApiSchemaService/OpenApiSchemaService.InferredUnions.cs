// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;

public partial class OpenApiSchemaServiceTests
{
    [Fact]
    public void CompositionDecision_UnionDomainsProveOnlyPairwiseDisjointCases()
    {
        AssertUnionDecision<UnionBoolString>(
            InferredAlternativeCompositionKind.OneOf,
            InferredAlternativeReason.UnionCasesHaveDisjointJsonDomains,
            [InferredJsonValueDomain.Boolean, InferredJsonValueDomain.String]);
        AssertUnionDecision<UnionObjectArray>(
            InferredAlternativeCompositionKind.OneOf,
            InferredAlternativeReason.UnionCasesHaveDisjointJsonDomains,
            [InferredJsonValueDomain.Object, InferredJsonValueDomain.Array]);
        AssertUnionDecision<UnionIntDouble>(
            InferredAlternativeCompositionKind.AnyOf,
            InferredAlternativeReason.UnionCasesHaveOverlappingJsonDomains,
            [
                InferredJsonValueDomain.Integer,
                InferredJsonValueDomain.Integer | InferredJsonValueDomain.NonIntegerNumber,
            ]);
        AssertUnionDecision<UnionIntDecimal>(
            InferredAlternativeCompositionKind.AnyOf,
            InferredAlternativeReason.UnionCasesHaveOverlappingJsonDomains,
            [
                InferredJsonValueDomain.Integer,
                InferredJsonValueDomain.Integer | InferredJsonValueDomain.NonIntegerNumber,
            ]);
        AssertUnionDecision<UnionTwoObjects>(
            InferredAlternativeCompositionKind.AnyOf,
            InferredAlternativeReason.UnionCasesHaveOverlappingJsonDomains,
            [InferredJsonValueDomain.Object, InferredJsonValueDomain.Object]);
        AssertUnionDecision<UnionTwoArrays>(
            InferredAlternativeCompositionKind.AnyOf,
            InferredAlternativeReason.UnionCasesHaveOverlappingJsonDomains,
            [InferredJsonValueDomain.Array, InferredJsonValueDomain.Array]);
    }

    [Fact]
    public void CompositionDecision_UnionUnknownDomainsRemainAnyOf()
    {
        AssertUnknownUnionDecision<UnionConvertedString>(InferredJsonDomainReason.CustomConverter);
        AssertUnknownUnionDecision<UnionJsonElementString>(InferredJsonDomainReason.ArbitraryJsonValue);
        AssertUnknownUnionDecision<UnionObjectString>(InferredJsonDomainReason.ArbitraryJsonValue);
        AssertUnknownUnionDecision<UnionEnumInt>(InferredJsonDomainReason.EnumRepresentation);
    }

    [Fact]
    public void CompositionDecision_StringEnumAndStringRemainAnyOf()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
        options.Converters.Add(new JsonStringEnumConverter());

        var document = InferredSchemaShapeBuilder.Build(options, typeof(UnionEnumString));
        var decision = document.CompositionDecisions[typeof(UnionEnumString)].Alternatives;

        Assert.Equal(InferredAlternativeCompositionKind.AnyOf, decision.Kind);
        Assert.Equal(InferredAlternativeReason.UnionCaseJsonDomainIsUnknown, decision.Reason);
        Assert.Equal(
            InferredJsonDomainReason.EnumRepresentation,
            decision.Branches[0].JsonDomain!.Value.Reason);
        Assert.Equal(InferredJsonValueDomain.String, decision.Branches[1].JsonDomain!.Value.Domains);
    }

    [Fact]
    public void CompositionDecision_UnionNullableDomainsIncludeNull()
    {
        AssertUnionDecision<UnionNullableIntString>(
            InferredAlternativeCompositionKind.OneOf,
            InferredAlternativeReason.UnionCasesHaveDisjointJsonDomains,
            [InferredJsonValueDomain.Integer | InferredJsonValueDomain.Null, InferredJsonValueDomain.String]);
        AssertUnionDecision<UnionNullableIntNullableBool>(
            InferredAlternativeCompositionKind.AnyOf,
            InferredAlternativeReason.UnionCasesHaveOverlappingJsonDomains,
            [
                InferredJsonValueDomain.Integer | InferredJsonValueDomain.Null,
                InferredJsonValueDomain.Boolean | InferredJsonValueDomain.Null,
            ]);
    }

    [Fact]
    public void SchemaGenerationMode_Inferred_UnionDecisionMismatchThrows()
    {
        var inferredSchema = BuildCompositionShape<ReverseShapeUnion>();
        var schema = new JsonObject
        {
            [OpenApiSchemaKeywords.AnyOfKeyword] = new JsonArray(new JsonObject()),
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => schema.ApplyCompositionDecision(
                inferredSchema.CompositionDecisions[typeof(ReverseShapeUnion)],
                static (_, _) => null));

        Assert.Contains("do not match", exception.Message);
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_DisjointUnionUsesOrderedOneOfWithoutDiscriminator()
    {
        var builder = CreateBuilder();
        builder.MapGet("/", () => new ReverseShapeUnion("value"));

        var document = await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var schema = Assert.IsType<OpenApiSchema>(document.Components.Schemas[nameof(ReverseShapeUnion)]);
            Assert.Null(schema.AnyOf);
            Assert.Collection(
                schema.OneOf,
                branch => Assert.Equal(JsonSchemaType.String, branch.Type),
                branch => Assert.Equal(JsonSchemaType.Integer, branch.Type));
            Assert.Null(schema.Discriminator);
            Assert.True((bool)schema.Metadata[Microsoft.AspNetCore.OpenApi.OpenApiConstants.SchemaIsInferredUnion]);
            Assert.False(schema.Metadata.ContainsKey(Microsoft.AspNetCore.OpenApi.OpenApiConstants.SchemaIsInferredPolymorphism));
        });

        foreach (var version in new[] { OpenApiSpecVersion.OpenApi3_0, OpenApiSpecVersion.OpenApi3_1, OpenApiSpecVersion.OpenApi3_2 })
        {
            var serialized = JsonNode.Parse(await document.SerializeAsJsonAsync(version));
            var schema = serialized?["components"]?["schemas"]?[nameof(ReverseShapeUnion)];
            Assert.NotNull(schema?["oneOf"]);
            Assert.Null(schema?["anyOf"]);
            Assert.Null(schema?["discriminator"]);
        }
    }

    [Fact]
    public async Task SchemaGenerationMode_Legacy_DisjointUnionRemainsAnyOf()
    {
        var builder = CreateBuilder();
        builder.MapGet("/", () => new ReverseShapeUnion("value"));

        await VerifyOpenApiDocument(builder, document =>
        {
            var schema = document.Components.Schemas[nameof(ReverseShapeUnion)];
            Assert.Equal(2, schema.AnyOf.Count);
            Assert.Null(schema.OneOf);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_UnprovenUnionsRemainAnyOf()
    {
        var builder = CreateBuilder();
        builder.MapPost("/int-double", (UnionIntDouble value) => { });
        builder.MapPost("/int-decimal", (UnionIntDecimal value) => { });
        builder.MapPost("/objects", (UnionTwoObjects value) => { });
        builder.MapPost("/arrays", (UnionTwoArrays value) => { });
        builder.MapPost("/converted", (UnionConvertedString value) => { });
        builder.MapPost("/element", (UnionJsonElementString value) => { });
        builder.MapPost("/object", (UnionObjectString value) => { });
        builder.MapPost("/enum", (UnionEnumInt value) => { });
        builder.MapPost("/nullable", (UnionNullableIntNullableBool value) => { });

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            foreach (var unionType in new[]
            {
                typeof(UnionIntDouble),
                typeof(UnionIntDecimal),
                typeof(UnionTwoObjects),
                typeof(UnionTwoArrays),
                typeof(UnionConvertedString),
                typeof(UnionJsonElementString),
                typeof(UnionObjectString),
                typeof(UnionNullableIntNullableBool),
            })
            {
                var schema = document.Components.Schemas[unionType.Name];
                Assert.True(schema.AnyOf is { Count: 2 }, unionType.Name);
                Assert.Null(schema.OneOf);
                Assert.Null(schema.Discriminator);
            }

            var enumSchema = document.Components.Schemas[nameof(UnionEnumInt)];
            Assert.Null(enumSchema.OneOf);
            Assert.Null(enumSchema.Discriminator);
            Assert.Equal(JsonSchemaType.Integer, enumSchema.Type);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_ObjectAndArrayUnionUsesStableReferences()
    {
        var first = await CreateDocument(reverse: false);
        var second = await CreateDocument(reverse: true);

        var firstSchemas = JsonNode.Parse(await first.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1))?["components"]?["schemas"];
        var secondSchemas = JsonNode.Parse(await second.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1))?["components"]?["schemas"];
        Assert.True(JsonNode.DeepEquals(firstSchemas, secondSchemas));

        var schema = first.Components.Schemas[nameof(UnionObjectArray)];
        Assert.Null(schema.AnyOf);
        Assert.Collection(
            schema.OneOf,
            branch => Assert.Equal(nameof(UnionDomainObject), Assert.IsType<OpenApiSchemaReference>(branch).Reference.Id),
            branch => Assert.Equal(JsonSchemaType.Integer, branch.Items.Type));

        static Task<OpenApiDocument> CreateDocument(bool reverse)
        {
            var builder = CreateBuilder();
            if (reverse)
            {
                builder.MapPost("/nested", (NestedDisjointUnion value) => { });
                builder.MapPost("/object-array", (UnionObjectArray value) => { });
            }
            else
            {
                builder.MapPost("/object-array", (UnionObjectArray value) => { });
                builder.MapPost("/nested", (NestedDisjointUnion value) => { });
            }

            return VerifyOpenApiDocument(builder, CreateInferredOptions(), _ => { });
        }
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_NestedAndRecursiveUnionsRemainFinite()
    {
        var builder = CreateBuilder();
        builder.MapPost("/nested", (NestedDisjointUnion value) => { });
        builder.MapPost("/recursive", (RecursiveDisjointUnion value) => { });

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var nested = document.Components.Schemas[nameof(NestedDisjointUnion)];
            Assert.Equal(2, nested.OneOf.Count);

            var recursive = document.Components.Schemas[nameof(RecursiveDisjointUnion)];
            Assert.Equal(2, recursive.OneOf.Count);
            Assert.Equal(
                nameof(RecursiveUnionNode),
                Assert.IsType<OpenApiSchemaReference>(recursive.OneOf[0]).Reference.Id);
            Assert.Equal(
                nameof(RecursiveDisjointUnion),
                Assert.IsType<OpenApiSchemaReference>(
                    document.Components.Schemas[nameof(RecursiveUnionNode)].Properties["next"]).Reference.Id);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_NullableUnionPropertyWrapsComponentReference()
    {
        var builder = CreateBuilder();
        builder.MapGet("/", () => new NullableUnionContainer());

        var document = await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var union = document.Components.Schemas[nameof(UnionBoolString)];
            Assert.Equal(2, union.OneOf.Count);
            Assert.False(union.Type?.HasFlag(JsonSchemaType.Null) ?? false);

            var property = document.Components.Schemas[nameof(NullableUnionContainer)].Properties["value"];
            Assert.Collection(
                property.OneOf,
                branch => Assert.Equal(JsonSchemaType.Null, branch.Type),
                branch => Assert.Equal(
                    nameof(UnionBoolString),
                    Assert.IsType<OpenApiSchemaReference>(branch).Reference.Id));
        });

        foreach (var version in new[] { OpenApiSpecVersion.OpenApi3_0, OpenApiSpecVersion.OpenApi3_1, OpenApiSpecVersion.OpenApi3_2 })
        {
            var serialized = JsonNode.Parse(await document.SerializeAsJsonAsync(version));
            Assert.NotNull(serialized?["components"]?["schemas"]?[nameof(UnionBoolString)]?["oneOf"]);
            Assert.NotNull(
                serialized?["components"]?["schemas"]?[nameof(NullableUnionContainer)]?["properties"]?["value"]?["oneOf"]);
        }
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_UnionOneOfTransformerVisitsCasesInOrder()
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (ReverseShapeUnion value) => { });
        var contexts = new List<Type>();
        var options = CreateInferredOptions();
        options.AddSchemaTransformer((_, context, _) =>
        {
            contexts.Add(context.JsonTypeInfo.Type);
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, _ => { });

        Assert.Equal([typeof(ReverseShapeUnion), typeof(string), typeof(int)], contexts);
    }

    [Fact]
    public async Task SchemaGenerationMode_Legacy_UnionTransformerTraversalIsUnchanged()
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (ReverseShapeUnion value) => { });
        var contexts = new List<Type>();
        var options = new OpenApiOptions();
        options.AddSchemaTransformer((_, context, _) =>
        {
            contexts.Add(context.JsonTypeInfo.Type);
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, _ => { });

        Assert.Equal([typeof(ReverseShapeUnion)], contexts);
    }

    private static void AssertUnionDecision<T>(
        InferredAlternativeCompositionKind expectedKind,
        InferredAlternativeReason expectedReason,
        InferredJsonValueDomain[] expectedDomains)
    {
        var decision = BuildCompositionShape<T>().CompositionDecisions[typeof(T)].Alternatives;
        Assert.Equal(InferredAlternativeSource.Union, decision.Source);
        Assert.Equal(expectedKind, decision.Kind);
        Assert.Equal(expectedReason, decision.Reason);
        Assert.Equal(expectedDomains, decision.Branches.Select(branch => branch.JsonDomain!.Value.Domains));
    }

    private static void AssertUnknownUnionDecision<T>(InferredJsonDomainReason expectedUnknownReason)
    {
        var decision = BuildCompositionShape<T>().CompositionDecisions[typeof(T)].Alternatives;
        Assert.Equal(InferredAlternativeSource.Union, decision.Source);
        Assert.Equal(InferredAlternativeCompositionKind.AnyOf, decision.Kind);
        Assert.Equal(InferredAlternativeReason.UnionCaseJsonDomainIsUnknown, decision.Reason);
        Assert.Contains(
            decision.Branches,
            branch => branch.JsonDomain is { IsExact: false, Reason: var reason } && reason == expectedUnknownReason);
    }

    internal sealed record UnionDomainObject(int Value);

    internal sealed record OtherUnionDomainObject(string Value);

#nullable enable
    internal sealed record RecursiveUnionNode(RecursiveDisjointUnion Next);

    private sealed class NullableUnionContainer
    {
        public UnionBoolString? Value { get; set; }
    }
#nullable disable

    [JsonConverter(typeof(ConvertedUnionCaseConverter))]
    internal sealed record ConvertedUnionCase(int Value);

    private sealed class ConvertedUnionCaseConverter : JsonConverter<ConvertedUnionCase>
    {
        public override ConvertedUnionCase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new(reader.GetInt32());

        public override void Write(Utf8JsonWriter writer, ConvertedUnionCase value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value.Value);
    }

    internal enum UnionEnum
    {
        One,
        Two,
    }
}

#nullable enable

internal union UnionBoolString(bool, string);

internal union UnionObjectArray(OpenApiSchemaServiceTests.UnionDomainObject, int[]);

internal union UnionIntDouble(int, double);

internal union UnionIntDecimal(int, decimal);

internal union UnionTwoObjects(
    OpenApiSchemaServiceTests.UnionDomainObject,
    OpenApiSchemaServiceTests.OtherUnionDomainObject);

internal union UnionTwoArrays(int[], string[]);

internal union UnionConvertedString(OpenApiSchemaServiceTests.ConvertedUnionCase, string);

internal union UnionJsonElementString(JsonElement, string);

internal union UnionObjectString(object, string);

internal union UnionEnumInt(OpenApiSchemaServiceTests.UnionEnum, int);

internal union UnionEnumString(OpenApiSchemaServiceTests.UnionEnum, string);

internal union UnionNullableIntString(int?, string);

internal union UnionNullableIntNullableBool(int?, bool?);

internal union NestedDisjointUnion(UnionBoolString, int[]);

internal union RecursiveDisjointUnion(OpenApiSchemaServiceTests.RecursiveUnionNode, string);

#nullable disable
