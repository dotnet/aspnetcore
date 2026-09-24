// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

#nullable enable
#pragma warning disable ASP0040

public partial class OpenApiSchemaServiceTests
{
    private const string IntegerPattern = "^-?(?:0|[1-9]\\d*)$";
    private const string NumberPattern = "^-?(?:0|[1-9]\\d*)(?:\\.\\d+)?(?:[eE][+-]?\\d+)?$";
    private const string DecimalPattern = "^-?(?:0|[1-9]\\d*)(?:\\.\\d+)?$";
    private const string TimeSpanPattern = "^-?(\\d+\\.)?\\d{2}:\\d{2}:\\d{2}(\\.\\d{1,7})?$";
    private const string VersionPattern = "^\\d+(\\.\\d+){1,3}$";

    public static TheoryData<Type, JsonTypeInfoKind, string?, string?> WellKnownScalarExporterContracts => new()
    {
        { typeof(DateTime), JsonTypeInfoKind.None, "date-time", null },
        { typeof(DateTimeOffset), JsonTypeInfoKind.None, "date-time", null },
        { typeof(DateOnly), JsonTypeInfoKind.None, "date", null },
        { typeof(TimeOnly), JsonTypeInfoKind.None, "time", null },
        { typeof(TimeSpan), JsonTypeInfoKind.None, null, TimeSpanPattern },
        { typeof(Guid), JsonTypeInfoKind.None, "uuid", null },
        { typeof(Uri), JsonTypeInfoKind.None, "uri", null },
        { typeof(Version), JsonTypeInfoKind.None, null, VersionPattern },
        { typeof(char), JsonTypeInfoKind.None, null, null },
        { typeof(Rune), JsonTypeInfoKind.Object, null, null },
        { typeof(byte[]), JsonTypeInfoKind.None, null, null },
        { typeof(Memory<byte>), JsonTypeInfoKind.None, null, null },
        { typeof(ReadOnlyMemory<byte>), JsonTypeInfoKind.None, null, null },
        { typeof(IPAddress), JsonTypeInfoKind.Object, null, null },
        { typeof(IPEndPoint), JsonTypeInfoKind.Object, null, null },
        { typeof(BigInteger), JsonTypeInfoKind.Object, null, null },
        { typeof(nint), JsonTypeInfoKind.None, null, null },
        { typeof(nuint), JsonTypeInfoKind.None, null, null },
    };

    [Theory]
    [MemberData(nameof(WellKnownScalarExporterContracts))]
    public void JsonSchemaExporter_WellKnownScalarContractsMatchEffectiveMetadata(
        Type type,
        JsonTypeInfoKind expectedKind,
        string? expectedFormat,
        string? expectedPattern)
    {
        var options = CreateScalarSerializerOptions(JsonNumberHandling.Strict);
        var typeInfo = options.GetTypeInfo(type);
        var schema = JsonSchemaExporter.GetJsonSchemaAsNode(typeInfo);

        Assert.Equal(expectedKind, typeInfo.Kind);
        Assert.Equal(expectedFormat, schema["format"]?.GetValue<string>());
        Assert.Equal(expectedPattern, schema["pattern"]?.GetValue<string>());

        if (type == typeof(char))
        {
            Assert.Equal("string", schema["type"]!.GetValue<string>());
            Assert.Equal(1, schema["minLength"]!.GetValue<int>());
            Assert.Equal(1, schema["maxLength"]!.GetValue<int>());
        }
        else if (type == typeof(byte[]) ||
            type == typeof(Memory<byte>) ||
            type == typeof(ReadOnlyMemory<byte>))
        {
            Assert.Equal("string", GetScalarType(schema));
            Assert.Equal("base64", schema["contentEncoding"]!.GetValue<string>());
        }
        else if (type == typeof(nint) || type == typeof(nuint))
        {
            Assert.Equal("Unsupported .NET type", schema["$comment"]!.GetValue<string>());
            Assert.True(schema["not"]!.GetValue<bool>());
        }
        else if (expectedKind == JsonTypeInfoKind.Object)
        {
            Assert.Equal("object", GetScalarType(schema));
            var properties = schema["properties"]!.AsObject();
            Assert.NotEmpty(properties);

            if (type == typeof(Rune))
            {
                Assert.Equal(
                    ["isAscii", "isBmp", "plane", "utf16SequenceLength", "utf8SequenceLength", "value"],
                    properties.Select(property => property.Key).Order(StringComparer.Ordinal));
            }
            else if (type == typeof(BigInteger))
            {
                Assert.Equal(
                    ["isEven", "isOne", "isPowerOfTwo", "isZero", "sign"],
                    properties.Select(property => property.Key).Order(StringComparer.Ordinal));
            }
        }
        else
        {
            Assert.Equal("string", GetScalarType(schema));
        }
    }

    public static TheoryData<JsonNumberHandling> NumericNumberHandling =>
    [
        JsonNumberHandling.Strict,
        JsonNumberHandling.AllowReadingFromString,
        JsonNumberHandling.WriteAsString,
        JsonNumberHandling.AllowNamedFloatingPointLiterals,
    ];

