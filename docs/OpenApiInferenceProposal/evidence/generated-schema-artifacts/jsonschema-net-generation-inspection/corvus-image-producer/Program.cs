// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;
using Corvus.Text.Json.RuntimeEvaluator;

if (args.Length is < 2 or > 4)
{
    Console.Error.WriteLine(
        "Usage: CorvusImageProducer <compatibility-schema> <generated-csharp> [entry-point] [canonical-bundle]");
    return 1;
}

var schema = Encoding.UTF8.GetBytes(File.ReadAllText(args[0]).TrimEnd('\r', '\n'));
var options = new JsonSchemaEvaluatorOptions
{
    DefaultDialect = JsonSchemaDialect.Draft202012,
    AssertFormat = false,
    AssertContent = true,
    CompileRegularExpressions = false,
    BaseUri = "urn:jsonschema:GeneratedSchemaInspection.FlagshipBundle",
};

using var evaluator = JsonSchemaEvaluator.Compile(schema, options);
var entryPoint = args.Length >= 3
    ? args[2]
    : "urn:jsonschema:GeneratedSchemaInspection.FlagshipModel";
evaluator.RegisterEntryPoints([entryPoint]);
var image = evaluator.ToProgramImage();
var patterns = JsonSchemaEvaluator.GetImagePatterns(image);
var schemaIdentity = args.Length == 4
    ? ReadGraphIdentity(args[3])
    : Convert.ToHexString(SHA256.HashData(schema));
var imageIdentity = Convert.ToHexString(SHA256.HashData(image));
const string corvusCommit = "6af6c149ee5c9461faa9850a34d0e0cff1fd9be2";
const string configurationIdentity = "draft2020-12;format=false;content=true;regex=interpreted;max-depth=128";
var bindingIdentity = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
    $"{schemaIdentity}\n{corvusCommit}\n6\n{configurationIdentity}")));
var base64 = Convert.ToBase64String(image);

var source = $$"""
    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.

    namespace GeneratedSchemaInspection;

    internal static class FlagshipCorvusProgramImage
    {
        internal const string CorvusCommit = "{{corvusCommit}}";
        internal const int ImageVersion = 6;
        internal const string ConfigurationIdentity = "{{configurationIdentity}}";
        internal const string SchemaGraphIdentity = "{{schemaIdentity}}";
        internal const string ImageIdentity = "{{imageIdentity}}";
        internal const string BindingIdentity = "{{bindingIdentity}}";
        internal const int PatternCount = {{patterns.Count}};
        internal static ReadOnlyMemory<byte> Bytes { get; } = Convert.FromBase64String("{{base64}}");
    }
    """;

File.WriteAllText(args[1], source, new UTF8Encoding(false));
Console.WriteLine($"schema-bytes={schema.Length}");
Console.WriteLine($"schema-identity={schemaIdentity}");
Console.WriteLine($"image-version=6");
Console.WriteLine($"image-bytes={image.Length}");
Console.WriteLine($"image-identity={imageIdentity}");
Console.WriteLine($"binding-identity={bindingIdentity}");
Console.WriteLine($"patterns={patterns.Count}");
for (var index = 0; index < patterns.Count; index++)
{
    Console.WriteLine($"pattern-{index}-ecma={patterns[index]}");
    Console.WriteLine($"pattern-{index}-dotnet={JsonSchemaEvaluator.ToDotNetPattern(patterns[index])}");
}

return 0;

static string ReadGraphIdentity(string bundlePath)
{
    using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllBytes(bundlePath));
    if (!document.RootElement.TryGetProperty("graphSha256", out var identity) ||
        identity.GetString() is not { Length: 64 } value)
    {
        throw new InvalidOperationException("The canonical bundle does not contain a valid graphSha256.");
    }

    return value.ToUpperInvariant();
}
