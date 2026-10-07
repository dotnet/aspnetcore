// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Linq;
using Json.Schema;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: AnnotatedSchemaExporter <stage1-assembly> <output-directory>");
    return 1;
}

var assemblyPath = Path.GetFullPath(args[0]);
var outputDirectory = Path.GetFullPath(args[1]);
Directory.CreateDirectory(outputDirectory);

const string generatedTypeName = "AnnotatedModelStage1.GeneratedJsonSchemas";
var expected = new[]
{
    new Resource("FlagshipModel", "urn:jsonschema:GeneratedSchemaInspection.FlagshipModel"),
    new Resource("FlagshipAddress", "urn:jsonschema:GeneratedSchemaInspection.FlagshipAddress"),
};

var context = new EvidenceLoadContext(assemblyPath);
try
{
    var assembly = context.LoadFromAssemblyPath(assemblyPath);
    var generatedType = assembly.GetType(generatedTypeName, throwOnError: true)!;
    var fields = generatedType.GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(static field => field.FieldType == typeof(JsonSchema))
        .OrderBy(static field => field.Name, StringComparer.Ordinal)
        .ToArray();
    var actualNames = fields.Select(static field => field.Name).ToArray();
    var expectedNames = expected.Select(static resource => resource.FieldName)
        .OrderBy(static name => name, StringComparer.Ordinal)
        .ToArray();
    if (!actualNames.SequenceEqual(expectedNames, StringComparer.Ordinal))
    {
        throw new InvalidOperationException(
            $"Unexpected generated resource set: {string.Join(", ", actualNames)}.");
    }

    var schemas = expected.ToDictionary(
        static resource => resource.Uri,
        resource => (JsonSchema)generatedType.GetField(
            resource.FieldName,
            BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!,
        StringComparer.Ordinal);
    var registry = new SchemaRegistry();
    foreach (var resource in expected.OrderBy(static resource => resource.Uri, StringComparer.Ordinal))
    {
        registry.Register(schemas[resource.Uri]);
    }

    const string bundleUri = "urn:jsonschema:GeneratedSchemaInspection.FlagshipBundle";
    var bundle = registry.CreateBundle(
        new Uri(expected[0].Uri),
        new Uri(bundleUri),
        new BuildOptions
        {
            Dialect = Dialect.Draft202012,
            SchemaRegistry = registry,
        }) ?? throw new InvalidOperationException("The supported registry API did not produce a complete bundle.");

    var bundleNode = JsonSerializer.SerializeToNode(bundle)
        ?? throw new InvalidOperationException("The generated bundle serialized to null.");
    RewriteKnownReferences(bundleNode, expected.Select(static resource => resource.Uri).ToHashSet(StringComparer.Ordinal));
    FlattenBundledResources(bundleNode);
    var bundleBytes = Canonicalize(bundleNode);
    var reparsed = JsonSchema.FromText(
        Encoding.UTF8.GetString(bundleBytes),
        new BuildOptions
        {
            Dialect = Dialect.Draft202012,
            SchemaRegistry = new SchemaRegistry(),
        });
    using var probe = JsonDocument.Parse(
        """{"isActive":false,"email":"person@example.com","score":50,"address":{"country":"GB"}}""");
    if (!reparsed.Evaluate(probe.RootElement, new EvaluationOptions()).IsValid)
    {
        throw new InvalidOperationException("The exported bundle does not resolve its complete resource graph.");
    }

    var manifestResources = expected
        .OrderBy(static resource => resource.Uri, StringComparer.Ordinal)
        .Select(resource =>
        {
            var bytes = Canonicalize(JsonSerializer.SerializeToNode(schemas[resource.Uri])!);
            return new
            {
                uri = resource.Uri,
                field = resource.FieldName,
                sha256 = Hash(bytes),
                length = bytes.Length,
            };
        })
        .ToArray();
    var graphIdentity = Hash(bundleBytes);
    var manifest = new
    {
        formatVersion = 1,
        rootUri = expected[0].Uri,
        bundleUri,
        dialect = "https://json-schema.org/draft/2020-12/schema",
        graphIdentity,
        bundleSha256 = graphIdentity,
        bundleLength = bundleBytes.Length,
        resources = manifestResources,
        provenance = new
        {
            producer = "JsonSchema.Net.Generation",
            producerVersion = "7.3.11",
            schemaRuntime = "JsonSchema.Net",
            schemaRuntimeVersion = "9.4.0",
            generatedType = generatedTypeName,
            configuration = "camelCase;strictConditionals=true;formatAssertions=false",
        },
    };
    var manifestBytes = Canonicalize(JsonSerializer.SerializeToNode(manifest)!);

    WriteIfChanged(Path.Combine(outputDirectory, "flagship.bundle.json"), bundleBytes);
    WriteIfChanged(Path.Combine(outputDirectory, "flagship.manifest.json"), manifestBytes);
    Console.WriteLine($"graph-identity={graphIdentity}");
    Console.WriteLine($"bundle-bytes={bundleBytes.Length}");
    foreach (var resource in manifestResources)
    {
        Console.WriteLine($"resource={resource.uri};sha256={resource.sha256};bytes={resource.length}");
    }
}
finally
{
    context.Unload();
}

return 0;

static byte[] Canonicalize(JsonNode node)
{
    using var stream = new MemoryStream();
    using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Indented = false,
    }))
    {
        WriteCanonical(writer, node);
    }

    return stream.ToArray();
}

