// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;

public partial class OpenApiSchemaServiceTests
{
    [Fact]
    public void InferredFiniteDomain_EnumExporterSemanticsAreLocked()
    {
        var numeric = ExportSchema<NumericFiniteEnum>();
        Assert.Equal("integer", numeric[OpenApiSchemaKeywords.TypeKeyword]!.GetValue<string>());
        Assert.Null(numeric[OpenApiSchemaKeywords.EnumKeyword]);
        Assert.Equal("99", JsonSerializer.Serialize((NumericFiniteEnum)99));

        var closed = ExportSchema<ClosedAlphaEnum>();
        Assert.Null(closed[OpenApiSchemaKeywords.TypeKeyword]);
        Assert.Equal(
            ["first-value", "shared-value"],
            closed[OpenApiSchemaKeywords.EnumKeyword]!.AsArray().Select(value => value!.GetValue<string>()));
        Assert.Throws<JsonException>(() => JsonSerializer.Serialize((ClosedAlphaEnum)99));

        var open = ExportSchema<OpenStringEnum>();
        Assert.Null(open[OpenApiSchemaKeywords.TypeKeyword]);
        Assert.Equal(
            ["FirstValue", "SecondValue"],
            open[OpenApiSchemaKeywords.EnumKeyword]!.AsArray().Select(value => value!.GetValue<string>()));
        Assert.Equal("99", JsonSerializer.Serialize((OpenStringEnum)99));

        var flags = ExportSchema<ClosedFlagsEnum>();
        Assert.Equal("string", flags[OpenApiSchemaKeywords.TypeKeyword]!.GetValue<string>());
        Assert.Null(flags[OpenApiSchemaKeywords.EnumKeyword]);
        Assert.Throws<JsonException>(() => JsonSerializer.Serialize((ClosedFlagsEnum)8));

        var nullable = ExportSchema<ClosedAlphaEnum?>();
        Assert.Equal(
            ["first-value", "shared-value", null],
            nullable[OpenApiSchemaKeywords.EnumKeyword]!.AsArray().Select(value => value?.GetValue<string>()));
    }

    [Fact]
    public void InferredFiniteDomain_FactsAreExactOnlyForClosedSerializerSchemas()
    {
        var closed = BuildCompositionShape<ClosedAlphaEnum>()[typeof(ClosedAlphaEnum)].FiniteDomain!;
        Assert.True(closed.IsExact);
        Assert.Equal(InferredJsonFiniteDomainReason.SchemaEnum, closed.Reason);
        Assert.Equal(
            [
                new(InferredJsonLiteralKind.String, "first-value"),
                new(InferredJsonLiteralKind.String, "shared-value"),
            ],
            closed.Values);

        AssertOpenFiniteDomain<NumericFiniteEnum>(InferredJsonFiniteDomainReason.IntegerValuesAccepted);
        AssertOpenFiniteDomain<OpenStringEnum>(InferredJsonFiniteDomainReason.IntegerValuesAccepted);
        AssertOpenFiniteDomain<ClosedFlagsEnum>(InferredJsonFiniteDomainReason.FlagsEnum);
        AssertOpenFiniteDomain<CustomConvertedEnum>(InferredJsonFiniteDomainReason.CustomConverter);
    }

    [Fact]
    public void CompositionDecision_FiniteEnumSetsUseLiteralIntersection()
    {
        AssertFiniteUnionDecision<UnionDisjointClosedEnums>(
            InferredAlternativeCompositionKind.OneOf,
            InferredAlternativeReason.UnionCasesHaveDisjointJsonDomains);
        AssertFiniteUnionDecision<UnionOverlappingClosedEnums>(
            InferredAlternativeCompositionKind.AnyOf,
            InferredAlternativeReason.UnionCasesHaveOverlappingJsonDomains);
        AssertFiniteUnionDecision<UnionClosedEnumString>(
            InferredAlternativeCompositionKind.AnyOf,
            InferredAlternativeReason.UnionCasesHaveOverlappingJsonDomains);
        AssertFiniteUnionDecision<UnionClosedEnumBool>(
            InferredAlternativeCompositionKind.OneOf,
            InferredAlternativeReason.UnionCasesHaveDisjointJsonDomains);
        AssertFiniteUnionDecision<UnionNullableClosedEnums>(
            InferredAlternativeCompositionKind.AnyOf,
            InferredAlternativeReason.UnionCasesHaveOverlappingJsonDomains);
    }

