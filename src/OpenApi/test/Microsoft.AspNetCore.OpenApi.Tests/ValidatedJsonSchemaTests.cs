// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable ASP0040

public class ValidatedJsonSchemaTests
{
    private static readonly byte[] Schema = """
        {
          "$schema": "https://json-schema.org/draft/2020-12/schema",
          "$defs": {
            "node": {
              "type": "object",
              "properties": {
                "value": { "type": "integer", "minimum": 1 },
                "next": { "$ref": "#/$defs/node" }
              },
              "required": [ "value" ],
              "additionalProperties": false
            }
          },
          "allOf": [ { "$ref": "#/$defs/node" } ],
          "if": { "required": [ "next" ] },
          "then": { "dependentRequired": { "next": [ "value" ] } },
          "else": { "not": { "required": [ "next" ] } },
          "prefixItems": [
            { "type": "integer" },
            { "type": "string", "pattern": "^[a-z]+$" }
          ],
          "items": false,
          "minItems": 2,
          "maxItems": 2
        }
        """u8.ToArray();

    [Fact]
    public async Task Import_PreservesTypedPrefixItemsWithoutUnrecognizedKeywordWrapper()
    {
        var registration = CreateRegistration(OpenApiSchemaEvidencePurpose.Input, static _ => true);

        var schema = OpenApiValidatedJsonSchemaImporter.Import(
            registration.Evidence,
            OpenApiSpecVersion.OpenApi3_1);
        var root = await SerializeSchemaAsync(schema, OpenApiSpecVersion.OpenApi3_1);

        Assert.Equal(2, root["prefixItems"]!.AsArray().Count);
        Assert.Null(root["unrecognizedKeywords"]);
        Assert.Equal(registration.Evidence.Identity, schema.Metadata![Microsoft.AspNetCore.OpenApi.OpenApiConstants.SchemaValidatedIdentity]);
    }

    [Fact]
    public async Task Import_OpenApi30WidensRecursiveSchema()
    {
        var registration = CreateRegistration(OpenApiSchemaEvidencePurpose.Input, static _ => true);

        var schema = OpenApiValidatedJsonSchemaImporter.Import(
            registration.Evidence,
            OpenApiSpecVersion.OpenApi3_0);
        var root = await SerializeSchemaAsync(schema, OpenApiSpecVersion.OpenApi3_0);

        Assert.Empty(root);
        Assert.Equal(registration.Evidence.Identity, schema.Metadata![Microsoft.AspNetCore.OpenApi.OpenApiConstants.SchemaValidatedIdentity]);
    }

    [Fact]
    public async Task Executor_ValidatesRequestBeforeInvokingEndpointAndRewindsBody()
    {
        var registration = CreateRegistration(
            OpenApiSchemaEvidencePurpose.Input,
            static payload => payload.Span.SequenceEqual("""{"value":1}"""u8));
        var context = CreateContext(registration);
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream("""{"value":1}"""u8.ToArray());
        var invoked = false;

        await OpenApiValidatedJsonSchemaEndpointExecutor.ExecuteAsync(context, async httpContext =>
        {
            invoked = true;
            using var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8);
            Assert.Equal("""{"value":1}""", await reader.ReadToEndAsync());
        });