    [Theory]
    [MemberData(nameof(NumericNumberHandling))]
    public void JsonSchemaExporter_NumericContractsMatchEffectiveNumberHandling(JsonNumberHandling numberHandling)
    {
        var options = CreateScalarSerializerOptions(numberHandling);
        Type[] integerTypes =
        [
            typeof(sbyte), typeof(byte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(Int128), typeof(UInt128),
        ];
        Type[] floatingTypes = [typeof(Half), typeof(float), typeof(double)];

        foreach (var type in integerTypes)
        {
            var schema = JsonSchemaExporter.GetJsonSchemaAsNode(options.GetTypeInfo(type));
            AssertNumericSchemaHasNoDomainBounds(schema);
            Assert.Null(schema["format"]);

            if (numberHandling is JsonNumberHandling.AllowReadingFromString or JsonNumberHandling.WriteAsString)
            {
                Assert.Equal(["integer", "string"], GetSchemaTypes(schema).Order(StringComparer.Ordinal));
                Assert.Equal(IntegerPattern, schema["pattern"]!.GetValue<string>());
            }
            else
            {
                Assert.Equal(["integer"], GetSchemaTypes(schema));
                Assert.Null(schema["pattern"]);
            }
        }

        foreach (var type in floatingTypes)
        {
            var schema = JsonSchemaExporter.GetJsonSchemaAsNode(options.GetTypeInfo(type));
            AssertNumericSchemaHasNoDomainBounds(schema);
            Assert.Null(schema["format"]);

            if (numberHandling == JsonNumberHandling.AllowNamedFloatingPointLiterals)
            {
                var anyOf = schema["anyOf"]!.AsArray();
                Assert.Equal("number", anyOf[0]!["type"]!.GetValue<string>());
                Assert.Equal(
                    ["NaN", "Infinity", "-Infinity"],
                    anyOf[1]!["enum"]!.AsArray().Select(value => value!.GetValue<string>()));
            }
            else if (numberHandling is JsonNumberHandling.AllowReadingFromString or JsonNumberHandling.WriteAsString)
            {
                Assert.Equal(["number", "string"], GetSchemaTypes(schema).Order(StringComparer.Ordinal));
                Assert.Equal(NumberPattern, schema["pattern"]!.GetValue<string>());
            }
            else
            {
                Assert.Equal(["number"], GetSchemaTypes(schema));
                Assert.Null(schema["pattern"]);
            }
        }

        var decimalSchema = JsonSchemaExporter.GetJsonSchemaAsNode(options.GetTypeInfo(typeof(decimal)));
        AssertNumericSchemaHasNoDomainBounds(decimalSchema);
        Assert.Null(decimalSchema["format"]);
        if (numberHandling is JsonNumberHandling.AllowReadingFromString or JsonNumberHandling.WriteAsString)
        {
            Assert.Equal(["number", "string"], GetSchemaTypes(decimalSchema).Order(StringComparer.Ordinal));
            Assert.Equal(DecimalPattern, decimalSchema["pattern"]!.GetValue<string>());
        }
        else
        {
            Assert.Equal(["number"], GetSchemaTypes(decimalSchema));
            Assert.Null(decimalSchema["pattern"]);
        }
    }

    [Fact]
    public void ScalarRuntimeBehavior_ExposesStandardFormatCompatibilityBoundaries()
    {
        var options = CreateScalarSerializerOptions(JsonNumberHandling.Strict);

        var unspecified = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Unspecified);
        Assert.Equal("\"2024-01-02T03:04:05\"", JsonSerializer.Serialize(unspecified, options));
        Assert.Equal(
            DateTimeKind.Unspecified,
            JsonSerializer.Deserialize<DateTime>("\"2024-01-02T03:04:05\"", options).Kind);
        Assert.Equal(
            TimeSpan.Zero,
            JsonSerializer.Deserialize<DateTimeOffset>("\"2024-01-02T03:04:05\"", options).Offset);

        Assert.Equal("\"2024-01-02\"", JsonSerializer.Serialize(new DateOnly(2024, 1, 2), options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateOnly>("\"2024-1-2\"", options));

        Assert.Equal("\"03:04:05.6780000\"", JsonSerializer.Serialize(new TimeOnly(3, 4, 5, 678), options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TimeOnly>("\"03:04:05Z\"", options));

        Assert.Equal(
            TimeSpan.FromDays(1) + new TimeSpan(2, 3, 4) + TimeSpan.FromMilliseconds(500),
            JsonSerializer.Deserialize<TimeSpan>("\"1.02:03:04.5000000\"", options));
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<TimeSpan>("\"P1DT2H3M4.5S\"", options));

        var guid = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
        Assert.Equal(guid, JsonSerializer.Deserialize<Guid>("\"00112233-4455-6677-8899-aabbccddeeff\"", options));
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<Guid>("\"00112233445566778899aabbccddeeff\"", options));

        var relativeUri = JsonSerializer.Deserialize<Uri>("\"relative/path?x=1\"", options);
        Assert.False(relativeUri!.IsAbsoluteUri);
        Assert.Equal("\"relative/path?x=1\"", JsonSerializer.Serialize(relativeUri, options));

        Assert.Equal(new Version(1, 2, 3, 4), JsonSerializer.Deserialize<Version>("\"1.2.3.4\"", options));
        Assert.Equal('é', JsonSerializer.Deserialize<char>("\"é\"", options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<char>("\"😀\"", options));

        var bytes = new byte[] { 0, 1, 2, 255 };
        Assert.Equal("\"AAEC/w==\"", JsonSerializer.Serialize(bytes, options));
        Assert.Equal(bytes, JsonSerializer.Deserialize<byte[]>("\"AAEC/w==\"", options));
        Assert.Equal("\"AAEC/w==\"", JsonSerializer.Serialize(bytes.AsMemory(), options));
        Assert.Equal("\"AAEC/w==\"", JsonSerializer.Serialize((ReadOnlyMemory<byte>)bytes, options));
    }

    [Fact]
    public void ScalarRuntimeBehavior_PreservesUnsupportedAndObjectContracts()
    {
        var options = CreateScalarSerializerOptions(JsonNumberHandling.Strict);

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Rune>("\"😀\"", options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IPAddress>("\"2001:db8::1\"", options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IPEndPoint>("\"127.0.0.1:8080\"", options));
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<BigInteger>("123456789012345678901234567890", options));
        Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize((nint)1, options));
        Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize((nuint)1, options));

        var runeJson = JsonNode.Parse(JsonSerializer.Serialize(new Rune(0x1F600), options))!.AsObject();
        Assert.Equal(128512, runeJson["value"]!.GetValue<int>());

