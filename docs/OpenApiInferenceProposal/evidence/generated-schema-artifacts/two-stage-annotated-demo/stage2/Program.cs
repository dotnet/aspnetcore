// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Linq;
using AnnotatedSchemaDemo;
using BenchmarkDotNet.Running;
using Corvus.Text.Json.RuntimeEvaluator;
using GeneratedSchemaInspection;
using Json.Schema;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

if (args is ["--verify"])
{
    await VerifyAsync();
    Console.WriteLine("verified");
    return;
}

if (args is ["--cold-probe", var scenario])
{
    var before = GC.GetAllocatedBytesForCurrentThread();
    var started = System.Diagnostics.Stopwatch.GetTimestamp();
    var value = scenario switch
    {
        "same-pass" => GeneratedJsonSchemas.CanonicalArtifacts.FlagshipModel.BundleUtf8.Length,
        "native-exporter" => HistoricalExporter.CreateSupportedBundleBytes().Length,
        "image-load" => LoadImageNodeCount(),
        _ => throw new InvalidOperationException($"Unknown cold-probe scenario '{scenario}'."),
    };
    var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(started);
    var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    Console.WriteLine($"{scenario},{elapsed.TotalNanoseconds:F0},{allocated},{value}");
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

static int LoadImageNodeCount()
{
    using var evaluator = DemoEngineEquivalence.LoadCorvusProgramImage();
    return evaluator.NodeCount;
}

static async Task VerifyAsync()
{
    var bundle = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "flagship.bundle.json"));
    var compatibilitySchema = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "flagship.schema.json"));
    var manifest = JsonNode.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "flagship.manifest.json")))!;
    var graphIdentity = manifest["graphSha256"]!.GetValue<string>();
    VerifyIdentityChain(graphIdentity);

    var parsed = JsonSchema.FromText(
        Encoding.UTF8.GetString(compatibilitySchema),
        new BuildOptions { Dialect = Dialect.Draft202012, SchemaRegistry = new SchemaRegistry() });
    var nativeOptions = new EvaluationOptions
    {
        OutputFormat = OutputFormat.Flag,
        RequireFormatValidation = false,
    };
    using var compiled = JsonSchemaEvaluator.Compile(compatibilitySchema, DemoCorvusOptions.Create());
    using var image = DemoEngineEquivalence.LoadCorvusProgramImage();

    VerifyEngineEquivalence(parsed, nativeOptions, compiled, image);
    await VerifyAspNetBindingsAsync();
    await VerifyOpenApiProjectionAsync();

    var jsonSchemaNetInitializations = JsonSchemaNetNativeValidator.InitializationCount;
    var corvusInitializations = CorvusProgramImageValidator.InitializationCount;
    await VerifyHttpPipelineAsync();
    if (JsonSchemaNetNativeValidator.InitializationCount != jsonSchemaNetInitializations ||
        CorvusProgramImageValidator.InitializationCount != corvusInitializations)
    {
        throw new InvalidOperationException("An ASP.NET request reinitialized a validator or processed its schema.");
    }
}