    [Fact]
    public void CompositionDecision_OpenEnumsRemainAnyOf()
    {
        AssertOpenEnumUnion<UnionNumericEnumInt>();
        AssertOpenEnumUnion<UnionNumericEnumDouble>();
        AssertOpenEnumUnion<UnionOpenStringEnumBool>();
        AssertOpenEnumUnion<UnionFlagsEnumBool>();
        AssertOpenEnumUnion<UnionCustomEnumBool>();
    }

    [Theory]
    [InlineData("1", "1.0")]
    [InlineData("1", "10e-1")]
    [InlineData("-12.50", "-125e-1")]
    [InlineData("0", "-0.0")]
    public void InferredJsonLiteral_NumbersUseJsonSchemaMathematicalEquality(string left, string right)
    {
        Assert.Equal(
            InferredJsonLiteral.Create(JsonNode.Parse(left)),
            InferredJsonLiteral.Create(JsonNode.Parse(right)));
    }

    [Fact]
    public void InferredJsonLiteral_PreservesNonNumericLiteralKinds()
    {
        Assert.Equal(
            new(InferredJsonLiteralKind.Null, "null"),
            InferredJsonLiteral.Create(null));
        Assert.Equal(
            new(InferredJsonLiteralKind.Boolean, "true"),
            InferredJsonLiteral.Create(JsonValue.Create(true)));
        Assert.Equal(
            new(InferredJsonLiteralKind.String, "1"),
            InferredJsonLiteral.Create(JsonValue.Create("1")));
        Assert.NotEqual(
            InferredJsonLiteral.Create(JsonValue.Create("1")),
            InferredJsonLiteral.Create(JsonNode.Parse("1")));
    }

    [Fact]
    public void InferredFiniteDomain_ExtractsSupportedConstAndEnumSchemas()
    {
        var constant = InferredJsonFiniteDomainBuilder.ExtractSchema(
            JsonNode.Parse("""{"const":1.0}""")!);
        Assert.True(constant.IsExact);
        Assert.Equal(InferredJsonFiniteDomainReason.SchemaConst, constant.Reason);
        Assert.Equal([new(InferredJsonLiteralKind.Number, "1e0")], constant.Values);

        var nullConstant = InferredJsonFiniteDomainBuilder.ExtractSchema(
            JsonNode.Parse("""{"const":null}""")!);
        Assert.True(nullConstant.IsExact);
        Assert.Equal([new(InferredJsonLiteralKind.Null, "null")], nullConstant.Values);

        var values = InferredJsonFiniteDomainBuilder.ExtractSchema(
            JsonNode.Parse("""{"enum":[true,null,"value",1,1.0]}""")!);
        Assert.True(values.IsExact);
        Assert.Equal(
            [
                new(InferredJsonLiteralKind.Null, "null"),
                new(InferredJsonLiteralKind.Boolean, "true"),
                new(InferredJsonLiteralKind.Number, "1e0"),
                new(InferredJsonLiteralKind.String, "value"),
            ],
            values.Values);

        var unsupported = InferredJsonFiniteDomainBuilder.ExtractSchema(
            JsonNode.Parse("""{"enum":["value"],"const":"value"}""")!);
        Assert.False(unsupported.IsExact);
        Assert.Equal(InferredJsonFiniteDomainReason.UnsupportedSchema, unsupported.Reason);
    }