        var integerJson = JsonNode.Parse(JsonSerializer.Serialize(BigInteger.Parse(
            "123456789012345678901234567890",
            CultureInfo.InvariantCulture), options))!.AsObject();
        Assert.Equal(1, integerJson["sign"]!.GetValue<int>());
    }

    [Fact]
    public void ScalarFacts_ReflectionAndSourceGenerationAreSemanticallyEquivalent()
    {
        var reflection = CreateScalarSerializerOptions(JsonNumberHandling.AllowReadingFromString);
        var generated = new JsonSerializerOptions(ScalarJsonContext.Default.Options);

        foreach (var type in ScalarContractTypes)
        {
            var reflectionTypeInfo = reflection.GetTypeInfo(type);
            var generatedTypeInfo = generated.GetTypeInfo(type);

            Assert.Equal(reflectionTypeInfo.Kind, generatedTypeInfo.Kind);
            Assert.Equal(
                InferredScalarContractFactBuilder.Build(reflectionTypeInfo),
                InferredScalarContractFactBuilder.Build(generatedTypeInfo));
            Assert.True(JsonNode.DeepEquals(
                JsonSchemaExporter.GetJsonSchemaAsNode(reflectionTypeInfo),
                JsonSchemaExporter.GetJsonSchemaAsNode(generatedTypeInfo)));
        }
    }

    [Theory]
    [InlineData(typeof(sbyte), "-128", "127")]
    [InlineData(typeof(byte), "0", "255")]
    [InlineData(typeof(short), "-32768", "32767")]
    [InlineData(typeof(ushort), "0", "65535")]
    [InlineData(typeof(int), "-2147483648", "2147483647")]
    [InlineData(typeof(uint), "0", "4294967295")]
    [InlineData(typeof(long), "-9223372036854775808", "9223372036854775807")]
    [InlineData(typeof(ulong), "0", "18446744073709551615")]
    [InlineData(typeof(Int128), "-170141183460469231731687303715884105728", "170141183460469231731687303715884105727")]
    [InlineData(typeof(UInt128), "0", "340282366920938463463374607431768211455")]
    public void InferredScalarDecision_UsesExactBuiltInIntegralBounds(
        Type type,
        string expectedMinimum,
        string expectedMaximum)
    {
        var options = CreateScalarSerializerOptions(JsonNumberHandling.AllowReadingFromString);
        var fact = InferredScalarContractFactBuilder.Build(options.GetTypeInfo(type));
        var decision = InferredScalarSchemaDecisionBuilder.Build(fact);

        Assert.Equal(InferredScalarContractProvenance.SystemTextJsonBuiltIn, fact.Provenance);
        Assert.Equal(InferredScalarContractKind.Integral, fact.Kind);
        Assert.Equal(expectedMinimum, decision.NumericBounds?.Minimum);
        Assert.Equal(expectedMaximum, decision.NumericBounds?.Maximum);
    }

    [Theory]
    [InlineData(typeof(byte[]))]
    [InlineData(typeof(Memory<byte>))]
    [InlineData(typeof(ReadOnlyMemory<byte>))]
    public void InferredScalarDecision_UsesBuiltInBase64Provenance(Type type)
    {
        var options = CreateScalarSerializerOptions(JsonNumberHandling.Strict);
        var fact = InferredScalarContractFactBuilder.Build(options.GetTypeInfo(type));
        var decision = InferredScalarSchemaDecisionBuilder.Build(fact);

        Assert.Equal(InferredScalarContractProvenance.SystemTextJsonBuiltIn, fact.Provenance);
        Assert.Equal(InferredScalarContractKind.Base64String, fact.Kind);
        Assert.Equal("base64", decision.ContentEncoding);
    }

    [Fact]
    public void JsonSchemaExporter_CustomScalarConverterRemainsUnconstrained()
    {
        var options = CreateScalarSerializerOptions(JsonNumberHandling.Strict);
        options.Converters.Add(new EpochDateTimeConverter());
        var typeInfo = options.GetTypeInfo(typeof(DateTime));
        var schema = JsonSchemaExporter.GetJsonSchemaAsNode(typeInfo);
        var fact = InferredScalarContractFactBuilder.Build(typeInfo);
        var decision = InferredScalarSchemaDecisionBuilder.Build(fact);

        Assert.IsType<EpochDateTimeConverter>(typeInfo.Converter);
        Assert.Equal(JsonTypeInfoKind.None, typeInfo.Kind);
        Assert.Equal(JsonValueKind.True, schema.GetValueKind());
        Assert.Equal("0", JsonSerializer.Serialize(DateTime.UnixEpoch, options));
        Assert.Equal(InferredScalarContractProvenance.CustomConverter, fact.Provenance);
        Assert.Equal(InferredScalarContractKind.Other, fact.Kind);
        Assert.Null(decision.Format);
        Assert.Null(decision.NumericBounds);
        Assert.Null(decision.ContentEncoding);
    }

    [Theory]
    [InlineData(OpenApiSchemaGenerationMode.Legacy, OpenApiSpecVersion.OpenApi3_0)]
    [InlineData(OpenApiSchemaGenerationMode.Legacy, OpenApiSpecVersion.OpenApi3_1)]
    [InlineData(OpenApiSchemaGenerationMode.Legacy, OpenApiSpecVersion.OpenApi3_2)]
    [InlineData(OpenApiSchemaGenerationMode.Inferred, OpenApiSpecVersion.OpenApi3_0)]
    [InlineData(OpenApiSchemaGenerationMode.Inferred, OpenApiSpecVersion.OpenApi3_1)]
    [InlineData(OpenApiSchemaGenerationMode.Inferred, OpenApiSpecVersion.OpenApi3_2)]
    public async Task OpenApiScalarContracts_AreStableAcrossModesAndVersions(
        OpenApiSchemaGenerationMode mode,
        OpenApiSpecVersion version)
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (ScalarContractContainer value) => value);
        var options = new OpenApiOptions
        {
            SchemaGenerationMode = mode,
            OpenApiVersion = version,
        };

        var document = await VerifyOpenApiDocument(builder, options, document =>
        {
            var containerName = mode == OpenApiSchemaGenerationMode.Inferred
                ? $"{nameof(ScalarContractContainer)}.Input"
                : nameof(ScalarContractContainer);
            var component = document.Components!.Schemas![containerName];
            var properties = Assert.IsAssignableFrom<IDictionary<string, IOpenApiSchema>>(component.Properties);

            AssertScalar(properties["dateTime"], JsonSchemaType.String, "date-time");
            AssertScalar(properties["dateTimeOffset"], JsonSchemaType.String, "date-time");
            AssertScalar(properties["dateOnly"], JsonSchemaType.String, "date");
            AssertScalar(properties["timeOnly"], JsonSchemaType.String, "time");
            AssertScalar(properties["timeSpan"], JsonSchemaType.String, pattern: TimeSpanPattern);
            AssertScalar(properties["guid"], JsonSchemaType.String, "uuid");
            AssertScalar(
                properties["uri"],
                JsonSchemaType.String | JsonSchemaType.Null,
                mode == OpenApiSchemaGenerationMode.Legacy ? "uri" : "uri-reference");
            AssertScalar(properties["version"], JsonSchemaType.String | JsonSchemaType.Null, pattern: VersionPattern);

            var character = Assert.IsType<OpenApiSchema>(properties["character"]);
            AssertScalar(character, JsonSchemaType.String, "char");
            Assert.Equal(1, character.MinLength);
            Assert.Equal(1, character.MaxLength);

            Assert.Equal(JsonSchemaType.Object, ResolveSchema(document, properties["rune"]).Type);
            var bytes = Assert.IsType<OpenApiSchema>(properties["bytes"]);
            var memory = Assert.IsType<OpenApiSchema>(ResolveSchema(document, properties["memory"]));
            var readOnlyMemory = Assert.IsType<OpenApiSchema>(ResolveSchema(document, properties["readOnlyMemory"]));
            if (mode == OpenApiSchemaGenerationMode.Inferred)
            {
                var expectedFormat = version == OpenApiSpecVersion.OpenApi3_0 ? "byte" : null;
                var expectedContentEncoding = version == OpenApiSpecVersion.OpenApi3_0 ? null : "base64";
                AssertScalar(bytes, JsonSchemaType.String | JsonSchemaType.Null, expectedFormat);
                Assert.Equal(expectedContentEncoding, bytes.ContentEncoding);
                AssertScalar(memory, JsonSchemaType.String, expectedFormat);
                Assert.Equal(expectedContentEncoding, memory.ContentEncoding);
                AssertScalar(readOnlyMemory, JsonSchemaType.String, expectedFormat);
                Assert.Equal(expectedContentEncoding, readOnlyMemory.ContentEncoding);
            }
            else
            {
                AssertScalar(bytes, JsonSchemaType.String | JsonSchemaType.Null, "byte");
                AssertScalar(memory, JsonSchemaType.String);
                AssertScalar(readOnlyMemory, JsonSchemaType.String);
            }
            Assert.Equal(JsonSchemaType.Object, ResolveSchema(document, properties["bigInteger"]).Type);
            AssertUnconstrained(properties["nativeInt"]);
            AssertUnconstrained(properties["nativeUInt"]);

            foreach (var (name, expectedFormat) in NumericOpenApiFormats)
            {
                var schema = Assert.IsType<OpenApiSchema>(properties[name]);
                Assert.Equal(
                    mode == OpenApiSchemaGenerationMode.Inferred && name == "decimal" ? null : expectedFormat,
                    schema.Format);
                if (mode == OpenApiSchemaGenerationMode.Inferred &&
                    NumericOpenApiBounds.TryGetValue(name, out var expectedBounds))
                {
                    Assert.Equal(expectedBounds.Minimum, schema.Minimum);
                    Assert.Equal(expectedBounds.Maximum, schema.Maximum);
                }
                else
                {
                    Assert.Null(schema.Minimum);
                    Assert.Null(schema.Maximum);
                }
                Assert.Null(schema.MultipleOf);
            }

            var custom = Assert.IsType<OpenApiSchema>(properties["customDateTime"]);
            Assert.Null(custom.Type);
            Assert.Equal(
                mode == OpenApiSchemaGenerationMode.Legacy ? "date-time" : null,
                custom.Format);
            var customInt32 = Assert.IsType<OpenApiSchema>(properties["customInt32"]);
            Assert.Null(customInt32.Type);
            Assert.Equal(
                mode == OpenApiSchemaGenerationMode.Legacy ? "int32" : null,
                customInt32.Format);
            Assert.Null(customInt32.Minimum);
            Assert.Null(customInt32.Maximum);
            var customBytes = Assert.IsType<OpenApiSchema>(properties["customBytes"]);
            Assert.Null(customBytes.Type);
            Assert.Equal(
                mode == OpenApiSchemaGenerationMode.Legacy ? "byte" : null,
                customBytes.Format);
            Assert.Null(customBytes.ContentEncoding);
        });

        var json = JsonNode.Parse(await document.SerializeAsJsonAsync(version))!;
        var schemas = json["components"]!["schemas"]!.AsObject();
        var containerName = mode == OpenApiSchemaGenerationMode.Inferred
            ? $"{nameof(ScalarContractContainer)}.Input"
            : nameof(ScalarContractContainer);
        var serializedProperties = schemas[containerName]!["properties"]!.AsObject();
        foreach (var (name, expectedBounds) in NumericOpenApiBounds)
        {
            if (mode == OpenApiSchemaGenerationMode.Inferred)
            {
                Assert.Equal(expectedBounds.Minimum, serializedProperties[name]!["minimum"]!.ToJsonString());
                Assert.Equal(expectedBounds.Maximum, serializedProperties[name]!["maximum"]!.ToJsonString());
            }
            else
            {
                Assert.Null(serializedProperties[name]!["minimum"]);
                Assert.Null(serializedProperties[name]!["maximum"]);
            }
        }
        if (mode == OpenApiSchemaGenerationMode.Inferred && version != OpenApiSpecVersion.OpenApi3_0)
        {
            Assert.Null(serializedProperties["bytes"]!["format"]);
            Assert.Equal("base64", serializedProperties["bytes"]!["contentEncoding"]!.GetValue<string>());
            Assert.Equal("base64", schemas["MemoryOfbyte"]!["contentEncoding"]!.GetValue<string>());
            Assert.Equal("base64", schemas["ReadOnlyMemoryOfbyte"]!["contentEncoding"]!.GetValue<string>());
        }
        else
        {
            Assert.Equal("byte", serializedProperties["bytes"]!["format"]!.GetValue<string>());
            Assert.Null(serializedProperties["bytes"]!["contentEncoding"]);
            if (mode == OpenApiSchemaGenerationMode.Inferred)
            {
                Assert.Equal("byte", schemas["MemoryOfbyte"]!["format"]!.GetValue<string>());
                Assert.Equal("byte", schemas["ReadOnlyMemoryOfbyte"]!["format"]!.GetValue<string>());
            }
            else
            {
                Assert.Null(schemas["MemoryOfbyte"]!["format"]);
                Assert.Null(schemas["ReadOnlyMemoryOfbyte"]!["format"]);
            }
            Assert.Null(schemas["MemoryOfbyte"]!["contentEncoding"]);
            Assert.Null(schemas["ReadOnlyMemoryOfbyte"]!["contentEncoding"]);
        }
        Assert.Equal("object", ResolveSerializedSchema(json, serializedProperties["ipAddress"]!)["type"]!.GetValue<string>());
        Assert.Equal("object", ResolveSerializedSchema(json, serializedProperties["ipEndPoint"]!)["type"]!.GetValue<string>());
        Assert.Empty(serializedProperties["nativeInt"]!.AsObject());
        Assert.Empty(serializedProperties["nativeUInt"]!.AsObject());
        Assert.Equal(
            mode == OpenApiSchemaGenerationMode.Legacy ? "date-time" : null,
            serializedProperties["customDateTime"]!["format"]?.GetValue<string>());
        Assert.Null(serializedProperties["customDateTime"]!["type"]);
        Assert.Null(serializedProperties["customInt32"]!["minimum"]);
        Assert.Null(serializedProperties["customInt32"]!["maximum"]);
        Assert.Equal(
            mode == OpenApiSchemaGenerationMode.Legacy ? "int32" : null,
            serializedProperties["customInt32"]!["format"]?.GetValue<string>());
        Assert.Equal(
            mode == OpenApiSchemaGenerationMode.Legacy ? "byte" : null,
            serializedProperties["customBytes"]!["format"]?.GetValue<string>());
        Assert.Null(serializedProperties["customBytes"]!["contentEncoding"]);
        Assert.DoesNotContain("x-jsonSchema-", json.ToJsonString());
    }

    [Theory]
    [InlineData(OpenApiSpecVersion.OpenApi3_0, OpenApiScalarFormatPolicy.Conventional)]
    [InlineData(OpenApiSpecVersion.OpenApi3_0, OpenApiScalarFormatPolicy.CompatibleOnly)]
    [InlineData(OpenApiSpecVersion.OpenApi3_0, OpenApiScalarFormatPolicy.None)]
    [InlineData(OpenApiSpecVersion.OpenApi3_1, OpenApiScalarFormatPolicy.Conventional)]
    [InlineData(OpenApiSpecVersion.OpenApi3_1, OpenApiScalarFormatPolicy.CompatibleOnly)]
    [InlineData(OpenApiSpecVersion.OpenApi3_1, OpenApiScalarFormatPolicy.None)]
    [InlineData(OpenApiSpecVersion.OpenApi3_2, OpenApiScalarFormatPolicy.Conventional)]
    [InlineData(OpenApiSpecVersion.OpenApi3_2, OpenApiScalarFormatPolicy.CompatibleOnly)]
    [InlineData(OpenApiSpecVersion.OpenApi3_2, OpenApiScalarFormatPolicy.None)]
    public async Task OpenApiScalarFormatPolicy_AppliesToInferredSchemasAndPreservesBase64(
        OpenApiSpecVersion version,
        OpenApiScalarFormatPolicy policy)
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (ScalarContractContainer value) => value);
        var options = new OpenApiOptions
        {
            SchemaGenerationMode = OpenApiSchemaGenerationMode.Inferred,
            OpenApiVersion = version,
            ScalarFormatPolicy = policy,
        };

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var properties = document.Components!.Schemas![$"{nameof(ScalarContractContainer)}.Input"].Properties!;
            Assert.Equal(policy == OpenApiScalarFormatPolicy.Conventional ? "uri-reference" : null, properties["uri"].Format);
            Assert.Equal(policy == OpenApiScalarFormatPolicy.None ? null : "uuid", properties["guid"].Format);
            Assert.Equal(policy == OpenApiScalarFormatPolicy.None ? null : "int32", properties["int32"].Format);
            Assert.Equal(policy == OpenApiScalarFormatPolicy.Conventional ? "float" : null, properties["single"].Format);
            Assert.Null(properties["decimal"].Format);
            Assert.Null(properties["timeSpan"].Format);

            var bytes = Assert.IsType<OpenApiSchema>(properties["bytes"]);
            Assert.Equal(version == OpenApiSpecVersion.OpenApi3_0 ? "byte" : null, bytes.Format);
            Assert.Equal(version == OpenApiSpecVersion.OpenApi3_0 ? null : "base64", bytes.ContentEncoding);
        });
    }

    [Theory]
    [InlineData(OpenApiSpecVersion.OpenApi3_0)]
    [InlineData(OpenApiSpecVersion.OpenApi3_1)]
    [InlineData(OpenApiSpecVersion.OpenApi3_2)]
    public async Task OpenApiScalarFormatCallback_ReceivesBodyContextsAndControlsFormats(
        OpenApiSpecVersion version)
    {
        var contexts = new List<OpenApiScalarFormatContext>();
        var builder = CreateBuilder();
        builder.MapPost("/", (ScalarContractContainer value) => value);
        var options = new OpenApiOptions
        {
            SchemaGenerationMode = OpenApiSchemaGenerationMode.Inferred,
            OpenApiVersion = version,
            CreateScalarFormat = context =>
            {
                contexts.Add(context);
                return context.Provenance switch
                {
                    OpenApiScalarFormatProvenance.CustomConverter => "custom",
                    _ when context.EffectiveType == typeof(Uri) => null,
                    _ when context.EffectiveType == typeof(Guid) => "guid-custom",
                    _ => context.DefaultFormat,
                };
            },
        };

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var input = document.Components!.Schemas![$"{nameof(ScalarContractContainer)}.Input"].Properties!;
            Assert.Null(input["uri"].Format);
            Assert.Equal("guid-custom", input["guid"].Format);
            Assert.Equal("custom", input["customDateTime"].Format);
            Assert.Equal(version == OpenApiSpecVersion.OpenApi3_0 ? "byte" : null, input["bytes"].Format);
            Assert.Equal(
                version == OpenApiSpecVersion.OpenApi3_0 ? null : "base64",
                Assert.IsType<OpenApiSchema>(input["bytes"]).ContentEncoding);
        });

        Assert.Contains(contexts, context =>
            context.Type == typeof(Guid) &&
            context.EffectiveType == typeof(Guid) &&
            context.Location == OpenApiScalarFormatLocation.JsonProperty &&
            context.Purpose == OpenApiScalarFormatPurpose.Input &&
            context.OpenApiVersion == version &&
            context.Provenance == OpenApiScalarFormatProvenance.SystemTextJsonBuiltIn &&
            context.DefaultFormat == "uuid");
        Assert.Contains(contexts, context =>
            context.Type == typeof(DateTime) &&
            context.Location == OpenApiScalarFormatLocation.JsonProperty &&
            context.Provenance == OpenApiScalarFormatProvenance.CustomConverter &&
            context.DefaultFormat is null);
    }

    [Theory]
    [InlineData(OpenApiSpecVersion.OpenApi3_0)]
    [InlineData(OpenApiSpecVersion.OpenApi3_1)]
    [InlineData(OpenApiSpecVersion.OpenApi3_2)]
    public async Task OpenApiScalarFormatOptions_AreIgnoredByLegacy(OpenApiSpecVersion version)
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (ScalarContractContainer value) => { });
        var options = new OpenApiOptions
        {
            SchemaGenerationMode = OpenApiSchemaGenerationMode.Legacy,
            OpenApiVersion = version,
            ScalarFormatPolicy = OpenApiScalarFormatPolicy.None,
            CreateScalarFormat = _ => throw new InvalidOperationException("Legacy must not invoke the callback."),
        };

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var properties = document.Components!.Schemas![nameof(ScalarContractContainer)].Properties!;
            Assert.Equal("uri", properties["uri"].Format);
            Assert.Equal("uuid", properties["guid"].Format);
            Assert.Equal("int32", properties["int32"].Format);
        });
    }

    [Theory]
    [InlineData(OpenApiSpecVersion.OpenApi3_0)]
    [InlineData(OpenApiSpecVersion.OpenApi3_1)]
    [InlineData(OpenApiSpecVersion.OpenApi3_2)]
    public async Task OpenApiScalarContracts_NumberStringsRetainNumericBranchBounds(
        OpenApiSpecVersion version)
    {
        var builder = CreateBuilder(numberHandling: JsonNumberHandling.AllowReadingFromString);
        builder.MapPost("/", (NumericStringContract value) => { });
        var options = new OpenApiOptions
        {
            SchemaGenerationMode = OpenApiSchemaGenerationMode.Inferred,
            OpenApiVersion = version,
        };

        var document = await VerifyOpenApiDocument(builder, options, document =>
        {
            var schema = document.Components!.Schemas![nameof(NumericStringContract)];
            var value = Assert.IsType<OpenApiSchema>(schema.Properties!["value"]);
            Assert.Equal(JsonSchemaType.Integer | JsonSchemaType.String, value.Type);
            Assert.Equal(int.MinValue.ToString(CultureInfo.InvariantCulture), value.Minimum);
            Assert.Equal(int.MaxValue.ToString(CultureInfo.InvariantCulture), value.Maximum);
            Assert.Equal(IntegerPattern, value.Pattern);
        });

        var json = JsonNode.Parse(await document.SerializeAsJsonAsync(version))!;
        var valueSchema = json["components"]!["schemas"]![nameof(NumericStringContract)]!["properties"]!["value"]!;
        Assert.Equal(int.MinValue.ToString(CultureInfo.InvariantCulture), valueSchema["minimum"]!.ToJsonString());
        Assert.Equal(int.MaxValue.ToString(CultureInfo.InvariantCulture), valueSchema["maximum"]!.ToJsonString());
        Assert.Equal(IntegerPattern, valueSchema["pattern"]!.GetValue<string>());
        if (version == OpenApiSpecVersion.OpenApi3_0)
        {
            Assert.Contains(
                valueSchema["anyOf"]!.AsArray(),
                branch => branch!["type"]!.GetValue<string>() == "string");
        }
        else
        {
            Assert.Equal(
                ["integer", "string"],
                valueSchema["type"]!.AsArray().Select(type => type!.GetValue<string>()).Order(StringComparer.Ordinal));
        }
    }

    [Theory]
    [InlineData(OpenApiSpecVersion.OpenApi3_0)]
    [InlineData(OpenApiSpecVersion.OpenApi3_1)]
    [InlineData(OpenApiSpecVersion.OpenApi3_2)]
    public async Task OpenApiScalarContracts_NamedFloatingAlternativesRemainUnbounded(
        OpenApiSpecVersion version)
    {
        var builder = CreateBuilder(numberHandling: JsonNumberHandling.AllowNamedFloatingPointLiterals);
        builder.MapPost("/", (NamedFloatingContract value) => { });
        var options = new OpenApiOptions
        {
            SchemaGenerationMode = OpenApiSchemaGenerationMode.Inferred,
            OpenApiVersion = version,
        };

        var document = await VerifyOpenApiDocument(builder, options, document =>
        {
            var value = Assert.IsType<OpenApiSchema>(
                document.Components!.Schemas![nameof(NamedFloatingContract)].Properties!["value"]);
            Assert.Null(value.Minimum);
            Assert.Null(value.Maximum);
            Assert.Collection(
                value.AnyOf!,
                number => Assert.Equal(JsonSchemaType.Number, number.Type),
                named => Assert.Equal(
                    ["NaN", "Infinity", "-Infinity"],
                    named.Enum!.Select(item => item!.GetValue<string>())));
        });

        var json = JsonNode.Parse(await document.SerializeAsJsonAsync(version))!;
        var valueSchema = json["components"]!["schemas"]![nameof(NamedFloatingContract)]!["properties"]!["value"]!;
        Assert.Null(valueSchema["minimum"]);
        Assert.Null(valueSchema["maximum"]);
        Assert.Equal(
            ["NaN", "Infinity", "-Infinity"],
            valueSchema["anyOf"]![1]!["enum"]!.AsArray().Select(item => item!.GetValue<string>()));
    }

    [Theory]
    [InlineData(OpenApiSpecVersion.OpenApi3_0)]
    [InlineData(OpenApiSpecVersion.OpenApi3_2)]
    public async Task OpenApiScalarContracts_TransformersObserveVersionedDecisions(OpenApiSpecVersion version)
    {
        OpenApiSchema? integerSchema = null;
        OpenApiSchema? bytesSchema = null;
        var builder = CreateBuilder();
        builder.MapPost("/", (ScalarContractContainer value) => { });
        var options = new OpenApiOptions
        {
            SchemaGenerationMode = OpenApiSchemaGenerationMode.Inferred,
            OpenApiVersion = version,
        };
        options.AddSchemaTransformer((schema, context, _) =>
        {
            if (context.JsonPropertyInfo?.Name == "int32")
            {
                integerSchema = schema;
            }
            else if (context.JsonPropertyInfo?.Name == "bytes")
            {
                bytesSchema = schema;
            }

            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, _ => { });

        Assert.Equal(int.MinValue.ToString(CultureInfo.InvariantCulture), integerSchema!.Minimum);
        Assert.Equal(int.MaxValue.ToString(CultureInfo.InvariantCulture), integerSchema.Maximum);
        Assert.Equal("int32", integerSchema.Format);
        if (version == OpenApiSpecVersion.OpenApi3_0)
        {
            Assert.Equal("byte", bytesSchema!.Format);
            Assert.Null(bytesSchema.ContentEncoding);
        }
        else
        {
            Assert.Null(bytesSchema!.Format);
            Assert.Equal("base64", bytesSchema.ContentEncoding);
        }
    }

    [Theory]
    [InlineData(OpenApiSchemaGenerationMode.Legacy, OpenApiSpecVersion.OpenApi3_0)]
    [InlineData(OpenApiSchemaGenerationMode.Legacy, OpenApiSpecVersion.OpenApi3_1)]
    [InlineData(OpenApiSchemaGenerationMode.Legacy, OpenApiSpecVersion.OpenApi3_2)]
    [InlineData(OpenApiSchemaGenerationMode.Inferred, OpenApiSpecVersion.OpenApi3_0)]
    [InlineData(OpenApiSchemaGenerationMode.Inferred, OpenApiSpecVersion.OpenApi3_1)]
    [InlineData(OpenApiSchemaGenerationMode.Inferred, OpenApiSpecVersion.OpenApi3_2)]
    public async Task OpenApiScalarParameterSourcesKeepCurrentFormats(
        OpenApiSchemaGenerationMode mode,
        OpenApiSpecVersion version)
    {
        var builder = CreateBuilder();
        builder.MapPost(
            "/{routeValue}",
            (DateTime routeValue,
                [FromQuery] TimeOnly queryValue,
                [FromHeader] Uri headerValue,
                [FromForm] decimal formValue) => { });
        var options = new OpenApiOptions
        {
            SchemaGenerationMode = mode,
            OpenApiVersion = version,
        };

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var operation = document.Paths["/{routeValue}"]!.Operations![HttpMethod.Post]!;
            Assert.Collection(
                operation.Parameters!,
                parameter =>
                {
                    Assert.Equal(ParameterLocation.Path, parameter.In);
                    AssertScalar(
                        parameter.Schema!,
                        JsonSchemaType.String,
                        "date-time");
                },
                parameter =>
                {
                    Assert.Equal(ParameterLocation.Query, parameter.In);
                    AssertScalar(
                        parameter.Schema!,
                        JsonSchemaType.String,
                        "time");
                },
                parameter =>
                {
                    Assert.Equal(ParameterLocation.Header, parameter.In);
                    AssertScalar(
                        parameter.Schema!,
                        JsonSchemaType.String,
                        mode == OpenApiSchemaGenerationMode.Legacy ? "uri" : "uri-reference");
                });

            var formSchema = operation.RequestBody!.Content!["application/x-www-form-urlencoded"]!.Schema!;
            AssertScalar(
                formSchema.Properties!["formValue"],
                JsonSchemaType.Number,
                mode == OpenApiSchemaGenerationMode.Legacy ? "double" : null);
        });
    }

    [Theory]
    [InlineData(OpenApiSpecVersion.OpenApi3_0)]
    [InlineData(OpenApiSpecVersion.OpenApi3_1)]
    [InlineData(OpenApiSpecVersion.OpenApi3_2)]
    public async Task OpenApiScalarFormatCallback_ReceivesTransportContextsBeforeTransformers(
        OpenApiSpecVersion version)
    {
        var contexts = new List<OpenApiScalarFormatContext>();
        var transformedFormats = new Dictionary<string, string?>(StringComparer.Ordinal);
        var builder = CreateBuilder();
        builder.MapPost(
            "/{routeValue}",
            (Guid routeValue,
                [FromQuery] Uri queryValue,
                [FromHeader] DateTime headerValue,
                [FromForm] int formValue,
                [FromQuery] Student customValue,
                [FromQuery] bool[] flags) => { });
        var options = new OpenApiOptions
        {
            SchemaGenerationMode = OpenApiSchemaGenerationMode.Inferred,
            OpenApiVersion = version,
            CreateScalarFormat = context =>
            {
                contexts.Add(context);
                return context.Location.ToString().ToLowerInvariant();
            },
        };
        options.AddSchemaTransformer((schema, context, _) =>
        {
            if (context.ParameterDescription?.Name is { } name)
            {
                transformedFormats[name] = schema.Format;
                if (name == "queryValue")
                {
                    schema.Format = "transformer";
                }
            }

            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var operation = document.Paths["/{routeValue}"]!.Operations![HttpMethod.Post]!;
            var parameters = operation.Parameters!.ToDictionary(parameter => parameter.Name!, StringComparer.Ordinal);
            Assert.Equal("route", parameters["routeValue"].Schema!.Format);
            Assert.Equal("transformer", parameters["queryValue"].Schema!.Format);
            Assert.Equal("header", parameters["headerValue"].Schema!.Format);
            Assert.Equal("query", parameters["customValue"].Schema!.Format);
            Assert.Equal("query", parameters["flags"].Schema!.Items!.Format);
            Assert.Equal(
                "form",
                operation.RequestBody!.Content!["application/x-www-form-urlencoded"]!.Schema!.Properties!["formValue"].Format);
        });

        Assert.DoesNotContain(contexts, context => context.Location == OpenApiScalarFormatLocation.JsonBody);
        Assert.Contains(contexts, context =>
            context.Type == typeof(Guid) &&
            context.Location == OpenApiScalarFormatLocation.Route &&
            context.Purpose == OpenApiScalarFormatPurpose.Input &&
            context.Provenance == OpenApiScalarFormatProvenance.FrameworkBuiltInParser &&
            context.DefaultFormat == "uuid");
        Assert.Contains(contexts, context =>
            context.Type == typeof(Student) &&
            context.Location == OpenApiScalarFormatLocation.Query &&
            context.Provenance == OpenApiScalarFormatProvenance.CustomParser &&
            context.DefaultFormat is null);
        Assert.Equal("route", transformedFormats["routeValue"]);
        Assert.Equal("query", transformedFormats["queryValue"]);
        Assert.Equal("header", transformedFormats["headerValue"]);
        Assert.Equal("query", transformedFormats["customValue"]);
        Assert.Equal("form", transformedFormats["formValue"]);
    }

    [Fact]
    public async Task OpenApiScalarFormatCallback_UsesTransportContextsForComplexFormProperties()
    {
        var contexts = new List<OpenApiScalarFormatContext>();
        var builder = CreateBuilder();
        builder.MapPost("/", ([FromForm] ScalarFormContract value) => { });
        var options = new OpenApiOptions
        {
            SchemaGenerationMode = OpenApiSchemaGenerationMode.Inferred,
            CreateScalarFormat = context =>
            {
                contexts.Add(context);
                return context.Location == OpenApiScalarFormatLocation.Form ? "form" : "unexpected";
            },
        };

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var schema = document.Paths["/"]!.Operations![HttpMethod.Post]!
                .RequestBody!.Content!["multipart/form-data"]!.Schema!;
            Assert.Null(schema.Format);
            Assert.Equal("form", schema.Properties!["identifier"].Format);
            Assert.Equal("form", schema.Properties["timestamp"].Format);
        });

        Assert.DoesNotContain(contexts, context =>
            context.Location is OpenApiScalarFormatLocation.JsonBody or OpenApiScalarFormatLocation.JsonProperty);
        Assert.Contains(contexts, context =>
            context.Type == typeof(Guid) &&
            context.Location == OpenApiScalarFormatLocation.Form &&
            context.Provenance == OpenApiScalarFormatProvenance.FrameworkBuiltInParser);
    }

    [Fact]
    public async Task OpenApiScalarFormatCallback_PropagatesExceptions()
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (Guid value) => value);
        var options = new OpenApiOptions
        {
            SchemaGenerationMode = OpenApiSchemaGenerationMode.Inferred,
            CreateScalarFormat = _ => throw new FormatException("Callback failure."),
        };

        var exception = await Assert.ThrowsAsync<FormatException>(
            () => VerifyOpenApiDocument(builder, options, _ => { }));
        Assert.Equal("Callback failure.", exception.Message);
    }

    [Theory]
    [InlineData(OpenApiSpecVersion.OpenApi3_0)]
    [InlineData(OpenApiSpecVersion.OpenApi3_1)]
    [InlineData(OpenApiSpecVersion.OpenApi3_2)]
    public async Task InferredTransportSchemasUseLogicalBinderContracts(OpenApiSpecVersion version)
    {
        var builder = CreateBuilder();
        builder.MapPost(
            "/{routeValue}",
            (int routeValue,
                [FromQuery] ulong queryValue,
                [FromQuery] int[] repeated,
                [FromHeader] decimal headerValue,
                [FromQuery] Priority enumValue,
                [FromQuery] Student customValue,
                [FromForm] DateOnly formValue) => { });
        builder.MapGet("/enum-default", (Priority value = Priority.HighPriority) => { });
        var options = new OpenApiOptions
        {
            SchemaGenerationMode = OpenApiSchemaGenerationMode.Inferred,
            OpenApiVersion = version,
        };

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var operation = document.Paths["/{routeValue}"]!.Operations![HttpMethod.Post]!;
            var parameters = operation.Parameters!.ToDictionary(parameter => parameter.Name!, StringComparer.Ordinal);

            var routeSchema = Assert.IsType<OpenApiSchema>(parameters["routeValue"].Schema);
            AssertScalar(routeSchema, JsonSchemaType.Integer, "int32");
            Assert.Equal(int.MinValue.ToString(CultureInfo.InvariantCulture), routeSchema.Minimum);
            Assert.Equal(int.MaxValue.ToString(CultureInfo.InvariantCulture), routeSchema.Maximum);

            var querySchema = Assert.IsType<OpenApiSchema>(parameters["queryValue"].Schema);
            AssertScalar(querySchema, JsonSchemaType.Integer, "uint64");
            Assert.Equal("0", querySchema.Minimum);
            Assert.Equal(ulong.MaxValue.ToString(CultureInfo.InvariantCulture), querySchema.Maximum);

            var repeatedSchema = Assert.IsType<OpenApiSchema>(parameters["repeated"].Schema);
            Assert.Equal(JsonSchemaType.Array, repeatedSchema.Type);
            var itemSchema = Assert.IsType<OpenApiSchema>(repeatedSchema.Items);
            AssertScalar(itemSchema, JsonSchemaType.Integer, "int32");
            Assert.Equal(int.MinValue.ToString(CultureInfo.InvariantCulture), itemSchema.Minimum);
            Assert.Equal(int.MaxValue.ToString(CultureInfo.InvariantCulture), itemSchema.Maximum);

            var headerSchema = Assert.IsType<OpenApiSchema>(parameters["headerValue"].Schema);
            AssertScalar(headerSchema, JsonSchemaType.Number, null);
            Assert.Null(headerSchema.Minimum);
            Assert.Null(headerSchema.Maximum);

            var enumSchema = Assert.IsType<OpenApiSchema>(parameters["enumValue"].Schema);
            Assert.Collection(
                enumSchema.AnyOf!,
                alternative => AssertScalar(alternative, JsonSchemaType.String, null),
                alternative =>
                {
                    AssertScalar(alternative, JsonSchemaType.Integer, null);
                    Assert.NotNull(alternative.Minimum);
                    Assert.NotNull(alternative.Maximum);
                });
            Assert.Null(enumSchema.Enum);

            AssertScalar(parameters["customValue"].Schema!, JsonSchemaType.String, null);

            var formSchema = operation.RequestBody!.Content!["application/x-www-form-urlencoded"]!.Schema!;
            AssertScalar(formSchema.Properties!["formValue"], JsonSchemaType.String, "date");

            var enumDefault = Assert.Single(
                document.Paths["/enum-default"]!.Operations![HttpMethod.Get]!.Parameters!);
            Assert.Equal("HighPriority", enumDefault.Schema!.Default!.GetValue<string>());
        });
    }

    [Theory]
    [InlineData(OpenApiSpecVersion.OpenApi3_0)]
    [InlineData(OpenApiSpecVersion.OpenApi3_2)]
    public async Task InferredTransportSchemasPreserveAbsenceAndRunBeforeTransformers(OpenApiSpecVersion version)
    {
        OpenApiSchema? transformedSchema = null;
        var builder = CreateBuilder();
        builder.MapGet("/", ([FromQuery] int? value = 42) => { });
        var options = new OpenApiOptions
        {
            SchemaGenerationMode = OpenApiSchemaGenerationMode.Inferred,
            OpenApiVersion = version,
        };
        options.AddSchemaTransformer((schema, context, _) =>
        {
            if (context.ParameterDescription?.Name == "value")
            {
                transformedSchema = schema;
            }
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var parameter = Assert.Single(document.Paths["/"]!.Operations![HttpMethod.Get]!.Parameters!);
            Assert.False(parameter.Required);
            AssertScalar(parameter.Schema!, JsonSchemaType.Integer, "int32");
            Assert.Equal(42, parameter.Schema!.Default!.GetValue<int>());
            Assert.Equal(int.MinValue.ToString(CultureInfo.InvariantCulture), parameter.Schema.Minimum);
            Assert.Equal(int.MaxValue.ToString(CultureInfo.InvariantCulture), parameter.Schema.Maximum);
        });

        Assert.NotNull(transformedSchema);
        AssertScalar(transformedSchema, JsonSchemaType.Integer, "int32");
        Assert.Equal(int.MinValue.ToString(CultureInfo.InvariantCulture), transformedSchema.Minimum);
        Assert.Equal(int.MaxValue.ToString(CultureInfo.InvariantCulture), transformedSchema.Maximum);
    }

    [Theory]
    [InlineData(OpenApiSchemaGenerationMode.Legacy, OpenApiSpecVersion.OpenApi3_1)]
    [InlineData(OpenApiSchemaGenerationMode.Legacy, OpenApiSpecVersion.OpenApi3_2)]
    [InlineData(OpenApiSchemaGenerationMode.Inferred, OpenApiSpecVersion.OpenApi3_0)]
    [InlineData(OpenApiSchemaGenerationMode.Inferred, OpenApiSpecVersion.OpenApi3_1)]
    [InlineData(OpenApiSchemaGenerationMode.Inferred, OpenApiSpecVersion.OpenApi3_2)]
    public async Task ParameterBinding_AcceptsValuesBroaderThanGeneratedScalarSchemas(
        OpenApiSchemaGenerationMode mode,
        OpenApiSpecVersion version)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddOpenApi(options =>
        {
            options.SchemaGenerationMode = mode;
            options.OpenApiVersion = version;
        });
        await using var app = builder.Build();

        app.MapGet(
            "/bind/{dateTime}",
            (DateTime dateTime,
                DateTimeOffset offset,
                TimeOnly time,
                Uri uri,
                BigInteger integer,
                IPAddress address,
                IPEndPoint endPoint,
                [FromHeader] decimal amount) => Results.Ok());
        app.MapOpenApi();

        await app.StartAsync();
        var client = app.GetTestClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/bind/2024-01-02T03:04:05" +
            "?offset=9%2F20%2F2021%204%3A18%3A44%20PM" +
            "&time=4%3A18%3A44%20PM" +
            "&uri=relative%2Fpath" +
            "&integer=123456789012345678901234567890" +
            "&address=2001%3Adb8%3A%3A1" +
            "&endPoint=127.0.0.1%3A8080");
        request.Headers.Add("amount", "1,234.5");

        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var openApi = JsonNode.Parse(await client.GetStringAsync("/openapi/v1.json"))!;
        var parameters = openApi["paths"]!["/bind/{dateTime}"]!["get"]!["parameters"]!.AsArray();
        var legacy = mode == OpenApiSchemaGenerationMode.Legacy;
        AssertParameterSchema(parameters, "dateTime", "string", "date-time");
        AssertParameterSchema(parameters, "offset", "string", "date-time");
        AssertParameterSchema(parameters, "time", "string", "time");
        AssertParameterSchema(parameters, "uri", "string", legacy ? "uri" : "uri-reference");
        AssertParameterSchema(parameters, "integer", legacy ? "string" : "integer", null);
        AssertParameterSchema(parameters, "address", "string", null);
        AssertParameterSchema(parameters, "endPoint", "string", null);
        var amountSchema = GetParameterSchema(parameters, "amount");
        if (legacy)
        {
            Assert.Equal(
                ["number", "string"],
                amountSchema["type"]!.AsArray().Select(type => type!.GetValue<string>()).Order(StringComparer.Ordinal));
            Assert.Equal("double", amountSchema["format"]!.GetValue<string>());
            Assert.Equal(DecimalPattern, amountSchema["pattern"]!.GetValue<string>());
        }
        else
        {
            Assert.Equal("number", amountSchema["type"]!.GetValue<string>());
            Assert.Null(amountSchema["format"]);
            Assert.Null(amountSchema["pattern"]);
        }
        Assert.Null(amountSchema["minimum"]);
        Assert.Null(amountSchema["maximum"]);
    }

    private static readonly Type[] ScalarContractTypes =
    [
        typeof(DateTime), typeof(DateTimeOffset), typeof(DateOnly), typeof(TimeOnly), typeof(TimeSpan),
        typeof(Guid), typeof(Uri), typeof(Version), typeof(char), typeof(Rune),
        typeof(byte[]), typeof(Memory<byte>), typeof(ReadOnlyMemory<byte>),
        typeof(IPAddress), typeof(IPEndPoint), typeof(BigInteger),
        typeof(sbyte), typeof(byte), typeof(short), typeof(ushort),
        typeof(int), typeof(uint), typeof(long), typeof(ulong),
        typeof(Int128), typeof(UInt128), typeof(nint), typeof(nuint),
        typeof(Half), typeof(float), typeof(double), typeof(decimal),
    ];

    private static readonly KeyValuePair<string, string?>[] NumericOpenApiFormats =
    [
        KeyValuePair.Create<string, string?>("signedByte", null),
        KeyValuePair.Create<string, string?>("unsignedByte", "uint8"),
        KeyValuePair.Create<string, string?>("int16", "int16"),
        KeyValuePair.Create<string, string?>("uInt16", "uint16"),
        KeyValuePair.Create<string, string?>("int32", "int32"),
        KeyValuePair.Create<string, string?>("nullableInt32", "int32"),
        KeyValuePair.Create<string, string?>("uInt32", "uint32"),
        KeyValuePair.Create<string, string?>("int64", "int64"),
        KeyValuePair.Create<string, string?>("uInt64", "uint64"),
        KeyValuePair.Create<string, string?>("int128", null),
        KeyValuePair.Create<string, string?>("uInt128", null),
        KeyValuePair.Create<string, string?>("half", null),
        KeyValuePair.Create<string, string?>("single", "float"),
        KeyValuePair.Create<string, string?>("double", "double"),
        KeyValuePair.Create<string, string?>("decimal", "double"),
    ];

    private static readonly IReadOnlyDictionary<string, (string Minimum, string Maximum)> NumericOpenApiBounds =
        new Dictionary<string, (string Minimum, string Maximum)>
        {
            ["signedByte"] = ("-128", "127"),
            ["unsignedByte"] = ("0", "255"),
            ["int16"] = ("-32768", "32767"),
            ["uInt16"] = ("0", "65535"),
            ["int32"] = ("-2147483648", "2147483647"),
            ["nullableInt32"] = ("-2147483648", "2147483647"),
            ["uInt32"] = ("0", "4294967295"),
            ["int64"] = ("-9223372036854775808", "9223372036854775807"),
            ["uInt64"] = ("0", "18446744073709551615"),
            ["int128"] = ("-170141183460469231731687303715884105728", "170141183460469231731687303715884105727"),
            ["uInt128"] = ("0", "340282366920938463463374607431768211455"),
        };

    private static JsonSerializerOptions CreateScalarSerializerOptions(JsonNumberHandling numberHandling)
        => new(JsonSerializerDefaults.Web)
        {
            NumberHandling = numberHandling,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };

    private static string GetScalarType(JsonNode schema)
        => GetSchemaTypes(schema).Single(type => type != "null");

    private static string[] GetSchemaTypes(JsonNode schema)
        => schema["type"] is JsonArray types
            ? types.Select(type => type!.GetValue<string>()).ToArray()
            : [schema["type"]!.GetValue<string>()];

    private static void AssertNumericSchemaHasNoDomainBounds(JsonNode schema)
    {
        Assert.Null(schema["minimum"]);
        Assert.Null(schema["maximum"]);
        Assert.Null(schema["multipleOf"]);
    }

    private static void AssertScalar(
        IOpenApiSchema schema,
        JsonSchemaType expectedType,
        string? format = null,
        string? pattern = null)
    {
        Assert.Equal(expectedType, schema.Type);
        Assert.Equal(format, schema.Format);
        Assert.Equal(pattern, schema.Pattern);
    }

    private static void AssertUnconstrained(IOpenApiSchema schema)
    {
        Assert.Null(schema.Type);
        Assert.Null(schema.Format);
        Assert.Null(schema.Properties);
        Assert.Null(schema.AnyOf);
        Assert.Null(schema.OneOf);
        Assert.Null(schema.AllOf);
    }

    private static IOpenApiSchema ResolveSchema(OpenApiDocument document, IOpenApiSchema schema)
        => schema is OpenApiSchemaReference reference
            ? document.Components!.Schemas![reference.Reference.Id!]
            : schema;

    private static void AssertParameterSchema(
        JsonArray parameters,
        string name,
        string? expectedType,
        string? expectedFormat)
    {
        var schema = GetParameterSchema(parameters, name);
        Assert.Equal(expectedType, schema["type"]?.GetValue<string>());
        Assert.Equal(expectedFormat, schema["format"]?.GetValue<string>());
        Assert.Null(schema["minimum"]);
        Assert.Null(schema["maximum"]);
    }

    private static JsonNode GetParameterSchema(JsonArray parameters, string name)
        => parameters.Single(
            parameter => parameter!["name"]!.GetValue<string>() == name)!["schema"]!;

    private static JsonNode ResolveSerializedSchema(JsonNode document, JsonNode schema)
    {
        if (schema["$ref"] is JsonValue reference)
        {
            var componentName = reference.GetValue<string>().Split('/')[^1];
            return document["components"]!["schemas"]![componentName]!;
        }

        foreach (var keyword in new[] { "oneOf", "anyOf", "allOf" })
        {
            if (schema[keyword] is JsonArray branches)
            {
                var referenceBranch = branches.FirstOrDefault(branch => branch?["$ref"] is not null);
                if (referenceBranch is not null)
                {
                    return ResolveSerializedSchema(document, referenceBranch);
                }
            }
        }

        return schema;
    }

    private sealed class EpochDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => DateTime.UnixEpoch.AddSeconds(reader.GetInt64());

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            => writer.WriteNumberValue((long)(value - DateTime.UnixEpoch).TotalSeconds);
    }

    private sealed class StringInt32Converter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => int.Parse(reader.GetString()!, CultureInfo.InvariantCulture);

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
    }

    private sealed class ArrayByteConverter : JsonConverter<byte[]>
    {
        public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => JsonSerializer.Deserialize<int[]>(ref reader, options)!.Select(value => checked((byte)value)).ToArray();

        public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, value.Select(item => (int)item).ToArray(), options);
    }

    private sealed class NumericStringContract
    {
        public int Value { get; set; }
    }

    private sealed class NamedFloatingContract
    {
        public double Value { get; set; }
    }

    private sealed class ScalarFormContract
    {
        public Guid Identifier { get; set; }
        public DateTime Timestamp { get; set; }
    }

    private sealed class ScalarContractContainer
    {
        public DateTime DateTime { get; set; }
        public DateTimeOffset DateTimeOffset { get; set; }
        public DateOnly DateOnly { get; set; }
        public TimeOnly TimeOnly { get; set; }
        public TimeSpan TimeSpan { get; set; }
        public Guid Guid { get; set; }
        public Uri? Uri { get; set; }
        public Version? Version { get; set; }
        public char Character { get; set; }
        public Rune Rune { get; set; }
        public byte[]? Bytes { get; set; }
        public Memory<byte> Memory { get; set; }
        public ReadOnlyMemory<byte> ReadOnlyMemory { get; set; }
        public IPAddress? IpAddress { get; set; }
        public IPEndPoint? IpEndPoint { get; set; }
        public BigInteger BigInteger { get; set; }
        public nint NativeInt { get; set; }
        public nuint NativeUInt { get; set; }
        public sbyte SignedByte { get; set; }
        public byte UnsignedByte { get; set; }
        public short Int16 { get; set; }
        public ushort UInt16 { get; set; }
        public int Int32 { get; set; }
        public int? NullableInt32 { get; set; }
        public uint UInt32 { get; set; }
        public long Int64 { get; set; }
        public ulong UInt64 { get; set; }
        public Int128 Int128 { get; set; }
        public UInt128 UInt128 { get; set; }
        public Half Half { get; set; }
        public float Single { get; set; }
        public double Double { get; set; }
        public decimal Decimal { get; set; }

        [JsonConverter(typeof(EpochDateTimeConverter))]
        public DateTime CustomDateTime { get; set; }

        [JsonConverter(typeof(StringInt32Converter))]
        public int CustomInt32 { get; set; }

        [JsonConverter(typeof(ArrayByteConverter))]
        public byte[] CustomBytes { get; set; } = [];
    }

    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowReadingFromString)]
    [JsonSerializable(typeof(DateTime))]
    [JsonSerializable(typeof(DateTimeOffset))]
    [JsonSerializable(typeof(DateOnly))]
    [JsonSerializable(typeof(TimeOnly))]
    [JsonSerializable(typeof(TimeSpan))]
    [JsonSerializable(typeof(Guid))]
    [JsonSerializable(typeof(Uri))]
    [JsonSerializable(typeof(Version))]
    [JsonSerializable(typeof(char))]
    [JsonSerializable(typeof(Rune))]
    [JsonSerializable(typeof(byte[]))]
    [JsonSerializable(typeof(Memory<byte>))]
    [JsonSerializable(typeof(ReadOnlyMemory<byte>))]
    [JsonSerializable(typeof(IPAddress))]
    [JsonSerializable(typeof(IPEndPoint))]
    [JsonSerializable(typeof(BigInteger))]
    [JsonSerializable(typeof(sbyte))]
    [JsonSerializable(typeof(byte))]
    [JsonSerializable(typeof(short))]
    [JsonSerializable(typeof(ushort))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(uint))]
    [JsonSerializable(typeof(long))]
    [JsonSerializable(typeof(ulong))]
    [JsonSerializable(typeof(Int128))]
    [JsonSerializable(typeof(UInt128))]
    [JsonSerializable(typeof(nint))]
    [JsonSerializable(typeof(nuint))]
    [JsonSerializable(typeof(Half))]
    [JsonSerializable(typeof(float))]
    [JsonSerializable(typeof(double))]
    [JsonSerializable(typeof(decimal))]
    private sealed partial class ScalarJsonContext : JsonSerializerContext;
}

#nullable restore
#pragma warning restore ASP0040
