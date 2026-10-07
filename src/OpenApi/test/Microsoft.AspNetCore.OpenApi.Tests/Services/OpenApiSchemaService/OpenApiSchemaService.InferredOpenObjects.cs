// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;

public partial class OpenApiSchemaServiceTests
{
    [Fact]
    public void InferredShape_ClassifiesObjectDictionaryAndExtensionDataContracts()
    {
        var openDocument = BuildCompositionShape<JsonElementExtensionData>();
        var openDecision = openDocument.CompositionDecisions[typeof(JsonElementExtensionData)].ObjectContract;
        Assert.Equal(InferredObjectContractKind.ExtensionData, openDecision.Kind);
        Assert.Equal(typeof(JsonElement), openDecision.AdditionalPropertiesType?.Identity.Type);
        Assert.Equal("extensionData", openDecision.ExtensionDataProperty?.Identity.JsonName);

        var closedDocument = BuildCompositionShape<DisallowingObject>();
        Assert.Equal(
            InferredObjectContractKind.DisallowUnmappedMembers,
            closedDocument.CompositionDecisions[typeof(DisallowingObject)].ObjectContract.Kind);

        var dictionaryDocument = BuildCompositionShape<Dictionary<string, OpenObjectValue>>();
        Assert.Equal(
            InferredObjectContractKind.NotObject,
            dictionaryDocument.CompositionDecisions[typeof(Dictionary<string, OpenObjectValue>)].ObjectContract.Kind);

        var convertedDictionaryDocument = BuildCompositionShape<ConvertedDictionary>();
        Assert.Equal(
            InferredObjectContractKind.NotObject,
            convertedDictionaryDocument.CompositionDecisions[typeof(ConvertedDictionary)].ObjectContract.Kind);

        var derivedDocument = BuildCompositionShape<OpenDerived>();
        Assert.Equal(
            InferredInheritanceReason.ExtensionData,
            derivedDocument.CompositionDecisions[typeof(OpenDerived)].Inheritance.Reason);
    }

    [Fact]
    public void SchemaGenerationMode_Inferred_ExtensionDataExporterMismatchThrows()
    {
        var inferredSchema = BuildCompositionShape<JsonElementExtensionData>();
        var schema = new JsonObject
        {
            [OpenApiSchemaKeywords.TypeKeyword] = "object",
            [OpenApiSchemaKeywords.AdditionalPropertiesKeyword] = false,
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => schema.ApplyObjectContractDecision(
                inferredSchema.CompositionDecisions[typeof(JsonElementExtensionData)],
                _ => new JsonObject()));

        Assert.Contains("unexpected additional-properties constraints", exception.Message);
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_ExtensionDataRetainsNamedPropertiesAndFallbackSchema()
    {
        var builder = CreateBuilder();
        builder.MapPost("/element", (JsonElementExtensionData value) => { });
        builder.MapPost("/object", (ObjectExtensionData value) => { });
        builder.MapPost("/interface", (InterfaceExtensionData value) => { });

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var element = document.Components.Schemas[nameof(JsonElementExtensionData)];
            Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, element.Properties["name"].Type);
            Assert.Contains("name", element.Required);
            Assert.NotNull(element.AdditionalProperties);
            Assert.Null(element.AdditionalProperties.Type);

            var @object = document.Components.Schemas[nameof(ObjectExtensionData)];
            Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, @object.Properties["name"].Type);
            Assert.NotNull(@object.AdditionalProperties);
            Assert.Null(@object.AdditionalProperties.Type);

