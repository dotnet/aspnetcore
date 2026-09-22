// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;

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
                InferredJsonValueDomain.Integer | InferredJsonValueDomain.String,
                InferredJsonValueDomain.Integer | InferredJsonValueDomain.NonIntegerNumber | InferredJsonValueDomain.String,
            ]);
        AssertUnionDecision<UnionIntDecimal>(
            InferredAlternativeCompositionKind.AnyOf,
            InferredAlternativeReason.UnionCasesHaveOverlappingJsonDomains,
            [
                InferredJsonValueDomain.Integer | InferredJsonValueDomain.String,
                InferredJsonValueDomain.Integer | InferredJsonValueDomain.NonIntegerNumber | InferredJsonValueDomain.String,
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
        AssertUnionDecision<UnionNullableIntBool>(
            InferredAlternativeCompositionKind.OneOf,
            InferredAlternativeReason.UnionCasesHaveDisjointJsonDomains,
            [
                InferredJsonValueDomain.Integer | InferredJsonValueDomain.String | InferredJsonValueDomain.Null,
                InferredJsonValueDomain.Boolean,
            ]);
        AssertUnionDecision<UnionNullableIntNullableBool>(
            InferredAlternativeCompositionKind.AnyOf,
            InferredAlternativeReason.UnionCasesHaveOverlappingJsonDomains,
            [
                InferredJsonValueDomain.Integer | InferredJsonValueDomain.String | InferredJsonValueDomain.Null,
                InferredJsonValueDomain.Boolean | InferredJsonValueDomain.Null,
            ]);
    }

    [Fact]
    public void InferredShape_NumberHandlingUsesTypeInfoBeforeSerializerOptions()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(typeInfo =>
        {
            if (typeInfo.Type == typeof(int))
            {
                typeInfo.NumberHandling = JsonNumberHandling.Strict;
            }
        });
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            TypeInfoResolver = resolver,
        };

        var document = InferredSchemaShapeBuilder.Build(options, typeof(UnionIntString));
        var decision = document.CompositionDecisions[typeof(UnionIntString)].Alternatives;
        var exportedSchema = JsonSchemaExporter.GetJsonSchemaAsNode(
            options,
            typeof(int),
            new JsonSchemaExporterOptions { TreatNullObliviousAsNonNullable = true });

        Assert.Equal(JsonNumberHandling.Strict, document[typeof(int)].NumberHandling);
        Assert.Equal("integer", exportedSchema[OpenApiSchemaKeywords.TypeKeyword]!.GetValue<string>());
        Assert.Null(exportedSchema[OpenApiSchemaKeywords.AnyOfKeyword]);
        Assert.Equal(InferredAlternativeCompositionKind.OneOf, decision.Kind);
        Assert.Equal(InferredJsonValueDomain.Integer, decision.Branches[0].JsonDomain!.Value.Domains);
    }

    [Fact]
    public void InferredDomains_MatchNumericExporterSchemas()
    {
        AssertNumericDomainMatchesExporter<UnionIntString, int>(JsonNumberHandling.Strict);
        AssertNumericDomainMatchesExporter<UnionIntString, int>(JsonNumberHandling.AllowReadingFromString);
        AssertNumericDomainMatchesExporter<UnionIntString, int>(JsonNumberHandling.WriteAsString);
        AssertNumericDomainMatchesExporter<UnionFloatString, float>(JsonNumberHandling.AllowNamedFloatingPointLiterals);
        AssertNumericDomainMatchesExporter<UnionDoubleString, double>(JsonNumberHandling.AllowReadingFromString);
        AssertNumericDomainMatchesExporter<UnionHalfString, Half>(JsonNumberHandling.WriteAsString);
        AssertNumericDomainMatchesExporter<UnionDecimalString, decimal>(JsonNumberHandling.AllowNamedFloatingPointLiterals);
        AssertNumericDomainMatchesExporter<UnionDecimalString, decimal>(JsonNumberHandling.AllowReadingFromString);
    }

    [Fact]
    public void SchemaGenerationMode_Inferred_UnionDecisionMismatchThrows()
    {
        var inferredSchema = BuildUnionShape<ReverseShapeUnion>(JsonNumberHandling.Strict);
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
        var services = new ServiceCollection();
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
        });
        var builder = CreateBuilder(services, numberHandling: null);
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
    public async Task SchemaGenerationMode_Inferred_WebNumberHandlingKeepsStringAndIntAsAnyOf()
    {
        var builder = CreateBuilder(numberHandling: null);
        builder.MapGet("/", () => new UnionIntString(42));

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var schema = document.Components.Schemas[nameof(UnionIntString)];
            Assert.Null(schema.OneOf);
            Assert.Collection(
                schema.AnyOf,
                branch => Assert.Equal(JsonSchemaType.Integer | JsonSchemaType.String, branch.Type),
                branch => Assert.Equal(JsonSchemaType.Null | JsonSchemaType.String, branch.Type));
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_WebNumberHandlingStillProvesBoolAndInt()
    {
        var builder = CreateBuilder(numberHandling: null);
        builder.MapPost("/", (UnionBoolInt value) => { });

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var schema = document.Components.Schemas[nameof(UnionBoolInt)];
            Assert.Null(schema.AnyOf);
            Assert.Collection(
                schema.OneOf,
                branch => Assert.Equal(JsonSchemaType.Boolean, branch.Type),
                branch => Assert.Equal(JsonSchemaType.Integer | JsonSchemaType.String, branch.Type));
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_WriteAsStringKeepsIntAndStringAsAnyOf()
    {
        var services = new ServiceCollection();
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.NumberHandling = JsonNumberHandling.WriteAsString;
        });
        var builder = CreateBuilder(services, numberHandling: null);
        builder.MapGet("/", () => new UnionIntString(42));

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var schema = document.Components.Schemas[nameof(UnionIntString)];
            Assert.Equal(2, schema.AnyOf.Count);
            Assert.Null(schema.OneOf);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_NamedFloatingPointLiteralsAffectOnlyIeeeFloats()
    {
        var builder = CreateBuilder(numberHandling: JsonNumberHandling.AllowNamedFloatingPointLiterals);
        builder.MapPost("/float", (UnionFloatString value) => { });
        builder.MapPost("/decimal", (UnionDecimalString value) => { });

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var floatSchema = document.Components.Schemas[nameof(UnionFloatString)];
            Assert.Equal(2, floatSchema.AnyOf.Count);
            Assert.Null(floatSchema.OneOf);

            var decimalSchema = document.Components.Schemas[nameof(UnionDecimalString)];
            Assert.Null(decimalSchema.AnyOf);
            Assert.Equal(2, decimalSchema.OneOf.Count);
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

    private static void AssertNumericDomainMatchesExporter<TUnion, TNumber>(JsonNumberHandling numberHandling)
    {
        var document = BuildUnionShape<TUnion>(numberHandling);
        var inferredDomain = document.CompositionDecisions[typeof(TUnion)].Alternatives.Branches
            .Single(branch => branch.Identity.Type == typeof(TNumber))
            .JsonDomain!.Value;

        Assert.Equal(numberHandling, document[typeof(TNumber)].NumberHandling);
        Assert.True(inferredDomain.IsExact);
        Assert.Equal(GetExporterDomain(typeof(TNumber), numberHandling), inferredDomain.Domains);
    }

    private static InferredSchemaDocument BuildUnionShape<T>(JsonNumberHandling numberHandling)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            NumberHandling = numberHandling,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
        return InferredSchemaShapeBuilder.Build(options, typeof(T));
    }

    private static InferredJsonValueDomain GetExporterDomain(Type type, JsonNumberHandling numberHandling)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            NumberHandling = numberHandling,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
        var schema = JsonSchemaExporter.GetJsonSchemaAsNode(
            options,
            type,
            new JsonSchemaExporterOptions { TreatNullObliviousAsNonNullable = true });

        return ReadDomain(schema);

        static InferredJsonValueDomain ReadDomain(JsonNode schema)
        {
            if (schema is not JsonObject schemaObject)
            {
                return InferredJsonValueDomain.Any;
            }

            var domains = InferredJsonValueDomain.None;
            if (schemaObject[OpenApiSchemaKeywords.TypeKeyword] is JsonValue typeValue)
            {
                domains |= ReadType(typeValue.GetValue<string>());
            }
            else if (schemaObject[OpenApiSchemaKeywords.TypeKeyword] is JsonArray types)
            {
                foreach (var typeNode in types)
                {
                    domains |= ReadType(typeNode!.GetValue<string>());
                }
            }

            if (schemaObject[OpenApiSchemaKeywords.AnyOfKeyword] is JsonArray alternatives)
            {
                foreach (var alternative in alternatives)
                {
                    domains |= ReadDomain(alternative!);
                }
            }

            if (schemaObject[OpenApiSchemaKeywords.EnumKeyword] is JsonArray values &&
                values.All(value => value?.GetValueKind() == JsonValueKind.String))
            {
                domains |= InferredJsonValueDomain.String;
            }

            return domains;
        }

        static InferredJsonValueDomain ReadType(string type) => type switch
        {
            "integer" => InferredJsonValueDomain.Integer,
            "number" => InferredJsonValueDomain.Integer | InferredJsonValueDomain.NonIntegerNumber,
            "string" => InferredJsonValueDomain.String,
            _ => InferredJsonValueDomain.Any,
        };
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

internal union UnionBoolInt(bool, int);

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

internal union UnionNullableIntBool(int?, bool);

internal union UnionNullableIntNullableBool(int?, bool?);

internal union UnionFloatString(float, string);

internal union UnionDoubleString(double, string);

internal union UnionHalfString(Half, string);

internal union UnionDecimalString(decimal, string);

internal union NestedDisjointUnion(UnionBoolString, int[]);

internal union RecursiveDisjointUnion(OpenApiSchemaServiceTests.RecursiveUnionNode, string);

#nullable disable
