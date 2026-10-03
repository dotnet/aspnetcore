// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
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
    public static TheoryData<Type, Type> EnumerableContracts => new()
    {
        { typeof(int[]), typeof(int) },
        { typeof(int[][]), typeof(int[]) },
        { typeof(List<int>), typeof(int) },
        { typeof(IList<int>), typeof(int) },
        { typeof(IReadOnlyList<int>), typeof(int) },
        { typeof(IEnumerable<int>), typeof(int) },
        { typeof(HashSet<int>), typeof(int) },
        { typeof(Queue<int>), typeof(int) },
        { typeof(Stack<int>), typeof(int) },
        { typeof(ImmutableArray<int>), typeof(int) },
        { typeof(ImmutableList<int>), typeof(int) },
        { typeof(FrozenSet<int>), typeof(int) },
        { typeof(ReadOnlyCollection<int>), typeof(int) },
        { typeof(ArrayList), typeof(object) },
        { typeof(IEnumerable), typeof(object) },
        { typeof(IAsyncEnumerable<int>), typeof(int) },
        { typeof(Memory<int>), typeof(int) },
        { typeof(ReadOnlyMemory<int>), typeof(int) },
        { typeof(CollectionWithProperty), typeof(int) },
    };

    public static TheoryData<Type> DictionaryContracts => new()
    {
        typeof(Dictionary<string, int?>),
        typeof(Dictionary<CollectionKey, int?>),
        typeof(Dictionary<int, int?>),
        typeof(Dictionary<Guid, int?>),
        typeof(Dictionary<DateTime, int?>),
        typeof(Dictionary<UnsupportedCollectionKey, int?>),
        typeof(ImmutableDictionary<string, int?>),
        typeof(FrozenDictionary<string, int?>),
        typeof(ReadOnlyDictionary<string, int?>),
        typeof(DictionaryWithProperty),
    };

    [Theory]
    [MemberData(nameof(EnumerableContracts))]
    public void JsonSchemaExporter_EffectiveEnumerableContractsHaveTypedItems(Type type, Type expectedElementType)
    {
        var options = CreateCollectionSerializerOptions();
        var typeInfo = options.GetTypeInfo(type);
        var schema = JsonSchemaExporter.GetJsonSchemaAsNode(typeInfo);

        Assert.Equal(JsonTypeInfoKind.Enumerable, typeInfo.Kind);
        Assert.Equal(expectedElementType, typeInfo.ElementType);
        Assert.Equal("array", GetSingleOrUnionType(schema, "array"));
        Assert.Null(schema["uniqueItems"]);
        Assert.Null(schema["propertyNames"]);
        Assert.DoesNotContain("x-jsonSchema-", schema.ToJsonString());

        if (expectedElementType == typeof(object))
        {
            Assert.Null(schema["items"]);
        }
        else
        {
            Assert.NotNull(schema["items"]);
        }
    }

    [Theory]
    [MemberData(nameof(DictionaryContracts))]
    public void JsonSchemaExporter_EffectiveDictionaryContractsHaveTypedAdditionalProperties(Type type)
    {
        var options = CreateCollectionSerializerOptions();
        var typeInfo = options.GetTypeInfo(type);
        var schema = JsonSchemaExporter.GetJsonSchemaAsNode(typeInfo);

        Assert.Equal(JsonTypeInfoKind.Dictionary, typeInfo.Kind);
        Assert.Equal(typeof(int?), typeInfo.ElementType);
        Assert.Equal("object", GetSingleOrUnionType(schema, "object"));
        Assert.Equal("integer", GetSingleOrUnionType(schema["additionalProperties"]!, "integer"));
        Assert.Contains("null", GetTypes(schema["additionalProperties"]!));
        Assert.Null(schema["propertyNames"]);
        Assert.Null(schema["uniqueItems"]);
        Assert.DoesNotContain("x-jsonSchema-", schema.ToJsonString());
    }

    [Fact]
    public void JsonSchemaExporter_PreservesUnsupportedAndCustomConverterContracts()
    {
        var options = CreateCollectionSerializerOptions();

        var multidimensional = JsonSchemaExporter.GetJsonSchemaAsNode(options.GetTypeInfo(typeof(int[,])));
        Assert.Equal(JsonTypeInfoKind.None, options.GetTypeInfo(typeof(int[,])).Kind);
        Assert.Equal("Unsupported .NET type", multidimensional["$comment"]!.GetValue<string>());
        Assert.True(multidimensional["not"]!.GetValue<bool>());

        foreach (var type in new[] { typeof(ConvertedCollection), typeof(CollectionConvertedDictionary) })
        {
            var typeInfo = options.GetTypeInfo(type);
            Assert.Equal(JsonTypeInfoKind.None, typeInfo.Kind);
            Assert.Equal(JsonValueKind.True, JsonSchemaExporter.GetJsonSchemaAsNode(typeInfo).GetValueKind());
        }
    }

    [Fact]
    public void CollectionRuntimeBehavior_DoesNotImplySchemaConstraints()
    {
        var options = CreateCollectionSerializerOptions();

        var set = JsonSerializer.Deserialize<HashSet<int>>("[1,1,2]", options);
        Assert.Equal([1, 2], set!.Order());
        Assert.Null(JsonSchemaExporter.GetJsonSchemaAsNode(options.GetTypeInfo(typeof(HashSet<int>)))["uniqueItems"]);

        var queue = new Queue<int>([1, 2]);
        Assert.Equal("[1,2]", JsonSerializer.Serialize(queue, options));
        var stack = new Stack<int>();
        stack.Push(1);
        stack.Push(2);
        Assert.Equal("[2,1]", JsonSerializer.Serialize(stack, options));

        var collection = new CollectionWithProperty { 1, 2 };
        collection.Name = "ignored";
        Assert.Equal("[1,2]", JsonSerializer.Serialize(collection, options));

        var dictionary = new DictionaryWithProperty { ["value"] = 1, Name = "ignored" };
        Assert.Equal("""{"value":1}""", JsonSerializer.Serialize(dictionary, options));
    }

    [Fact]
    public void DictionaryRuntimeBehavior_DoesNotImplyPropertyNameConstraints()
    {
        var options = CreateCollectionSerializerOptions();
        options.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;

        Assert.Equal("""{"pascal_case":1}""", JsonSerializer.Serialize(new Dictionary<string, int> { ["PascalCase"] = 1 }, options));
        var schema = JsonSchemaExporter.GetJsonSchemaAsNode(options.GetTypeInfo(typeof(Dictionary<string, int>)));
        Assert.Null(schema["propertyNames"]);

        var unsupported = new Dictionary<UnsupportedCollectionKey, int> { [new("key")] = 1 };
        Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(unsupported, options));
        var unsupportedSchema = JsonSchemaExporter.GetJsonSchemaAsNode(options.GetTypeInfo(unsupported.GetType()));
        Assert.NotNull(unsupportedSchema["additionalProperties"]);
        Assert.Null(unsupportedSchema["propertyNames"]);
    }

    [Fact]
    public void InferredShape_ReflectionAndSourceGeneratedCollectionFactsAgree()
    {
        var reflection = CreateCollectionSerializerOptions();
        var generated = new JsonSerializerOptions(CollectionJsonContext.Default.Options);

        foreach (var type in new[]
        {
            typeof(int[]),
            typeof(List<int>),
            typeof(HashSet<int>),
            typeof(Dictionary<string, int?>),
            typeof(CollectionContractItem),
        })
        {
            var reflectionTypeInfo = reflection.GetTypeInfo(type);
            var generatedTypeInfo = generated.GetTypeInfo(type);

            Assert.Equal(reflectionTypeInfo.Kind, generatedTypeInfo.Kind);
            Assert.Equal(reflectionTypeInfo.ElementType, generatedTypeInfo.ElementType);
        }
    }

    [Theory]
    [InlineData(OpenApiSchemaGenerationMode.Legacy, OpenApiSpecVersion.OpenApi3_0)]
    [InlineData(OpenApiSchemaGenerationMode.Legacy, OpenApiSpecVersion.OpenApi3_1)]
    [InlineData(OpenApiSchemaGenerationMode.Legacy, OpenApiSpecVersion.OpenApi3_2)]
    [InlineData(OpenApiSchemaGenerationMode.Inferred, OpenApiSpecVersion.OpenApi3_0)]
    [InlineData(OpenApiSchemaGenerationMode.Inferred, OpenApiSpecVersion.OpenApi3_1)]
    [InlineData(OpenApiSchemaGenerationMode.Inferred, OpenApiSpecVersion.OpenApi3_2)]
    public async Task CollectionContracts_PreserveSchemasAndVersionCleanliness(
        OpenApiSchemaGenerationMode mode,
        OpenApiSpecVersion version)
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (CollectionContractContainer value) => value);
        var options = new OpenApiOptions
        {
            SchemaGenerationMode = mode,
            OpenApiVersion = version,
        };

        var document = await VerifyOpenApiDocument(builder, options, document =>
        {
            var container = document.Components!.Schemas![nameof(CollectionContractContainer)];
            var properties = Assert.IsAssignableFrom<IDictionary<string, IOpenApiSchema>>(container.Properties);
            AssertTypedArray(properties["array"]);
            AssertTypedArray(properties["jagged"], JsonSchemaType.Array);
            AssertTypedArray(properties["list"]);
            AssertTypedArray(properties["set"]);
            AssertTypedArray(properties["queue"]);
            AssertTypedArray(properties["stack"]);
            AssertTypedArray(properties["immutable"]);
            AssertTypedArray(properties["memory"]);
            Assert.Null(properties["set"].UniqueItems);

            var nonGeneric = properties["nonGeneric"];
            Assert.Equal(JsonSchemaType.Array, nonGeneric.Type);
            Assert.Null(nonGeneric.Items);

            var collectionSubclass = properties["collectionSubclass"];
            Assert.Equal(JsonSchemaType.Array, collectionSubclass.Type);
            Assert.Null(collectionSubclass.Properties);

            var dictionary = Assert.IsType<OpenApiSchema>(properties["dictionary"]);
            Assert.Equal(JsonSchemaType.Object, dictionary.Type);
            var additionalProperties = Assert.IsType<OpenApiSchema>(dictionary.AdditionalProperties);
            Assert.True(additionalProperties.Type?.HasFlag(JsonSchemaType.Integer));
            Assert.True(additionalProperties.Type?.HasFlag(JsonSchemaType.Null));
            Assert.Null(dictionary.PropertyNames);

            var dictionarySubclass = properties["dictionarySubclass"];
            Assert.Equal(JsonSchemaType.Object, dictionarySubclass.Type);
            Assert.Null(dictionarySubclass.Properties);

            Assert.Null(properties["convertedCollection"].Type);
            Assert.Null(properties["convertedDictionary"].Type);
        });

        var json = await document.SerializeAsJsonAsync(version);
        Assert.DoesNotContain("\"propertyNames\"", json);
        Assert.DoesNotContain("\"uniqueItems\"", json);
        Assert.DoesNotContain("x-jsonSchema-", json);
    }

    [Theory]
    [InlineData(OpenApiSchemaGenerationMode.Legacy)]
    [InlineData(OpenApiSchemaGenerationMode.Inferred)]
    public async Task CollectionContracts_TransformerTraversalAndRecursiveReferencesAreStable(OpenApiSchemaGenerationMode mode)
    {
        var visited = new List<Type>();
        var builder = CreateBuilder();
        builder.MapPost("/", (Dictionary<string, List<RecursiveCollectionValue>> value) => value);
        var options = new OpenApiOptions { SchemaGenerationMode = mode };
        options.AddSchemaTransformer((_, context, _) =>
        {
            visited.Add(context.JsonTypeInfo.Type);
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var requestSchema = document.Paths!["/"].Operations![HttpMethod.Post].RequestBody!.Content!["application/json"].Schema!;
            var listSchema = Assert.IsType<OpenApiSchema>(requestSchema.AdditionalProperties);
            var itemReference = Assert.IsType<OpenApiSchemaReference>(listSchema.Items);
            Assert.Equal(nameof(RecursiveCollectionValue), itemReference.Reference.Id);

            var recursive = document.Components!.Schemas![nameof(RecursiveCollectionValue)];
            var recursiveProperties = Assert.IsAssignableFrom<IDictionary<string, IOpenApiSchema>>(recursive.Properties);
            var nullableNext = Assert.IsType<OpenApiSchema>(recursiveProperties["next"]);
            Assert.Equal(nameof(RecursiveCollectionValue), Assert.Single(nullableNext.OneOf!.OfType<OpenApiSchemaReference>()).Reference.Id);
        });

        var dictionaryType = typeof(Dictionary<string, List<RecursiveCollectionValue>>);
        var listType = typeof(List<RecursiveCollectionValue>);
        var dictionaryVisits = visited.Count(type => type == dictionaryType);
        Assert.True(dictionaryVisits >= 2);
        Assert.Equal(dictionaryVisits, visited.Count(type => type == listType));
        Assert.True(visited.Count(type => type == typeof(RecursiveCollectionValue)) >= dictionaryVisits * 2);
        for (var i = 0; i < visited.Count; i++)
        {
            if (visited[i] == dictionaryType)
            {
                Assert.Equal(listType, visited[i + 1]);
            }
        }
    }

    private static JsonSerializerOptions CreateCollectionSerializerOptions() => new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.Strict,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
    };

    private static string GetSingleOrUnionType(JsonNode schema, string expected)
    {
        Assert.Contains(expected, GetTypes(schema));
        return expected;
    }

    private static string[] GetTypes(JsonNode schema)
        => schema["type"] is JsonArray types
            ? types.Select(type => type!.GetValue<string>()).ToArray()
            : [schema["type"]!.GetValue<string>()];

    private static void AssertTypedArray(IOpenApiSchema schema, JsonSchemaType itemType = JsonSchemaType.Integer)
    {
        Assert.Equal(JsonSchemaType.Array, schema.Type);
        Assert.NotNull(schema.Items);
        Assert.True(schema.Items.Type?.HasFlag(itemType));
    }

    private enum CollectionKey
    {
        FirstValue,
        SecondValue,
    }

    private sealed record UnsupportedCollectionKey(string Value);

    private sealed class CollectionWithProperty : List<int>
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class DictionaryWithProperty : Dictionary<string, int?>
    {
        public string Name { get; set; } = string.Empty;
    }

    [JsonConverter(typeof(ConvertedCollectionConverter))]
    private sealed class ConvertedCollection : List<int>;

    private sealed class ConvertedCollectionConverter : JsonConverter<ConvertedCollection>
    {
        public override ConvertedCollection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            reader.Skip();
            return [];
        }

        public override void Write(Utf8JsonWriter writer, ConvertedCollection value, JsonSerializerOptions options)
            => writer.WriteStringValue(nameof(ConvertedCollection));
    }

    [JsonConverter(typeof(CollectionConvertedDictionaryConverter))]
    private sealed class CollectionConvertedDictionary : Dictionary<string, int>;

    private sealed class CollectionConvertedDictionaryConverter : JsonConverter<CollectionConvertedDictionary>
    {
        public override CollectionConvertedDictionary Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            reader.Skip();
            return [];
        }

        public override void Write(Utf8JsonWriter writer, CollectionConvertedDictionary value, JsonSerializerOptions options)
            => writer.WriteStringValue(nameof(CollectionConvertedDictionary));
    }

    private sealed class CollectionContractContainer
    {
        public int[] Array { get; set; } = [];
        public int[][] Jagged { get; set; } = [];
        public List<int> List { get; set; } = [];
        public HashSet<int> Set { get; set; } = [];
        public Queue<int> Queue { get; set; } = [];
        public Stack<int> Stack { get; set; } = [];
        public ImmutableList<int> Immutable { get; set; } = [];
        public Memory<int> Memory { get; set; }
        public ArrayList NonGeneric { get; set; } = [];
        public CollectionWithProperty CollectionSubclass { get; set; } = [];
        public Dictionary<string, int?> Dictionary { get; set; } = [];
        public DictionaryWithProperty DictionarySubclass { get; set; } = [];
        public ConvertedCollection ConvertedCollection { get; set; } = [];
        public CollectionConvertedDictionary ConvertedDictionary { get; set; } = [];
    }

    private sealed class CollectionContractItem
    {
        public int Value { get; set; }
    }

    private sealed class RecursiveCollectionValue
    {
        public RecursiveCollectionValue? Next { get; set; }
    }

    [JsonSerializable(typeof(int[]))]
    [JsonSerializable(typeof(List<int>))]
    [JsonSerializable(typeof(HashSet<int>))]
    [JsonSerializable(typeof(Dictionary<string, int?>))]
    [JsonSerializable(typeof(CollectionContractItem))]
    private sealed partial class CollectionJsonContext : JsonSerializerContext;
}

#nullable restore
#pragma warning restore ASP0040