            var @interface = document.Components.Schemas[nameof(InterfaceExtensionData)];
            Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, @interface.Properties["name"].Type);
            Assert.NotNull(@interface.AdditionalProperties);
            Assert.Null(@interface.AdditionalProperties.Type);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_ClosedAndDictionaryContractsRemainDistinct()
    {
        var builder = CreateBuilder();
        builder.MapPost("/closed", (DisallowingObject value) => { });
        builder.MapPost("/dictionary", (Dictionary<string, OpenObjectValue> value) => { });

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var closed = document.Components.Schemas[nameof(DisallowingObject)];
            Assert.False(closed.AdditionalPropertiesAllowed);
            Assert.Null(closed.AdditionalProperties);

            var dictionary = document.Paths["/dictionary"].Operations[HttpMethod.Post].RequestBody.Content["application/json"].Schema;
            Assert.Null(dictionary.Properties);
            Assert.Equal(
                nameof(OpenObjectValue),
                Assert.IsType<OpenApiSchemaReference>(dictionary.AdditionalProperties).Reference.Id);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Legacy_ExtensionDataOutputIsUnchanged()
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (JsonElementExtensionData value) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var schema = document.Components.Schemas[nameof(JsonElementExtensionData)];
            Assert.Contains("name", schema.Properties.Keys);
            Assert.Null(schema.AdditionalProperties);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_ExtensionDataSerializationIsStable()
    {
        var first = await CreateDocument(reverse: false);
        var second = await CreateDocument(reverse: true);

        var firstSchemas = JsonNode.Parse(await first.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1))?["components"]?["schemas"];
        var secondSchemas = JsonNode.Parse(await second.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1))?["components"]?["schemas"];
        Assert.True(JsonNode.DeepEquals(firstSchemas, secondSchemas));

        foreach (var version in new[] { OpenApiSpecVersion.OpenApi3_0, OpenApiSpecVersion.OpenApi3_1, OpenApiSpecVersion.OpenApi3_2 })
        {
            var serialized = JsonNode.Parse(await first.SerializeAsJsonAsync(version));
            Assert.IsType<JsonObject>(
                serialized?["components"]?["schemas"]?[nameof(JsonElementExtensionData)]?["additionalProperties"]);
        }

        static Task<OpenApiDocument> CreateDocument(bool reverse)
        {
            var builder = CreateBuilder();
            if (reverse)
            {
                builder.MapPost("/object", (ObjectExtensionData value) => { });
                builder.MapPost("/element", (JsonElementExtensionData value) => { });
            }
            else
            {
                builder.MapPost("/element", (JsonElementExtensionData value) => { });
                builder.MapPost("/object", (ObjectExtensionData value) => { });
            }

            return VerifyOpenApiDocument(builder, CreateInferredOptions(), _ => { });
        }
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_MixedObjectInheritanceRemainsFlattened()
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (OpenDerived value) => { });

        await VerifyOpenApiDocument(builder, CreateInferredOptions(), document =>
        {
            var schema = document.Components.Schemas[nameof(OpenDerived)];
            Assert.Null(schema.AllOf);
            Assert.Contains("baseName", schema.Properties.Keys);
            Assert.Contains("localName", schema.Properties.Keys);
            Assert.NotNull(schema.AdditionalProperties);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_ExtensionDataTransformerContextFollowsNamedProperties()
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (JsonElementExtensionData value) => { });
        var contexts = new List<(Type Type, string PropertyName)>();
        var options = CreateInferredOptions();
        options.AddSchemaTransformer((_, context, _) =>
        {
            contexts.Add((context.JsonTypeInfo.Type, context.JsonPropertyInfo?.Name));
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, _ => { });

        Assert.Equal(
            [
                (typeof(JsonElementExtensionData), null),
                (typeof(string), "name"),
                (typeof(JsonElement), "extensionData"),
            ],
            contexts);
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_TransformerAuthoredAdditionalPropertiesIsUntouched()
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (OpenObjectValue value) => { });
        var options = CreateInferredOptions();
        options.AddSchemaTransformer((schema, context, _) =>
        {
            if (context.JsonTypeInfo.Type == typeof(OpenObjectValue))
            {
                schema.AdditionalProperties = new OpenApiSchema
                {
                    Type = JsonSchemaType.Integer,
                    Format = "custom",
                };
            }
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var schema = document.Components.Schemas[nameof(OpenObjectValue)];
            Assert.Equal(JsonSchemaType.Integer, schema.AdditionalProperties.Type);
            Assert.Equal("custom", schema.AdditionalProperties.Format);
        });
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_InvalidExtensionDataShapeUsesSerializerValidation()
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (InvalidExtensionData value) => { });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => VerifyOpenApiDocument(builder, CreateInferredOptions(), _ => { }));

        Assert.Contains("extension data", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SchemaGenerationMode_Inferred_ExtensionDataWithoutReliableValueContractThrows()
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (JsonObjectExtensionData value) => { });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => VerifyOpenApiDocument(builder, CreateInferredOptions(), _ => { }));

        Assert.Contains("value contract", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class JsonElementExtensionData
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; } = [];
    }

    private sealed class ObjectExtensionData
    {
        public string Name { get; set; } = string.Empty;

        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; } = [];
    }

    private sealed class InterfaceExtensionData
    {
        public string Name { get; set; } = string.Empty;

        [JsonExtensionData]
        public IDictionary<string, JsonElement> ExtensionData { get; set; } = new Dictionary<string, JsonElement>();
    }

    private sealed class JsonObjectExtensionData
    {
        [JsonExtensionData]
        public JsonObject ExtensionData { get; set; } = [];
    }

    [JsonConverter(typeof(ConvertedDictionaryConverter))]
    private sealed class ConvertedDictionary : Dictionary<string, int>
    {
    }

    private sealed class ConvertedDictionaryConverter : JsonConverter<ConvertedDictionary>
    {
        public override ConvertedDictionary Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotSupportedException();

        public override void Write(Utf8JsonWriter writer, ConvertedDictionary value, JsonSerializerOptions options)
            => throw new NotSupportedException();
    }

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    private sealed class DisallowingObject
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class OpenObjectValue
    {
        public int Value { get; set; }
    }

    private class OpenBase
    {
        public string BaseName { get; set; } = string.Empty;

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; } = [];
    }

    private sealed class OpenDerived : OpenBase
    {
        public string LocalName { get; set; } = string.Empty;
    }

    private sealed class InvalidExtensionData
    {
        [JsonExtensionData]
        public Dictionary<string, string> ExtensionData { get; set; } = [];
    }
}
