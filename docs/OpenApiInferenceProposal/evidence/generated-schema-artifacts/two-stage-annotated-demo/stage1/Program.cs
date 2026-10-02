// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Linq;
using Artifact = AnnotatedModelStage1.GeneratedJsonSchemas.CanonicalArtifacts.FlagshipModel;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: AnnotatedModelStage1 <output-directory>");
    return 1;
}

var outputDirectory = Path.GetFullPath(args[0]);
Directory.CreateDirectory(outputDirectory);

var bundle = Artifact.BundleUtf8.ToArray();
var bundleNode = JsonNode.Parse(bundle)?.AsObject()
    ?? throw new InvalidOperationException("The generated canonical bundle is not a JSON object.");
var schema = CreateCompatibilitySchema(bundleNode, Artifact.RootUri);

WriteIfChanged(Path.Combine(outputDirectory, "flagship.bundle.json"), bundle);
WriteIfChanged(Path.Combine(outputDirectory, "flagship.manifest.json"), bundle);
WriteIfChanged(
    Path.Combine(outputDirectory, "flagship.schema.json"),
    Encoding.UTF8.GetBytes(schema.ToJsonString()));
WriteIfChanged(
    Path.Combine(outputDirectory, "flagship.generated.props"),
    Encoding.UTF8.GetBytes(
        $"""
        <Project>
          <PropertyGroup>
            <FlagshipSchemaIdentity>{Artifact.GraphSha256}</FlagshipSchemaIdentity>
            <FlagshipRootUri>{Artifact.RootUri}</FlagshipRootUri>
          </PropertyGroup>
        </Project>

        """));

Console.WriteLine($"graph-identity={Artifact.GraphSha256}");
Console.WriteLine($"bundle-bytes={bundle.Length}");
for (var ordinal = 0; ordinal < Artifact.ResourceCount; ordinal++)
{
    var resource = Artifact.GetResource(ordinal);
    Console.WriteLine(
        $"resource={resource.Uri};sha256={resource.Sha256};bytes={resource.CanonicalUtf8.Length}");
}

return 0;

static JsonObject CreateCompatibilitySchema(JsonObject bundle, string rootUri)
{
    var resources = bundle["resources"]?.AsArray()
        ?? throw new InvalidOperationException("The generated canonical bundle has no resources.");
    var schemas = resources
        .Select(static resource => resource?.AsObject()
            ?? throw new InvalidOperationException("A canonical resource entry is null."))
        .ToDictionary(
            static resource => resource["uri"]!.GetValue<string>(),
            static resource => resource["schema"]!.DeepClone().AsObject(),
            StringComparer.Ordinal);
    if (!schemas.TryGetValue(rootUri, out var root))
    {
        throw new InvalidOperationException($"The canonical root resource '{rootUri}' is missing.");
    }

    var definitions = new JsonObject();
    foreach (var resource in schemas.OrderBy(static item => item.Key, StringComparer.Ordinal))
    {
        RewriteReferences(resource.Value, schemas.Keys);
        if (!string.Equals(resource.Key, rootUri, StringComparison.Ordinal))
        {
            resource.Value.Remove("$id");
            resource.Value.Remove("$schema");
            definitions[resource.Key] = resource.Value;
        }
    }

    root["$defs"] = definitions;
    return root;
}

static void RewriteReferences(JsonNode? node, IEnumerable<string> resourceUris)
{
    if (node is JsonObject schema)
    {
        if (schema["$ref"] is JsonValue reference &&
            reference.TryGetValue<string>(out var value) &&
            resourceUris.Contains(value, StringComparer.Ordinal))
        {
            schema["$ref"] = $"#/$defs/{EscapePointer(value)}";
        }

        foreach (var property in schema.ToArray())
        {
            RewriteReferences(property.Value, resourceUris);
        }
    }
    else if (node is JsonArray array)
    {
        foreach (var item in array)
        {
            RewriteReferences(item, resourceUris);
        }
    }
}

static string EscapePointer(string value)
    => value.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);

static void WriteIfChanged(string path, byte[] content)
{
    if (!File.Exists(path) || !File.ReadAllBytes(path).AsSpan().SequenceEqual(content))
    {
        File.WriteAllBytes(path, content);
    }
}
