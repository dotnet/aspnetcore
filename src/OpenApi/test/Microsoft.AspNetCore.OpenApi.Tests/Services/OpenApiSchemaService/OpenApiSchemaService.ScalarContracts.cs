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
            Assert.True(JsonNode.DeepEquals(
                JsonSchemaExporter.GetJsonSchemaAsNode(reflectionTypeInfo),
                JsonSchemaExporter.GetJsonSchemaAsNode(generatedTypeInfo)));
        }
    }

    [Fact]
    public void JsonSchemaExporter_CustomScalarConverterRemainsUnconstrained()
    {
        var options = CreateScalarSerializerOptions(JsonNumberHandling.Strict);
        options.Converters.Add(new EpochDateTimeConverter());
        var typeInfo = options.GetTypeInfo(typeof(DateTime));
        var schema = JsonSchemaExporter.GetJsonSchemaAsNode(typeInfo);

        Assert.IsType<EpochDateTimeConverter>(typeInfo.Converter);
        Assert.Equal(JsonTypeInfoKind.None, typeInfo.Kind);
        Assert.Equal(JsonValueKind.True, schema.GetValueKind());
        Assert.Equal("0", JsonSerializer.Serialize(DateTime.UnixEpoch, options));
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
            AssertScalar(properties["uri"], JsonSchemaType.String | JsonSchemaType.Null, "uri");
            AssertScalar(properties["version"], JsonSchemaType.String | JsonSchemaType.Null, pattern: VersionPattern);

            var character = Assert.IsType<OpenApiSchema>(properties["character"]);
            AssertScalar(character, JsonSchemaType.String, "char");
            Assert.Equal(1, character.MinLength);
            Assert.Equal(1, character.MaxLength);

            Assert.Equal(JsonSchemaType.Object, ResolveSchema(document, properties["rune"]).Type);
            AssertScalar(properties["bytes"], JsonSchemaType.String | JsonSchemaType.Null, "byte");
            AssertScalar(ResolveSchema(document, properties["memory"]), JsonSchemaType.String);
            AssertScalar(ResolveSchema(document, properties["readOnlyMemory"]), JsonSchemaType.String);
            Assert.Equal(JsonSchemaType.Object, ResolveSchema(document, properties["bigInteger"]).Type);
            AssertUnconstrained(properties["nativeInt"]);
            AssertUnconstrained(properties["nativeUInt"]);

            foreach (var (name, expectedFormat) in NumericOpenApiFormats)
            {
                var schema = Assert.IsType<OpenApiSchema>(properties[name]);
                Assert.Equal(expectedFormat, schema.Format);
                Assert.Null(schema.Minimum);
                Assert.Null(schema.Maximum);
                Assert.Null(schema.MultipleOf);
            }

            var custom = Assert.IsType<OpenApiSchema>(properties["customDateTime"]);
            Assert.Null(custom.Type);
            Assert.Equal("date-time", custom.Format);
        });

        var json = JsonNode.Parse(await document.SerializeAsJsonAsync(version))!;
        var schemas = json["components"]!["schemas"]!.AsObject();
        var containerName = mode == OpenApiSchemaGenerationMode.Inferred
            ? $"{nameof(ScalarContractContainer)}.Input"
            : nameof(ScalarContractContainer);
        var serializedProperties = schemas[containerName]!["properties"]!.AsObject();
        Assert.Equal("byte", serializedProperties["bytes"]!["format"]!.GetValue<string>());
        Assert.Null(serializedProperties["bytes"]!["contentEncoding"]);
        Assert.Null(schemas["MemoryOfbyte"]!["contentEncoding"]);
        Assert.Null(schemas["ReadOnlyMemoryOfbyte"]!["contentEncoding"]);
        Assert.Equal("object", ResolveSerializedSchema(json, serializedProperties["ipAddress"]!)["type"]!.GetValue<string>());
        Assert.Equal("object", ResolveSerializedSchema(json, serializedProperties["ipEndPoint"]!)["type"]!.GetValue<string>());
        Assert.Empty(serializedProperties["nativeInt"]!.AsObject());
        Assert.Empty(serializedProperties["nativeUInt"]!.AsObject());
        Assert.Equal("date-time", serializedProperties["customDateTime"]!["format"]!.GetValue<string>());
        Assert.Null(serializedProperties["customDateTime"]!["type"]);
        Assert.DoesNotContain("x-jsonSchema-", json.ToJsonString());
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
                    AssertScalar(parameter.Schema!, JsonSchemaType.String, "date-time");
                },
                parameter =>
                {
                    Assert.Equal(ParameterLocation.Query, parameter.In);
                    AssertScalar(parameter.Schema!, JsonSchemaType.String, "time");
                },
                parameter =>
                {
                    Assert.Equal(ParameterLocation.Header, parameter.In);
                    AssertScalar(parameter.Schema!, JsonSchemaType.String, "uri");
                });

            var formSchema = operation.RequestBody!.Content!["application/x-www-form-urlencoded"]!.Schema!;
            AssertScalar(formSchema.Properties!["formValue"], JsonSchemaType.Number, "double");
        });
    }

    [Fact]
    public async Task ParameterBinding_AcceptsValuesBroaderThanGeneratedScalarSchemas()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddOpenApi();
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
        AssertParameterSchema(parameters, "dateTime", "string", "date-time");
        AssertParameterSchema(parameters, "offset", "string", "date-time");
        AssertParameterSchema(parameters, "time", "string", "time");
        AssertParameterSchema(parameters, "uri", "string", "uri");
        AssertParameterSchema(parameters, "integer", "string", null);
        AssertParameterSchema(parameters, "address", "string", null);
        AssertParameterSchema(parameters, "endPoint", "string", null);
        var amountSchema = GetParameterSchema(parameters, "amount");
        Assert.Equal(
            ["number", "string"],
            amountSchema["type"]!.AsArray().Select(type => type!.GetValue<string>()).Order(StringComparer.Ordinal));
        Assert.Equal("double", amountSchema["format"]!.GetValue<string>());
        Assert.Equal(DecimalPattern, amountSchema["pattern"]!.GetValue<string>());
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