        Assert.True(invoked);
    }

    [Fact]
    public async Task Executor_InvalidRequestReturnsValidationProblem()
    {
        var registration = CreateRegistration(OpenApiSchemaEvidencePurpose.Input, static _ => false);
        var context = CreateContext(registration);
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream("""{"value":0}"""u8.ToArray());

        await OpenApiValidatedJsonSchemaEndpointExecutor.ExecuteAsync(
            context,
            _ => throw new InvalidOperationException("The endpoint must not run."));

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Contains("must be valid", Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray()));
    }

    [Fact]
    public async Task Executor_InvalidResponseSuppressesPayloadAndReturns500()
    {
        var registration = CreateRegistration(OpenApiSchemaEvidencePurpose.Output, static _ => false);
        var context = CreateContext(registration);

        await OpenApiValidatedJsonSchemaEndpointExecutor.ExecuteAsync(context, async httpContext =>
        {
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync("""{"value":0}""");
        });

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Empty(((MemoryStream)context.Response.Body).ToArray());
    }

    [Fact]
    public async Task Executor_ValidResponseCopiesPayloadUnchanged()
    {
        var registration = CreateRegistration(OpenApiSchemaEvidencePurpose.Output, static _ => true);
        var context = CreateContext(registration);

        await OpenApiValidatedJsonSchemaEndpointExecutor.ExecuteAsync(context, async httpContext =>
        {
            httpContext.Response.ContentType = "application/json; charset=utf-8";
            await httpContext.Response.WriteAsync("""{"value":1}""");
        });

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("""{"value":1}""", Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray()));
    }

    [Fact]
    public async Task Executor_ValidResponseWrittenThroughBodyWriterCopiesPayloadUnchanged()
    {
        var registration = CreateRegistration(OpenApiSchemaEvidencePurpose.Output, static _ => true);
        var context = CreateContext(registration);

        await OpenApiValidatedJsonSchemaEndpointExecutor.ExecuteAsync(context, async httpContext =>
        {
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.BodyWriter.WriteAsync("""{"value":1}"""u8.ToArray());
        });

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("""{"value":1}""", Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray()));
    }

    [Fact]
    public async Task Executor_RequestPayloadOverLimitReturns413()
    {
        var registration = CreateRegistration(
            OpenApiSchemaEvidencePurpose.Input,
            static _ => true,
            new() { MaxPayloadSize = 4 });
        var context = CreateContext(registration);
        context.Request.ContentType = "application/json";
        context.Request.ContentLength = 11;
        context.Request.Body = new MemoryStream("""{"value":1}"""u8.ToArray());

        await OpenApiValidatedJsonSchemaEndpointExecutor.ExecuteAsync(
            context,
            _ => throw new InvalidOperationException("The endpoint must not run."));

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, context.Response.StatusCode);
    }

    [Fact]
    public async Task Executor_ResponsePayloadOverLimitIsSuppressed()
    {
        var registration = CreateRegistration(
            OpenApiSchemaEvidencePurpose.Output,
            static _ => true,
            new() { MaxPayloadSize = 4 });
        var context = CreateContext(registration);

        await OpenApiValidatedJsonSchemaEndpointExecutor.ExecuteAsync(context, async httpContext =>
        {
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync("""{"value":1}""");
        });

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Empty(((MemoryStream)context.Response.Body).ToArray());
    }

    [Fact]
    public void Registration_RejectsUnresolvedLocalReferencesBeforeCompilingValidator()
    {
        var schema = """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "$ref": "#/$defs/missing"
            }
            """u8.ToArray();

        Assert.Throws<ArgumentException>(() => new OpenApiValidatedJsonSchemaRegistration(
            typeof(object),
            OpenApiSchemaEvidencePurpose.Input,
            schema,
            OpenApiJsonSchemaDialect.Draft202012,
            OpenApiJsonSchemaValidationCapabilities.None,
            new ValidatorFactory(static _ => true)));
    }

    [Fact]
    public async Task EndpointConvention_WrapsRequestDelegateOutsideBodyBinding()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .AddRouting()
            .BuildServiceProvider();
        var endpoints = new DefaultEndpointRouteBuilder(new ApplicationBuilder(services));
        endpoints.MapPost("/", (ValidatedNode node) => Results.Json(new { value = node.Value }))
            .WithValidatedJsonSchema(CreateRegistration(
                OpenApiSchemaEvidencePurpose.Input,
                IsValueOne))
            .WithValidatedJsonSchema(CreateRegistration(
                OpenApiSchemaEvidencePurpose.Output,
                IsValueOne));
        var endpoint = Assert.IsType<RouteEndpoint>(endpoints.DataSources.Single().Endpoints.Single());
        var context = new DefaultHttpContext
        {
            RequestServices = services,
        };
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = "application/json";
        context.Request.ContentLength = 11;
        context.Request.Body = new MemoryStream("""{"value":1}"""u8.ToArray());
        context.Request.Body.Position = 0;
        context.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature());
        context.Response.Body = new MemoryStream();
        context.SetEndpoint(endpoint);

        await endpoint.RequestDelegate!(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.True(IsValueOne(((MemoryStream)context.Response.Body).ToArray()));
    }

    private static DefaultHttpContext CreateContext(OpenApiValidatedJsonSchemaRegistration registration)
    {
        var services = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = services,
        };
        context.Response.Body = new MemoryStream();
        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(registration),
            "validated"));
        return context;
    }

    private static async Task<JsonObject> SerializeSchemaAsync(OpenApiSchema schema, OpenApiSpecVersion version)
    {
        var document = new OpenApiDocument
        {
            Info = new() { Title = "Validated schema", Version = "1" },
            Paths = new(),
            Components = new()
            {
                Schemas = new Dictionary<string, IOpenApiSchema>
                {
                    ["Validated"] = schema,
                },
            },
        };
        var json = JsonNode.Parse(await document.SerializeAsJsonAsync(version))!;
        return json["components"]!["schemas"]!["Validated"]!.AsObject();
    }

    private static bool IsValueOne(ReadOnlyMemory<byte> payload)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        return (root.TryGetProperty("value", out var value) || root.TryGetProperty("Value", out value)) &&
            value.GetInt32() == 1;
    }

    private static OpenApiValidatedJsonSchemaRegistration CreateRegistration(
        OpenApiSchemaEvidencePurpose purpose,
        Func<ReadOnlyMemory<byte>, bool> validate,
        OpenApiValidatedJsonSchemaOptions options = null)
        => new(
            typeof(object),
            purpose,
            Schema,
            OpenApiJsonSchemaDialect.Draft202012,
            OpenApiJsonSchemaValidationCapabilities.FormatAssertions,
            new ValidatorFactory(validate),
            options);

    private sealed class ValidatorFactory(Func<ReadOnlyMemory<byte>, bool> validate) : IOpenApiJsonSchemaValidatorFactory
    {
        public IOpenApiJsonSchemaValidator CreateValidator(OpenApiValidatedJsonSchemaEvidence evidence)
            => new Validator(validate);
    }

    private sealed class Validator(Func<ReadOnlyMemory<byte>, bool> validate) : IOpenApiJsonSchemaValidator
    {
        public ValueTask<OpenApiJsonSchemaValidationResult> ValidateAsync(
            ReadOnlyMemory<byte> utf8Json,
            OpenApiJsonSchemaValidationContext context,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(validate(utf8Json)
                ? OpenApiJsonSchemaValidationResult.Valid
                : new OpenApiJsonSchemaValidationResult(
                [
                    new("/value", "minimum", "The value must be valid."),
                ]));
    }

    public sealed class ValidatedNode
    {
        public int Value { get; set; }
    }

    private sealed class RequestBodyDetectionFeature : IHttpRequestBodyDetectionFeature
    {
        public bool CanHaveBody => true;
    }
}