static void VerifyIdentityChain(string graphIdentity)
{
    if (!string.Equals(
            graphIdentity,
            Microsoft.AspNetCore.OpenApi.Generated.FlagshipAnnotatedArtifact.SchemaIdentity,
            StringComparison.OrdinalIgnoreCase) ||
        !string.Equals(graphIdentity, JsonSchemaNetBinding.SchemaIdentity, StringComparison.OrdinalIgnoreCase) ||
        !string.Equals(graphIdentity, CorvusBinding.SchemaIdentity, StringComparison.OrdinalIgnoreCase) ||
        !string.Equals(graphIdentity, FlagshipCorvusProgramImage.SchemaGraphIdentity, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(JsonSchemaNetBinding.Identity, CorvusBinding.Identity, StringComparison.Ordinal) ||
        string.Equals(
            JsonSchemaNetNativeValidator.ConfigurationIdentity,
            CorvusProgramImageValidator.ConfigurationIdentity,
            StringComparison.Ordinal))
    {
        throw new InvalidOperationException("The shared artifact or engine-specific binding identity chain is invalid.");
    }
}

static void VerifyEngineEquivalence(
    JsonSchema parsed,
    EvaluationOptions nativeOptions,
    JsonSchemaEvaluator compiled,
    JsonSchemaEvaluator image)
{
    foreach (var payload in DemoCorpus.Valid.Concat(DemoCorpus.Invalid))
    {
        var expected = DemoCorpus.Valid.Contains(payload);
        var outcomes = new[]
        {
            EvaluateJsonSchemaNet(GeneratedJsonSchemas.FlagshipModel, payload, nativeOptions),
            EvaluateJsonSchemaNet(parsed, payload, nativeOptions),
            EvaluateCorvus(compiled, payload),
            EvaluateCorvus(image, payload),
        };
        if (outcomes.Any(outcome => outcome != expected))
        {
            throw new InvalidOperationException(
                $"Validation divergence for {Encoding.UTF8.GetString(payload)}: {string.Join(",", outcomes)}.");
        }
    }
}

static async Task VerifyAspNetBindingsAsync()
{
    foreach (var purpose in new[] { OpenApiSchemaEvidencePurpose.Input, OpenApiSchemaEvidencePurpose.Output })
    {
        foreach (var payload in DemoCorpus.Valid.Concat(DemoCorpus.Invalid))
        {
            var expected = DemoCorpus.Valid.Contains(payload);
            var jsonSchemaNet = await ValidateBindingAsync<JsonSchemaNetBinding>(payload, purpose);
            var corvus = await ValidateBindingAsync<CorvusBinding>(payload, purpose);
            if (jsonSchemaNet.IsValid != expected ||
                corvus.IsValid != expected ||
                GetDiagnosticClassification(jsonSchemaNet) != GetDiagnosticClassification(corvus))
            {
                throw new InvalidOperationException(
                    $"ASP.NET binding divergence for {purpose} {Encoding.UTF8.GetString(payload)}.");
            }
        }
    }
}

static ValueTask<OpenApiJsonSchemaValidationResult> ValidateBindingAsync<TBinding>(
    byte[] payload,
    OpenApiSchemaEvidencePurpose purpose)
    where TBinding : IOpenApiValidatedJsonSchemaBinding<TBinding>
    => TBinding.ValidateAsync(payload, purpose);

static string GetDiagnosticClassification(OpenApiJsonSchemaValidationResult result)
    => result.IsValid
        ? "valid"
        : string.Join(
            "|",
            result.Errors!.Select(static error =>
                $"{error.InstanceLocation}:{error.Keyword}:{error.Message}"));

static async Task VerifyOpenApiProjectionAsync()
{
    foreach (var version in new[]
    {
        OpenApiSpecVersion.OpenApi3_0,
        OpenApiSpecVersion.OpenApi3_1,
        OpenApiSpecVersion.OpenApi3_2,
    })
    {
        var nativeSchema = JsonSchemaNetBinding.CreateOpenApiSchema(version);
        var corvusSchema = CorvusBinding.CreateOpenApiSchema(version);
        if (nativeSchema.Metadata?["x-schema-validated-identity"] as string != JsonSchemaNetBinding.Identity ||
            corvusSchema.Metadata?["x-schema-validated-identity"] as string != CorvusBinding.Identity)
        {
            throw new InvalidOperationException($"OpenAPI {version} schema association lost its binding identity.");
        }
        var native = await SerializeSchemaAsync(nativeSchema, version);
        var corvus = await SerializeSchemaAsync(corvusSchema, version);
        if (!JsonNode.DeepEquals(native, corvus))
        {
            throw new InvalidOperationException($"OpenAPI {version} output differs between validator bindings.");
        }
    }
}

static bool EvaluateJsonSchemaNet(JsonSchema schema, byte[] payload, EvaluationOptions options)
{
    try
    {
        using var document = JsonDocument.Parse(payload);
        return schema.Evaluate(document.RootElement, options).IsValid;
    }

    catch (JsonException)
    {
        return false;
    }
}

static bool EvaluateCorvus(JsonSchemaEvaluator evaluator, byte[] payload)
{
    try
    {
        using var _ = JsonDocument.Parse(payload);
        return evaluator.Evaluate(payload);
    }
    catch (JsonException)
    {
        return false;
    }
}

static async Task VerifyHttpPipelineAsync()
{
    var builder = WebApplication.CreateSlimBuilder();
    builder.WebHost.UseUrls("http://127.0.0.1:0");
    builder.Services.ConfigureHttpJsonOptions(static options =>
        options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);
    builder.Services.AddControllers()
        .AddApplicationPart(typeof(DemoController).Assembly)
        .AddJsonOptions(static options =>
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);
    var app = builder.Build();

    MapMinimal<JsonSchemaNetBinding>(app, "/minimal/native");
    MapMinimal<CorvusBinding>(app, "/minimal/corvus");
    var controllers = app.MapControllers();
    ConfigureMvc<JsonSchemaNetBinding>(controllers, "native");
    ConfigureMvc<CorvusBinding>(controllers, "corvus");

    await app.StartAsync();
    try
    {
        var address = app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        foreach (var (jsonSchemaNetRoute, corvusRoute) in new[]
        {
            ("/minimal/native", "/minimal/corvus"),
            ("/mvc/native", "/mvc/corvus"),
        })
        {
            foreach (var payload in DemoCorpus.Valid)
            {
                var jsonSchemaNet = await PostJsonAsync(client, jsonSchemaNetRoute, payload);
                var corvus = await PostJsonAsync(client, corvusRoute, payload);
                if (jsonSchemaNet.StatusCode != HttpStatusCode.OK ||
                    corvus.StatusCode != HttpStatusCode.OK ||
                    !jsonSchemaNet.Body.AsSpan().SequenceEqual(corvus.Body))
                {
                    throw new InvalidOperationException(
                        $"{jsonSchemaNetRoute} and {corvusRoute} diverged for valid input.");
                }
            }

            foreach (var payload in DemoCorpus.Invalid)
            {
                var jsonSchemaNet = await PostJsonAsync(client, jsonSchemaNetRoute, payload);
                var corvus = await PostJsonAsync(client, corvusRoute, payload);
                if (jsonSchemaNet.StatusCode != HttpStatusCode.BadRequest ||
                    corvus.StatusCode != HttpStatusCode.BadRequest ||
                    GetValidationProblemClassification(jsonSchemaNet.Body) !=
                        GetValidationProblemClassification(corvus.Body))
                {
                    throw new InvalidOperationException(
                        $"{jsonSchemaNetRoute} and {corvusRoute} diverged for invalid input.");
                }
            }
        }

        foreach (var (jsonSchemaNetRoute, corvusRoute) in new[]
        {
            ("/minimal/native/invalid-response", "/minimal/corvus/invalid-response"),
            ("/mvc/native/invalid-response", "/mvc/corvus/invalid-response"),
        })
        {
            using var jsonSchemaNet = await client.GetAsync(jsonSchemaNetRoute);
            using var corvus = await client.GetAsync(corvusRoute);
            if (jsonSchemaNet.StatusCode != HttpStatusCode.InternalServerError ||
                corvus.StatusCode != HttpStatusCode.InternalServerError ||
                (await jsonSchemaNet.Content.ReadAsByteArrayAsync()).Length != 0 ||
                (await corvus.Content.ReadAsByteArrayAsync()).Length != 0)
            {
                throw new InvalidOperationException(
                    $"{jsonSchemaNetRoute} and {corvusRoute} diverged for invalid response suppression.");
            }
        }

        var oversizedPayload = Encoding.UTF8.GetBytes(
            """{"kind":"personal","address":{"street":"1 High Street","city":"London"},"padding":"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"}""");
        foreach (var (jsonSchemaNetRoute, corvusRoute) in new[]
        {
            ("/minimal/native/limited", "/minimal/corvus/limited"),
            ("/mvc/native/limited", "/mvc/corvus/limited"),
        })
        {
            var jsonSchemaNet = await PostJsonAsync(client, jsonSchemaNetRoute, oversizedPayload);
            var corvus = await PostJsonAsync(client, corvusRoute, oversizedPayload);
            if (jsonSchemaNet.StatusCode != HttpStatusCode.RequestEntityTooLarge ||
                corvus.StatusCode != HttpStatusCode.RequestEntityTooLarge ||
                GetProblemClassification(jsonSchemaNet.Body) != GetProblemClassification(corvus.Body))
            {
                throw new InvalidOperationException(
                    $"{jsonSchemaNetRoute} and {corvusRoute} diverged for request limits.");
            }
        }
    }
    finally
    {
        await app.StopAsync();
        await app.DisposeAsync();
    }
}

static async Task<(HttpStatusCode StatusCode, byte[] Body)> PostJsonAsync(
    HttpClient client,
    string route,
    byte[] payload)
{
    using var content = new ByteArrayContent(payload);
    content.Headers.ContentType = new("application/json");
    using var response = await client.PostAsync(route, content);
    return (response.StatusCode, await response.Content.ReadAsByteArrayAsync());
}

static string GetValidationProblemClassification(byte[] body)
{
    var problem = JsonNode.Parse(body)!.AsObject();
    return problem["errors"]!.ToJsonString();
}

static string GetProblemClassification(byte[] body)
{
    var problem = JsonNode.Parse(body)!.AsObject();
    return $"{problem["status"]}:{problem["title"]}:{problem["detail"]}";
}

static void MapMinimal<TBinding>(WebApplication app, string route)
    where TBinding : IOpenApiValidatedJsonSchemaBinding<TBinding>
{
    app.MapPost(route, static (FlagshipModel model) => Results.Json(model))
        .WithValidatedJsonSchema<TBinding>(OpenApiSchemaEvidencePurpose.Input)
        .WithValidatedJsonSchema<TBinding>(OpenApiSchemaEvidencePurpose.Output);
    app.MapGet($"{route}/invalid-response", static () => Results.Json(new
        {
            kind = "business",
            address = new { street = "1 High Street", city = "London" },
        }))
        .WithValidatedJsonSchema<TBinding>(OpenApiSchemaEvidencePurpose.Output);
    app.MapPost($"{route}/limited", static (FlagshipModel model) => Results.Json(model))
        .WithValidatedJsonSchema<TBinding>(
            OpenApiSchemaEvidencePurpose.Input,
            new OpenApiValidatedJsonSchemaOptions { MaxPayloadSize = 128 });
}

static void ConfigureMvc<TBinding>(IEndpointConventionBuilder controllers, string engine)
    where TBinding : IOpenApiValidatedJsonSchemaBinding<TBinding>
{
    controllers
        .WithValidatedJsonSchema<TBinding>(
            OpenApiSchemaEvidencePurpose.Input,
            action => string.Equals(action.AttributeRouteInfo?.Template, $"mvc/{engine}", StringComparison.Ordinal))
        .WithValidatedJsonSchema<TBinding>(
            OpenApiSchemaEvidencePurpose.Output,
            action => string.Equals(action.AttributeRouteInfo?.Template, $"mvc/{engine}", StringComparison.Ordinal))
        .WithValidatedJsonSchema<TBinding>(
            OpenApiSchemaEvidencePurpose.Output,
            action => string.Equals(
                action.AttributeRouteInfo?.Template,
                $"mvc/{engine}/invalid-response",
                StringComparison.Ordinal))
        .WithValidatedJsonSchema<TBinding>(
            OpenApiSchemaEvidencePurpose.Input,
            action => string.Equals(action.AttributeRouteInfo?.Template, $"mvc/{engine}/limited", StringComparison.Ordinal),
            new OpenApiValidatedJsonSchemaOptions { MaxPayloadSize = 128 });
}

static async Task<JsonObject> SerializeSchemaAsync(OpenApiSchema schema, OpenApiSpecVersion version)
{
    var document = new OpenApiDocument
    {
        Info = new() { Title = "Annotated model", Version = "1" },
        Paths = new(),
        Components = new()
        {
            Schemas = new Dictionary<string, IOpenApiSchema> { ["Flagship"] = schema },
        },
    };
    return JsonNode.Parse(await document.SerializeAsJsonAsync(version))!["components"]!["schemas"]!["Flagship"]!.AsObject();
}

[ApiController]
public sealed class DemoController : ControllerBase
{
    [HttpPost("/mvc/native")]
    public ActionResult<FlagshipModel> Native(FlagshipModel model) => model;

    [HttpPost("/mvc/corvus")]
    public ActionResult<FlagshipModel> Corvus(FlagshipModel model) => model;

    [HttpGet("/mvc/native/invalid-response")]
    public object NativeInvalidResponse()
        => new
        {
            kind = "business",
            address = new { street = "1 High Street", city = "London" },
        };

    [HttpGet("/mvc/corvus/invalid-response")]
    public object CorvusInvalidResponse()
        => new
        {
            kind = "business",
            address = new { street = "1 High Street", city = "London" },
        };

    [HttpPost("/mvc/native/limited")]
    public ActionResult<FlagshipModel> NativeLimited(FlagshipModel model) => model;

    [HttpPost("/mvc/corvus/limited")]
    public ActionResult<FlagshipModel> CorvusLimited(FlagshipModel model) => model;
}
