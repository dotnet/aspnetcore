using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

if (args.Length is < 2 or > 3 ||
    !Enum.TryParse<OpenApiSchemaGenerationMode>(args[0], ignoreCase: true, out var mode) ||
    !TryParseVersion(args[1], out var version) ||
    !TryParsePolicy(args.ElementAtOrDefault(2), out var policy, out var useCallback))
{
    Console.Error.WriteLine("Usage: dotnet run -- <Legacy|Inferred> <3.0|3.1|3.2> [Conventional|CompatibleOnly|None|Callback]");
    return 2;
}

const string documentName = "evidence";
var builder = WebApplication.CreateBuilder();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
    options.SerializerOptions.Converters.Add(JsonArrayTupleConverters.CreateValueTuple<long, bool>());
});
builder.Services.AddOpenApi(documentName, options =>
{
    options.SchemaGenerationMode = mode;
    options.OpenApiVersion = version;
    options.ScalarFormatPolicy = policy;
    if (useCallback)
    {
        options.CreateScalarFormat = context => context.EffectiveType switch
        {
            var type when type == typeof(Guid) => "guid-custom",
            var type when type == typeof(Uri) => null,
            _ => context.DefaultFormat,
        };
    }
});

await using var app = builder.Build();

app.MapPost("/directional", (DirectionalDto value) => value)
    .WithName("DirectionalContract");

app.MapGet("/inheritance", () => new Customer())
    .WithName("LosslessInheritance");
app.MapGet("/polymorphism", () => (Animal)new Cat())
    .WithName("DiscriminatedPolymorphism");

app.MapPost("/serializer", (SerializerEnvelope value) => value)
    .WithName("EffectiveSerializerContract");

app.MapGet(
        "/transport/{routeValue}",
        (int routeValue,
            [FromQuery] ulong count,
            [FromQuery] Uri relativeUri,
            [FromQuery] Guid identifier,
            [FromQuery] DateTime timestamp,
            [FromQuery] BigInteger bigInteger,
            [FromQuery] IPAddress address,
            [FromQuery] IPEndPoint endpoint,
            [FromHeader] decimal amount) => Results.Ok())
    .WithName("TransportContracts");

app.MapGet("/tuple", () => (42L, true))
    .WithName("PositionalTuple");

await app.StartAsync();
var serializerOptions = app.Services
    .GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>()
    .Value.SerializerOptions;
try
{
    JsonSerializer.Deserialize<sbyte>("\"128\"", serializerOptions);
    throw new InvalidOperationException("System.Text.Json unexpectedly accepted the out-of-range quoted sbyte value \"128\".");
}
catch (JsonException)
{
    Console.Error.WriteLine("System.Text.Json rejected the out-of-range quoted sbyte value \"128\".");
    Directory.CreateDirectory("validation");
    await File.WriteAllTextAsync(
        Path.Combine("validation", "runtime-probe.json"),
        """
        {
          "quotedSByte128": "rejected-by-system-text-json"
        }

        """);
}

var tupleTypeInfo = (JsonTypeInfo<(long, bool)>)serializerOptions.GetTypeInfo(typeof((long, bool)));
var tupleJson = JsonSerializer.Serialize((42L, true), tupleTypeInfo);
if (tupleJson != "[42,true]")
{
    throw new InvalidOperationException($"The explicitly opted-in tuple contract produced '{tupleJson}'.");
}

var provider = app.Services.GetRequiredKeyedService<IOpenApiVersionedDocumentProvider>(documentName);
var document = await provider.GetOpenApiDocumentForVersionAsync(version);
var outputPath = Path.GetFullPath(
    Path.Combine(
        "documents",
        $"{mode.ToString().ToLowerInvariant()}-oas{args[1].Replace(".", string.Empty, StringComparison.Ordinal)}{GetPolicySuffix(args.ElementAtOrDefault(2))}.json"));

await using (var stream = File.Create(outputPath))
await using (var textWriter = new StreamWriter(stream))
{
    var writer = new OpenApiJsonWriter(textWriter);
    await document.SerializeAsync(writer, version);
}

var json = await File.ReadAllTextAsync(outputPath);
var parseResult = OpenApiDocument.Parse(json, format: "json");
if (parseResult.Diagnostic?.Errors is { Count: > 0 } errors)
{
    foreach (var error in errors)
    {
        Console.Error.WriteLine(error.Message);
    }

    return 1;
}

Console.WriteLine($"{mode} OpenAPI {args[1]}: {outputPath} ({new FileInfo(outputPath).Length} bytes)");
return 0;

static bool TryParseVersion(string value, out OpenApiSpecVersion version)
{
    version = value switch
    {
        "3.0" => OpenApiSpecVersion.OpenApi3_0,
        "3.1" => OpenApiSpecVersion.OpenApi3_1,
        "3.2" => OpenApiSpecVersion.OpenApi3_2,
        _ => default,
    };
    return value is "3.0" or "3.1" or "3.2";
}

static bool TryParsePolicy(
    string? value,
    out OpenApiScalarFormatPolicy policy,
    out bool useCallback)
{
    useCallback = string.Equals(value, "Callback", StringComparison.OrdinalIgnoreCase);
    if (useCallback || value is null)
    {
        policy = OpenApiScalarFormatPolicy.Conventional;
        return true;
    }

    return Enum.TryParse(value, ignoreCase: true, out policy);
}

static string GetPolicySuffix(string? value)
    => value is null || string.Equals(value, "Conventional", StringComparison.OrdinalIgnoreCase)
        ? string.Empty
        : $"-{value.ToLowerInvariant()}";

public sealed class DirectionalDto
{
    private string? _inputOnly;
    private string? _nullableInput;

    public string Both { get; set; } = string.Empty;

    public string OutputOnly { get; } = string.Empty;

    public string? InputOnly
    {
        set => _inputOnly = value;
    }

    [AllowNull]
    public string NullableInput
    {
        get => _nullableInput ?? string.Empty;
        set => _nullableInput = value;
    }

    [JsonRequired]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RequiredButOmittable { get; set; }
}

public class Entity
{
    public long Id { get; set; }
}

public sealed class Customer : Entity
{
    public string Name { get; set; } = string.Empty;
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(Cat), "cat")]
[JsonDerivedType(typeof(Dog), "dog")]
public abstract class Animal
{
    public string Name { get; set; } = string.Empty;
}

public sealed class Cat : Animal
{
    public int Lives { get; set; }
}

public sealed class Dog : Animal
{
    public bool Good { get; set; }
}

public sealed class SerializerEnvelope
{
    public sbyte Small { get; set; }

    public decimal Amount { get; set; }

    public byte[] Data { get; set; } = [];

    public Guid Identifier { get; set; }

    public Uri? RelativeUri { get; set; }

    public string Name { get; set; } = string.Empty;

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Additional { get; set; } = [];
}