    [Fact]
    public void InferredFiniteDomain_ReflectionAndSourceGeneratedMetadataAgree()
    {
        var reflectionOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
        var reflectionTypeInfo = reflectionOptions.GetTypeInfo(typeof(ClosedAlphaEnum));
        var sourceGeneratedTypeInfo = InferredEnumJsonContext.Default.GetTypeInfo(typeof(ClosedAlphaEnum))!;
        var reflection = InferredJsonFiniteDomainBuilder.Build(reflectionTypeInfo, hasCustomConverter: false)!;
        var sourceGenerated = InferredJsonFiniteDomainBuilder.Build(sourceGeneratedTypeInfo, hasCustomConverter: false)!;

        Assert.Equal(reflection.IsExact, sourceGenerated.IsExact);
        Assert.Equal(reflection.Reason, sourceGenerated.Reason);
        Assert.Equal(reflection.Values, sourceGenerated.Values);
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_DisjointFiniteEnumsUseOrderedOneOf()
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (UnionDisjointClosedEnums value) => { });
        var contexts = new List<Type>();
        var options = CreateInferredOptions();
        options.AddSchemaTransformer((_, context, _) =>
        {
            contexts.Add(context.JsonTypeInfo.Type);
            return Task.CompletedTask;
        });

        var document = await VerifyOpenApiDocument(builder, options, document =>
        {
            var schema = document.Components.Schemas[nameof(UnionDisjointClosedEnums)];
            Assert.Null(schema.AnyOf);
            Assert.Collection(
                schema.OneOf,
                branch => Assert.Equal(nameof(ClosedAlphaEnum), Assert.IsType<OpenApiSchemaReference>(branch).Reference.Id),
                branch => Assert.Equal(nameof(ClosedGammaEnum), Assert.IsType<OpenApiSchemaReference>(branch).Reference.Id));
            Assert.Null(schema.Discriminator);
            Assert.Equal(
                ["first-value", "shared-value"],
                document.Components.Schemas[nameof(ClosedAlphaEnum)].Enum.Select(value => value.GetValue<string>()));
            Assert.Equal(
                ["third-value", "fourth-value"],
                document.Components.Schemas[nameof(ClosedGammaEnum)].Enum.Select(value => value.GetValue<string>()));
        });

        Assert.Equal(
            [typeof(UnionDisjointClosedEnums), typeof(ClosedAlphaEnum), typeof(ClosedGammaEnum)],
            contexts);

        foreach (var version in new[] { OpenApiSpecVersion.OpenApi3_0, OpenApiSpecVersion.OpenApi3_1, OpenApiSpecVersion.OpenApi3_2 })
        {
            var serialized = JsonNode.Parse(await document.SerializeAsJsonAsync(version));
            var schema = serialized?["components"]?["schemas"]?[nameof(UnionDisjointClosedEnums)];
            Assert.NotNull(schema?["oneOf"]);
            Assert.Null(schema?["anyOf"]);
            Assert.Null(schema?["discriminator"]);
        }
    }

    [Fact]
    public async Task SchemaGenerationMode_Legacy_DisjointFiniteEnumsRemainAnyOf()
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (UnionDisjointClosedEnums value) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var schema = document.Components.Schemas[nameof(UnionDisjointClosedEnums)];
            Assert.Equal(2, schema.AnyOf.Count);
            Assert.Null(schema.OneOf);
        });
    }

    private static JsonNode ExportSchema<T>()
    {
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
        return JsonSchemaExporter.GetJsonSchemaAsNode(
            options,
            typeof(T),
            new JsonSchemaExporterOptions { TreatNullObliviousAsNonNullable = true });
    }

    private static void AssertOpenFiniteDomain<T>(InferredJsonFiniteDomainReason expectedReason)
    {
        var fact = BuildCompositionShape<T>()[typeof(T)].FiniteDomain!;
        Assert.False(fact.IsExact);
        Assert.Equal(expectedReason, fact.Reason);
        Assert.Empty(fact.Values);
    }

    private static void AssertFiniteUnionDecision<T>(
        InferredAlternativeCompositionKind expectedKind,
        InferredAlternativeReason expectedReason)
    {
        var decision = BuildCompositionShape<T>().CompositionDecisions[typeof(T)].Alternatives;
        Assert.Equal(expectedKind, decision.Kind);
        Assert.Equal(expectedReason, decision.Reason);
    }

    private static void AssertOpenEnumUnion<T>()
    {
        var decision = BuildCompositionShape<T>().CompositionDecisions[typeof(T)].Alternatives;
        Assert.Equal(InferredAlternativeCompositionKind.AnyOf, decision.Kind);
        Assert.Equal(InferredAlternativeReason.UnionCaseJsonDomainIsUnknown, decision.Reason);
    }
}

