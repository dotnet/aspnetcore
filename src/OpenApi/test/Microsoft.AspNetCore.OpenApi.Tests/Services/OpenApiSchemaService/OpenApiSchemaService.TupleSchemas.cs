// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;

#nullable enable
#pragma warning disable ASP0040

public partial class OpenApiSchemaServiceTests
{
    [Theory]
    [MemberData(nameof(TupleRoundTripData))]
    public void JsonArrayTupleConverter_RoundTripsSupportedTuples(Type tupleType, object value, string expectedJson)
    {
        var options = CreateTupleSerializerOptions();

        var json = JsonSerializer.Serialize(value, tupleType, options);
        var roundTripped = JsonSerializer.Deserialize(json, tupleType, options);

        Assert.Equal(expectedJson, json);
        Assert.Equal(value, roundTripped);
    }

    public static object[][] TupleRoundTripData =>
    [
        [typeof(ValueTuple), default(ValueTuple), "[]"],
        [typeof(ValueTuple<int>), ValueTuple.Create(1), "[1]"],
        [typeof(Tuple<int>), Tuple.Create(1), "[1]"],
        [typeof((int, string)), (1, "two"), "[1,\"two\"]"],
        [typeof(Tuple<int, string>), Tuple.Create(1, "two"), "[1,\"two\"]"],
        [typeof((int, int, int, int, int, int, int)), (1, 2, 3, 4, 5, 6, 7), "[1,2,3,4,5,6,7]"],
        [typeof((int, int, int, int, int, int, int, int, int)), (1, 2, 3, 4, 5, 6, 7, 8, 9), "[1,2,3,4,5,6,7,8,9]"],
        [
            typeof(Tuple<int, int, int, int, int, int, int, Tuple<int, int>>),
            new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(1, 2, 3, 4, 5, 6, 7, Tuple.Create(8, 9)),
            "[1,2,3,4,5,6,7,8,9]"
        ],
        [typeof((int, (string, bool))), (1, ("two", true)), "[1,[\"two\",true]]"],
    ];