static void WriteCanonical(Utf8JsonWriter writer, JsonNode? node)
{
    switch (node)
    {
        case JsonObject value:
            writer.WriteStartObject();
            foreach (var property in value.OrderBy(static property => property.Key, StringComparer.Ordinal))
            {
                writer.WritePropertyName(property.Key);
                WriteCanonical(writer, property.Value);
            }
            writer.WriteEndObject();
            break;
        case JsonArray value:
            writer.WriteStartArray();
            foreach (var item in value)
            {
                WriteCanonical(writer, item);
            }
            writer.WriteEndArray();
            break;
        case null:
            writer.WriteNullValue();
            break;
        default:
            node.WriteTo(writer);
            break;
    }
}

static void RewriteKnownReferences(JsonNode? node, HashSet<string> resourceUris)
{
    switch (node)
    {
        case JsonObject value:
            if (value["$ref"] is JsonValue referenceValue &&
                referenceValue.TryGetValue<string>(out var reference) &&
                resourceUris.Contains(reference))
            {
                value["$ref"] = $"#/$defs/{reference.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal)}";
            }
            foreach (var property in value)
            {
                RewriteKnownReferences(property.Value, resourceUris);
            }
            break;
        case JsonArray value:
            foreach (var item in value)
            {
                RewriteKnownReferences(item, resourceUris);
            }
            break;
    }
}

static void FlattenBundledResources(JsonNode bundle)
{
    if (bundle["$defs"] is not JsonObject definitions)
    {
        throw new InvalidOperationException("The supported bundle did not contain a resource definition map.");
    }

    foreach (var resource in definitions)
    {
        if (resource.Value is not JsonObject schema)
        {
            throw new InvalidOperationException($"The bundled resource '{resource.Key}' is not an object schema.");
        }

        schema.Remove("$id");
        schema.Remove("$schema");
    }
}

static string Hash(ReadOnlySpan<byte> bytes)
    => Convert.ToHexString(SHA256.HashData(bytes));

static void WriteIfChanged(string path, byte[] content)
{
    if (!File.Exists(path) || !File.ReadAllBytes(path).AsSpan().SequenceEqual(content))
    {
        File.WriteAllBytes(path, content);
    }
}

sealed record Resource(string FieldName, string Uri);

sealed class EvidenceLoadContext(string assemblyPath) : AssemblyLoadContext(isCollectible: true)
{
    private readonly AssemblyDependencyResolver _resolver = new(assemblyPath);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name is "JsonSchema.Net" or "System.Runtime" ||
            assemblyName.Name?.StartsWith("System.", StringComparison.Ordinal) is true)
        {
            return null;
        }

        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path is null ? null : LoadFromAssemblyPath(path);
    }
}