[JsonConverter(typeof(ClosedAlphaEnumConverter))]
internal enum ClosedAlphaEnum
{
    FirstValue,
    SharedValue,
}

internal sealed class ClosedAlphaEnumConverter()
    : JsonStringEnumConverter<ClosedAlphaEnum>(JsonNamingPolicy.KebabCaseLower, allowIntegerValues: false);

[JsonConverter(typeof(ClosedBetaEnumConverter))]
internal enum ClosedBetaEnum
{
    OtherValue,

    [JsonStringEnumMemberName("shared-value")]
    AliasedValue,
}

internal sealed class ClosedBetaEnumConverter()
    : JsonStringEnumConverter<ClosedBetaEnum>(JsonNamingPolicy.KebabCaseLower, allowIntegerValues: false);

[JsonConverter(typeof(ClosedGammaEnumConverter))]
internal enum ClosedGammaEnum
{
    ThirdValue,
    FourthValue,
}

internal sealed class ClosedGammaEnumConverter()
    : JsonStringEnumConverter<ClosedGammaEnum>(JsonNamingPolicy.KebabCaseLower, allowIntegerValues: false);

internal enum NumericFiniteEnum
{
    One = 1,
    Two = 2,
}

[JsonConverter(typeof(OpenStringEnumConverter))]
internal enum OpenStringEnum
{
    FirstValue,
    SecondValue,
}

internal sealed class OpenStringEnumConverter()
    : JsonStringEnumConverter<OpenStringEnum>(namingPolicy: null, allowIntegerValues: true);

[Flags]
[JsonConverter(typeof(ClosedFlagsEnumConverter))]
internal enum ClosedFlagsEnum
{
    One = 1,
    Two = 2,
}

internal sealed class ClosedFlagsEnumConverter()
    : JsonStringEnumConverter<ClosedFlagsEnum>(namingPolicy: null, allowIntegerValues: false);

[JsonConverter(typeof(CustomConvertedEnumConverter))]
internal enum CustomConvertedEnum
{
    One,
    Two,
}

internal sealed class CustomConvertedEnumConverter : JsonConverter<CustomConvertedEnum>
{
    public override CustomConvertedEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetString() == "one" ? CustomConvertedEnum.One : CustomConvertedEnum.Two;

    public override void Write(Utf8JsonWriter writer, CustomConvertedEnum value, JsonSerializerOptions options)
        => writer.WriteStringValue(value == CustomConvertedEnum.One ? "one" : "two");
}

internal union UnionDisjointClosedEnums(ClosedAlphaEnum, ClosedGammaEnum);

internal union UnionOverlappingClosedEnums(ClosedAlphaEnum, ClosedBetaEnum);

internal union UnionClosedEnumString(ClosedAlphaEnum, string);

internal union UnionClosedEnumBool(ClosedAlphaEnum, bool);

internal union UnionNullableClosedEnums(ClosedAlphaEnum?, ClosedGammaEnum?);

internal union UnionNumericEnumInt(NumericFiniteEnum, int);

internal union UnionNumericEnumDouble(NumericFiniteEnum, double);

internal union UnionOpenStringEnumBool(OpenStringEnum, bool);

internal union UnionFlagsEnumBool(ClosedFlagsEnum, bool);

internal union UnionCustomEnumBool(CustomConvertedEnum, bool);

[JsonSerializable(typeof(ClosedAlphaEnum))]
internal sealed partial class InferredEnumJsonContext : JsonSerializerContext;