    [Fact]
    public void JsonArrayTupleConverter_UnsupportedTypeUsesResourceMessage()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => new JsonArrayTupleConverter().CreateConverter(typeof(string), new()));

        Assert.Equal(Resources.FormatTypeNotSupportedTupleType(typeof(string)), exception.Message);
    }

    [Theory]
    [InlineData("[1]", "fewer than 2")]
    [InlineData("[1,\"two\",true]", "more than 2")]
    [InlineData("{}", "Expected a JSON array")]
    public void JsonArrayTupleConverter_RejectsMalformedArityOrToken(string json, string expectedMessage)
    {
        var exception = Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<(int, string)>(json, CreateTupleSerializerOptions()));

        Assert.Contains(expectedMessage, exception.Message);
    }

    [Fact]
    public void JsonArrayTupleConverter_PreservesNullAndElementConverters()
    {
        var options = CreateTupleSerializerOptions();
        options.Converters.Insert(0, new UpperCaseStringConverter());
        var value = Tuple.Create<string?, int>(null, 2);

        Assert.Equal("[null,2]", JsonSerializer.Serialize(value, options));
        Assert.Equal(
            Tuple.Create<string?, int>("VALUE", 2),
            JsonSerializer.Deserialize<Tuple<string?, int>>("[\"value\",2]", options));
        Assert.Equal("null", JsonSerializer.Serialize<Tuple<string, int>?>(null, options));
        Assert.Null(JsonSerializer.Deserialize<Tuple<string, int>?>("null", options));
    }

    [Fact]
    public void JsonArrayTupleConverters_RoundTripClosedContractsWithoutDynamicCode()
    {
        var options = CreateClosedTupleSerializerOptions();
        var valueTuple = (1, "two");
        var referenceTuple = Tuple.Create(1, "two");
        var longReferenceTuple = new Tuple<int, int, int, int, int, int, int, Tuple<int, int>>(
            1, 2, 3, 4, 5, 6, 7, Tuple.Create(8, 9));
        var longTuple = (1, 2, 3, 4, 5, 6, 7, 8, 9);
        var nestedTuple = (1, ("two", true));

        Assert.Equal("[]", JsonSerializer.Serialize(default(ValueTuple), options));
        Assert.Equal(default, JsonSerializer.Deserialize<ValueTuple>("[]", options));
        Assert.Equal("[1,\"two\"]", JsonSerializer.Serialize(valueTuple, options));
        Assert.Equal(valueTuple, JsonSerializer.Deserialize<(int, string)>("[1,\"two\"]", options));
        Assert.Equal("[1,\"two\"]", JsonSerializer.Serialize(referenceTuple, options));
        Assert.Equal(referenceTuple, JsonSerializer.Deserialize<Tuple<int, string>>("[1,\"two\"]", options));
        Assert.Equal("[1,2,3,4,5,6,7,8,9]", JsonSerializer.Serialize(longReferenceTuple, options));
        Assert.Equal(
            longReferenceTuple,
            JsonSerializer.Deserialize<Tuple<int, int, int, int, int, int, int, Tuple<int, int>>>("[1,2,3,4,5,6,7,8,9]", options));
        Assert.Equal("[1,2,3,4,5,6,7,8,9]", JsonSerializer.Serialize(longTuple, options));
        Assert.Equal(longTuple, JsonSerializer.Deserialize<(int, int, int, int, int, int, int, int, int)>("[1,2,3,4,5,6,7,8,9]", options));
        Assert.Equal("[1,[\"two\",true]]", JsonSerializer.Serialize(nestedTuple, options));
        Assert.Equal(nestedTuple, JsonSerializer.Deserialize<(int, (string, bool))>("[1,[\"two\",true]]", options));
        Assert.Equal("null", JsonSerializer.Serialize<Tuple<int, string>?>(null, options));
        Assert.Null(JsonSerializer.Deserialize<Tuple<int, string>?>("null", options));
    }

    [Fact]
    public void JsonArrayTupleConverters_AreSafeForConcurrentReuse()
    {
        var options = CreateClosedTupleSerializerOptions();
        Assert.Equal("[1,\"two\"]", JsonSerializer.Serialize((1, "two"), options));

        Parallel.For(
            0,
            100,
            _ =>
            {
                Assert.Equal("[1,\"two\"]", JsonSerializer.Serialize((1, "two"), options));
                Assert.Equal((1, "two"), JsonSerializer.Deserialize<(int, string)>("[1,\"two\"]", options));
            });
    }

    [Fact]
    public void JsonArrayTupleConverters_PreserveSourceGeneratedMetadataAndElementConverters()
    {
        var options = new JsonSerializerOptions(TupleJsonSerializerContext.Default.Options);
        options.Converters.Add(JsonArrayTupleConverters.CreateValueTuple<int, string>());
        options.Converters.Insert(0, new UpperCaseStringConverter());

        Assert.Equal("[1,\"TWO\"]", JsonSerializer.Serialize((1, "two"), options));
        Assert.Equal((1, "TWO"), JsonSerializer.Deserialize<(int, string)>("[1,\"two\"]", options));
    }

    [Fact]
    public void JsonArrayTupleConverters_RejectInvalidRestConvertersAndMalformedArity()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => JsonArrayTupleConverters.CreateValueTuple<int, int, int, int, int, int, int, ValueTuple<int>>(
                new InvalidRestConverter()));
        Assert.Equal("restConverter", exception.ParamName);

        var options = new JsonSerializerOptions();
        options.Converters.Add(JsonArrayTupleConverters.CreateValueTuple<int, string>());
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<(int, string)>("[1]", options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<(int, string)>("[1,\"two\",true]", options));
    }

    [Theory]
    [InlineData(OpenApiSpecVersion.OpenApi3_0)]
    [InlineData(OpenApiSpecVersion.OpenApi3_2)]
    public async Task JsonArrayTupleConverters_ReuseTupleInferenceAndSchemaBehavior(OpenApiSpecVersion version)
    {
        var services = new ServiceCollection();
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(JsonArrayTupleConverters.CreateValueTuple<int, string>()));
        var builder = CreateBuilder(services);
        builder.MapGet("/", () => (1, "two"));

        var document = await VerifyOpenApiDocument(
            builder,
            new OpenApiOptions { OpenApiVersion = version },
            _ => { });
        var json = JsonNode.Parse(await document.SerializeAsJsonAsync(version))!;
        var component = Assert.Single(json["components"]!["schemas"]!.AsObject()).Value!;

        Assert.Equal("array", component["type"]!.GetValue<string>());
        Assert.Equal(2, component["minItems"]!.GetValue<int>());
        Assert.Equal(2, component["maxItems"]!.GetValue<int>());
        if (version == OpenApiSpecVersion.OpenApi3_0)
        {
            Assert.Empty(component["items"]!.AsObject());
            Assert.Null(component["prefixItems"]);
        }
        else
        {
            Assert.False(component["items"]!.GetValue<bool>());
            Assert.Equal(["integer", "string"], component["prefixItems"]!.AsArray().Select(item => item!["type"]!.GetValue<string>()));
        }
    }

    [Fact]
    public async Task JsonArrayTupleConverter_AbsentRetainsDefaultRuntimeAndSchemaBehavior()
    {
        Assert.Equal("{}", JsonSerializer.Serialize((1, "two")));

        var schema = new JsonSerializerOptions
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        }.GetJsonSchemaAsNode(typeof((int, string)));
        Assert.Equal("object", schema["type"]!.GetValue<string>());
        Assert.Null(schema["minItems"]);
        Assert.Null(schema["prefixItems"]);
    }

    [Theory]
    [InlineData(OpenApiSpecVersion.OpenApi3_1, OpenApiSchemaGenerationMode.Legacy)]
    [InlineData(OpenApiSpecVersion.OpenApi3_1, OpenApiSchemaGenerationMode.Inferred)]
    [InlineData(OpenApiSpecVersion.OpenApi3_2, OpenApiSchemaGenerationMode.Legacy)]
    [InlineData(OpenApiSpecVersion.OpenApi3_2, OpenApiSchemaGenerationMode.Inferred)]
    public async Task JsonArrayTupleConverter_EmitsExactPrefixItemsForOpenApi31And32(
        OpenApiSpecVersion version,
        OpenApiSchemaGenerationMode schemaGenerationMode)
    {
        var document = await CreateTupleDocumentAsync(version, schemaGenerationMode);
        var json = JsonNode.Parse(await document.SerializeAsJsonAsync(version))!;
        var schema = json["paths"]!["/"]!["get"]!["responses"]!["200"]!["content"]!["application/json"]!["schema"]!;
        if (schema["$ref"] is not null)
        {
            var componentName = schema["$ref"]!.GetValue<string>().Split('/')[^1];
            schema = json["components"]!["schemas"]![componentName]!;
        }

        Assert.Equal("array", schema["type"]!.GetValue<string>());
        Assert.Equal(2, schema["minItems"]!.GetValue<int>());
        Assert.Equal(2, schema["maxItems"]!.GetValue<int>());
        Assert.False(schema["items"]!.GetValue<bool>());
        Assert.Collection(
            schema["prefixItems"]!.AsArray(),
            item => Assert.Equal("integer", item!["type"]!.GetValue<string>()),
            item => Assert.Equal("string", item!["type"]!.GetValue<string>()));
    }

    [Fact]
    public void JsonArrayTupleConverter_ExposesTupleShapeFactsAndDynamicCodeAnnotations()
    {
        var serializerOptions = CreateTupleSerializerOptions();
        var document = InferredSchemaShapeBuilder.Build(serializerOptions, typeof((int?, string)));
        var tuple = document[typeof((int?, string))];
        var converterType = typeof(JsonArrayTupleConverter);

        Assert.Equal(InferredSchemaShapeKind.Tuple, tuple.Kind);
        Assert.Collection(
            tuple.TupleElements,
            element =>
            {
                Assert.Equal(typeof(int), element.Identity.Type);
                Assert.True(element.AllowsNull);
            },
            element =>
            {
                Assert.Equal(typeof(string), element.Identity.Type);
                Assert.False(element.AllowsNull);
            });
        Assert.NotNull(converterType.GetCustomAttributes(typeof(RequiresDynamicCodeAttribute), inherit: false).Single());
        Assert.NotNull(converterType.GetCustomAttributes(typeof(RequiresUnreferencedCodeAttribute), inherit: false).Single());
        var constructor = Assert.Single(converterType.GetConstructors());
        Assert.NotNull(constructor.GetCustomAttributes(typeof(RequiresDynamicCodeAttribute), inherit: false).Single());
        Assert.NotNull(constructor.GetCustomAttributes(typeof(RequiresUnreferencedCodeAttribute), inherit: false).Single());
    }

    [Fact]
    public void SchemaEvidenceProvider_PreservesTupleShapeAcrossReflectionAndSourceGeneratedMetadata()
    {
        var reflectionOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
        reflectionOptions.Converters.Add(JsonArrayTupleConverters.CreateValueTuple<int, string>());
        var generatedOptions = new JsonSerializerOptions(TupleJsonSerializerContext.Default.Options);
        generatedOptions.Converters.Add(JsonArrayTupleConverters.CreateValueTuple<int, string>());

        var reflection = InferredSchemaShapeBuilder.Build(reflectionOptions, typeof((int, string)))[typeof((int, string))];
        var generated = InferredSchemaShapeBuilder.Build(generatedOptions, typeof((int, string)))[typeof((int, string))];

        Assert.Equal(InferredSchemaShapeKind.Tuple, reflection.Kind);
        Assert.Equal(reflection.Kind, generated.Kind);
        Assert.Equal(
            reflection.TupleElements.Select(element => element.Identity.Type),
            generated.TupleElements.Select(element => element.Identity.Type));
    }

    [Fact]
    public void SchemaEvidenceResults_AreValidatedAndPositionalElementsAreCopied()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new OpenApiScalarSchemaEvidence((OpenApiScalarSchemaValueKind)(-1)));
        Assert.Throws<ArgumentException>(
            () => new OpenApiScalarSchemaEvidence(OpenApiScalarSchemaValueKind.Integer, pattern: "pattern"));
        Assert.Throws<ArgumentException>(
            () => new OpenApiScalarSchemaEvidence(OpenApiScalarSchemaValueKind.String, minimum: 0));
        Assert.Throws<ArgumentException>(
            () => new OpenApiScalarSchemaEvidence(OpenApiScalarSchemaValueKind.Integer, minimum: 2, maximum: 1));

        Type[] elements = [typeof(int), typeof(string)];
        var evidence = new OpenApiPositionalArraySchemaEvidence(elements);
        elements[0] = typeof(bool);

        Assert.Equal([typeof(int), typeof(string)], evidence.ElementTypes);
    }

    [Fact]
    public async Task JsonArrayTupleConverter_PreservesElementReferencesAndNullableValueSchemas()
    {
        var services = new ServiceCollection();
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonArrayTupleConverter()));
        var builder = CreateBuilder(services);
        builder.MapGet("/", () => (new TupleElement(), (int?)1));
        var document = await VerifyOpenApiDocument(
            builder,
            new OpenApiOptions { OpenApiVersion = OpenApiSpecVersion.OpenApi3_2 },
            _ => { });
        var json = JsonNode.Parse(await document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_2))!;
        var tupleComponent = json["components"]!["schemas"]!.AsObject()
            .Single(component => component.Value!["prefixItems"] is not null)
            .Value!;
        var prefixItems = tupleComponent["prefixItems"]!.AsArray();

        Assert.Equal("#/components/schemas/TupleElement", prefixItems[0]!["$ref"]!.GetValue<string>());
        Assert.Equal(["null", "integer"], prefixItems[1]!["type"]!.AsArray().Select(type => type!.GetValue<string>()));
        Assert.NotNull(json["components"]!["schemas"]!["TupleElement"]);
    }

    [Fact]
    public async Task JsonArrayTupleConverter_RecursiveAndNullableTuplePropertiesRemainFinite()
    {
        var services = new ServiceCollection();
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonArrayTupleConverter()));
        var builder = CreateBuilder(services);
        builder.MapGet("/", () => new TupleContainer());
        var document = await VerifyOpenApiDocument(
            builder,
            new OpenApiOptions { OpenApiVersion = OpenApiSpecVersion.OpenApi3_2 },
            _ => { });
        var json = JsonNode.Parse(await document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_2))!;
        var schemas = json["components"]!["schemas"]!.AsObject();
        var container = schemas[nameof(TupleContainer)]!;
        var recursiveTupleComponent = schemas.AsObject()
            .Single(component => component.Value!["prefixItems"] is JsonArray prefixItems &&
                prefixItems.Count == 2 &&
                prefixItems[0]?["$ref"]?.GetValue<string>() == "#/components/schemas/TupleContainer");
        var recursiveTuple = recursiveTupleComponent.Value!;

        Assert.Equal(2, recursiveTuple["minItems"]!.GetValue<int>());
        Assert.Equal(2, recursiveTuple["maxItems"]!.GetValue<int>());
        Assert.False(recursiveTuple["items"]!.GetValue<bool>());
        Assert.Equal(
            $"#/components/schemas/{recursiveTupleComponent.Key}",
            container["properties"]!["next"]!["$ref"]!.GetValue<string>());
        Assert.NotNull(container["properties"]!["optional"]!["oneOf"]);
    }

    [Fact]
    public async Task JsonArrayTupleConverter_EmitsConformingOpenApi30Approximation()
    {
        var document = await CreateTupleDocumentAsync(OpenApiSpecVersion.OpenApi3_0);
        var json = JsonNode.Parse(await document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_0))!;
        var schema = json["paths"]!["/"]!["get"]!["responses"]!["200"]!["content"]!["application/json"]!["schema"]!;
        if (schema["$ref"] is not null)
        {
            var componentName = schema["$ref"]!.GetValue<string>().Split('/')[^1];
            schema = json["components"]!["schemas"]![componentName]!;
        }

        Assert.Equal("array", schema["type"]!.GetValue<string>());
        Assert.Equal(2, schema["minItems"]!.GetValue<int>());
        Assert.Equal(2, schema["maxItems"]!.GetValue<int>());
        Assert.Empty(schema["items"]!.AsObject());
        Assert.Null(schema["prefixItems"]);
    }

    [Fact]
    public async Task JsonArrayTupleConverter_TransformerVisitsElementsInOrderAndPersistsMutations()
    {
        var visitedTypes = new List<Type>();
        var services = new ServiceCollection();
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonArrayTupleConverter()));
        var builder = CreateBuilder(services);
        builder.MapGet("/", () => (1, "two"));
        var options = new OpenApiOptions
        {
            OpenApiVersion = OpenApiSpecVersion.OpenApi3_2,
        };
        options.AddSchemaTransformer((schema, context, _) =>
        {
            Assert.Equal(OpenApiSpecVersion.OpenApi3_2, context.OpenApiVersion);
            if (context.JsonTypeInfo.Type == typeof(int) || context.JsonTypeInfo.Type == typeof(string))
            {
                visitedTypes.Add(context.JsonTypeInfo.Type);
                Assert.Null(context.JsonPropertyInfo);
                schema.Description = context.JsonTypeInfo.Type.Name;
            }

            return Task.CompletedTask;
        });

        var document = await VerifyOpenApiDocument(builder, options, _ => { });
        var json = JsonNode.Parse(await document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_2))!;
        var component = Assert.Single(json["components"]!["schemas"]!.AsObject()).Value!;
        var prefixItems = component["prefixItems"]!.AsArray();

        Assert.Equal([typeof(int), typeof(string)], visitedTypes);
        Assert.Equal("Int32", prefixItems[0]!["description"]!.GetValue<string>());
        Assert.Equal("String", prefixItems[1]!["description"]!.GetValue<string>());
    }

    [Theory]
    [InlineData(OpenApiSpecVersion.OpenApi3_0)]
    [InlineData(OpenApiSpecVersion.OpenApi3_1)]
    [InlineData(OpenApiSpecVersion.OpenApi3_2)]
    public async Task SchemaEvidenceProvider_EmitsConverterBackedScalarEvidence(OpenApiSpecVersion version)
    {
        var provider = new StrictIdSchemaEvidenceProvider();
        var services = new ServiceCollection();
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new StrictIdConverter()));
        var builder = CreateBuilder(services);
        builder.MapGet("/", () => new StrictId("ABC-123"));
        var options = new OpenApiOptions
        {
            OpenApiVersion = version,
            SchemaGenerationMode = OpenApiSchemaGenerationMode.Inferred,
        };
        options.AddSchemaEvidenceProvider(provider);

        var document = await VerifyOpenApiDocument(builder, options, _ => { });
        var schema = GetResponseSchema(document);

        Assert.Equal(JsonSchemaType.String, schema.Type);
        Assert.Equal("strict-id", schema.Format);
        Assert.Equal("^[A-Z]{3}-[0-9]{3}$", schema.Pattern);
        Assert.Contains(provider.Contexts, context =>
            context.Type == typeof(StrictId) &&
            context.EffectiveType == typeof(StrictId) &&
            context.TypeInfo.Type == typeof(StrictId) &&
            context.Converter is StrictIdConverter &&
            context.Purpose == OpenApiSchemaEvidencePurpose.Output);
    }

    [Theory]
    [InlineData(OpenApiScalarFormatPolicy.Conventional, "strict-id")]
    [InlineData(OpenApiScalarFormatPolicy.CompatibleOnly, null)]
    [InlineData(OpenApiScalarFormatPolicy.None, null)]
    public async Task SchemaEvidenceProvider_FormatObeysScalarFormatPolicy(
        OpenApiScalarFormatPolicy policy,
        string? expectedFormat)
    {
        var services = new ServiceCollection();
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new StrictIdConverter()));
        var builder = CreateBuilder(services);
        builder.MapGet("/", () => new StrictId("ABC-123"));
        var options = new OpenApiOptions
        {
            SchemaGenerationMode = OpenApiSchemaGenerationMode.Inferred,
            ScalarFormatPolicy = policy,
        };
        options.AddSchemaEvidenceProvider(new StrictIdSchemaEvidenceProvider());

        var document = await VerifyOpenApiDocument(builder, options, _ => { });
        var schema = GetResponseSchema(document);

        Assert.Equal(expectedFormat, schema.Format);
        Assert.Equal("^[A-Z]{3}-[0-9]{3}$", schema.Pattern);
    }

    [Fact]
    public async Task SchemaEvidenceProvider_FormatCallbackReceivesPolicyFilteredCandidate()
    {
        OpenApiScalarFormatContext? callbackContext = null;
        var services = new ServiceCollection();
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new StrictIdConverter()));
        var builder = CreateBuilder(services);
        builder.MapGet("/", () => new StrictId("ABC-123"));
        var options = new OpenApiOptions
        {
            SchemaGenerationMode = OpenApiSchemaGenerationMode.Inferred,
            ScalarFormatPolicy = OpenApiScalarFormatPolicy.CompatibleOnly,
            CreateScalarFormat = context =>
            {
                if (context.EffectiveType == typeof(StrictId))
                {
                    callbackContext = context;
                    return "application-strict-id";
                }

                return context.DefaultFormat;
            },
        };
        options.AddSchemaEvidenceProvider(new StrictIdSchemaEvidenceProvider());

        var document = await VerifyOpenApiDocument(builder, options, _ => { });
        var schema = GetResponseSchema(document);

        Assert.NotNull(callbackContext);
        Assert.Null(callbackContext.DefaultFormat);
        Assert.Equal("application-strict-id", schema.Format);
        Assert.Equal("^[A-Z]{3}-[0-9]{3}$", schema.Pattern);
    }

    [Fact]
    public async Task SchemaEvidenceProvider_WithoutEnforcingConverterIsInert()
    {
        var provider = new StrictIdSchemaEvidenceProvider();
        var builder = CreateBuilder();
        builder.MapGet("/", () => new StrictId("ABC-123"));
        var options = new OpenApiOptions
        {
            SchemaGenerationMode = OpenApiSchemaGenerationMode.Inferred,
        };
        options.AddSchemaEvidenceProvider(provider);

        var document = await VerifyOpenApiDocument(builder, options, _ => { });
        var schema = GetResponseSchema(document);

        Assert.Equal(JsonSchemaType.Object, schema.Type);
        Assert.Null(schema.Pattern);
    }

    [Fact]
    public async Task SchemaEvidenceProviders_RunInRegistrationOrderAndRejectMultipleClaims()
    {
        var calls = new List<string>();
        var services = new ServiceCollection();
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new StrictIdConverter()));
        var builder = CreateBuilder(services);
        builder.MapGet("/", () => new StrictId("ABC-123"));
        var options = new OpenApiOptions
        {
            SchemaGenerationMode = OpenApiSchemaGenerationMode.Inferred,
        };
        options.AddSchemaEvidenceProvider(new RecordingSchemaEvidenceProvider("first", calls));
        options.AddSchemaEvidenceProvider(new RecordingSchemaEvidenceProvider("second", calls));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => VerifyOpenApiDocument(builder, options, _ => { }));

        Assert.Equal(["first", "second"], calls.Take(2));
        Assert.Contains(typeof(RecordingSchemaEvidenceProvider).FullName!, exception.Message);
    }

    [Fact]
    public void SchemaEvidenceProvider_SameInstanceIsEvaluatedOnce()
    {
        var serializerOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
        serializerOptions.Converters.Add(new StrictIdConverter());
        var typeInfo = serializerOptions.GetTypeInfo(typeof(StrictId));
        var provider = new StrictIdSchemaEvidenceProvider();

        var evidence = OpenApiSchemaEvidenceResolver.Resolve(
            typeInfo,
            typeInfo.Converter,
            InferredSchemaPurpose.Output,
            [provider, provider]);

        Assert.IsType<OpenApiScalarSchemaEvidence>(evidence);
        Assert.Single(provider.Contexts);
    }

    [Fact]
    public async Task SchemaEvidenceProviderExceptionsPropagate()
    {
        var builder = CreateBuilder();
        builder.MapGet("/", () => new StrictId("ABC-123"));
        var options = new OpenApiOptions
        {
            SchemaGenerationMode = OpenApiSchemaGenerationMode.Inferred,
        };
        options.AddSchemaEvidenceProvider(new ThrowingSchemaEvidenceProvider());

        var exception = await Assert.ThrowsAsync<SchemaEvidenceProviderException>(
            () => VerifyOpenApiDocument(builder, options, _ => { }));

        Assert.Equal("Provider failure.", exception.Message);
    }

    private static async Task<OpenApiDocument> CreateTupleDocumentAsync(
        OpenApiSpecVersion version,
        OpenApiSchemaGenerationMode schemaGenerationMode = OpenApiSchemaGenerationMode.Legacy)
    {
        var services = new ServiceCollection();
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonArrayTupleConverter()));
        var builder = CreateBuilder(services);
        builder.MapGet("/", () => (1, "two"));
        return await VerifyOpenApiDocument(
            builder,
            new OpenApiOptions
            {
                OpenApiVersion = version,
                SchemaGenerationMode = schemaGenerationMode,
            },
            _ => { });
    }

    private static OpenApiSchema GetResponseSchema(OpenApiDocument document)
    {
        var operation = document.Paths!["/"]!.Operations![HttpMethod.Get]!;
        var response = operation.Responses!["200"]!;
        var schema = response.Content!["application/json"]!.Schema!;
        return schema switch
        {
            OpenApiSchema concrete => concrete,
            OpenApiSchemaReference reference => Assert.IsType<OpenApiSchema>(reference.Target),
            _ => throw new InvalidOperationException($"Unexpected schema type '{schema.GetType()}'."),
        };
    }

    private static JsonSerializerOptions CreateTupleSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
        options.Converters.Add(new JsonArrayTupleConverter());
        return options;
    }

    private static JsonSerializerOptions CreateClosedTupleSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
        options.Converters.Add(JsonArrayTupleConverters.CreateValueTuple());
        options.Converters.Add(JsonArrayTupleConverters.CreateValueTuple<int, string>());
        options.Converters.Add(JsonArrayTupleConverters.CreateTuple<int, string>());
        options.Converters.Add(JsonArrayTupleConverters.CreateTuple<int, int>());
        options.Converters.Add(JsonArrayTupleConverters.CreateTuple<int, int, int, int, int, int, int, Tuple<int, int>>(
            JsonArrayTupleConverters.CreateTuple<int, int>()));
        options.Converters.Add(JsonArrayTupleConverters.CreateValueTuple<int, int>());
        options.Converters.Add(JsonArrayTupleConverters.CreateValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(
            JsonArrayTupleConverters.CreateValueTuple<int, int>()));
        options.Converters.Add(JsonArrayTupleConverters.CreateValueTuple<string, bool>());
        options.Converters.Add(JsonArrayTupleConverters.CreateValueTuple<int, ValueTuple<string, bool>>());
        return options;
    }

    private sealed class UpperCaseStringConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.GetString()?.ToUpperInvariant();

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToUpperInvariant());
    }

    private sealed class InvalidRestConverter : JsonConverter<ValueTuple<int>>
    {
        public override ValueTuple<int> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new(reader.GetInt32());

        public override void Write(Utf8JsonWriter writer, ValueTuple<int> value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value.Item1);
    }

    private sealed class TupleElement
    {
        public int Value { get; set; }
    }

    private sealed class TupleContainer
    {
        public (TupleContainer?, int) Next { get; set; }

        public Tuple<int, string>? Optional { get; set; }
    }

    private readonly record struct StrictId(string Value);

    private sealed class StrictIdConverter : JsonConverter<StrictId>
    {
        public override StrictId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new(reader.GetString()!);

        public override void Write(Utf8JsonWriter writer, StrictId value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.Value);
    }

    private sealed class StrictIdSchemaEvidenceProvider : IOpenApiSchemaEvidenceProvider
    {
        public List<OpenApiSchemaEvidenceContext> Contexts { get; } = [];

        public OpenApiSchemaEvidence? GetSchemaEvidence(OpenApiSchemaEvidenceContext context)
        {
            Contexts.Add(context);
            return context.EffectiveType == typeof(StrictId) && context.Converter is StrictIdConverter
                ? new OpenApiScalarSchemaEvidence(
                    OpenApiScalarSchemaValueKind.String,
                    "strict-id",
                    "^[A-Z]{3}-[0-9]{3}$")
                : null;
        }
    }

    private sealed class RecordingSchemaEvidenceProvider(string name, List<string> calls) : IOpenApiSchemaEvidenceProvider
    {
        public OpenApiSchemaEvidence? GetSchemaEvidence(OpenApiSchemaEvidenceContext context)
        {
            if (context.EffectiveType != typeof(StrictId))
            {
                return null;
            }

            calls.Add(name);
            return new OpenApiScalarSchemaEvidence(OpenApiScalarSchemaValueKind.String);
        }
    }

    private sealed class ThrowingSchemaEvidenceProvider : IOpenApiSchemaEvidenceProvider
    {
        public OpenApiSchemaEvidence? GetSchemaEvidence(OpenApiSchemaEvidenceContext context)
            => context.EffectiveType == typeof(StrictId)
                ? throw new SchemaEvidenceProviderException("Provider failure.")
                : null;
    }

    private sealed class SchemaEvidenceProviderException(string message) : Exception(message);

    [JsonSerializable(typeof((int, string)))]
    private sealed partial class TupleJsonSerializerContext : JsonSerializerContext;
}

#pragma warning restore ASP0040
