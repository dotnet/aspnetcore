// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
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

    public static TheoryData<OpenApiJsonSchemaDialect, string> DialectSchemas => new()
    {
        {
            OpenApiJsonSchemaDialect.Draft4,
            """
            {
              "$schema": "http://json-schema.org/draft-04/schema#",
              "id": "urn:example:tuple",
              "definitions": {
                "item": { "type": "number", "minimum": 1, "exclusiveMinimum": true }
              },
              "type": "array",
              "items": [ { "type": "number", "minimum": 1, "exclusiveMinimum": true } ],
              "additionalItems": false,
              "dependencies": {
                "a": [ "b" ],
                "c": { "required": [ "d" ] }
              }
            }
            """
        },
        {
            OpenApiJsonSchemaDialect.Draft6,
            """
            {
              "$schema": "http://json-schema.org/draft-06/schema#",
              "$id": "urn:example:tuple",
              "definitions": {
                "item": { "type": "number", "exclusiveMinimum": 1 }
              },
              "type": "array",
              "items": [ { "type": "number", "exclusiveMinimum": 1 } ],
              "additionalItems": false,
              "dependencies": {
                "a": [ "b" ],
                "c": { "required": [ "d" ] }
              }
            }
            """
        },
        {
            OpenApiJsonSchemaDialect.Draft7,
            """
            {
              "$schema": "http://json-schema.org/draft-07/schema#",
              "$id": "urn:example:tuple",
              "definitions": {
                "item": { "type": "number", "exclusiveMinimum": 1 }
              },
              "type": "array",
              "items": [ { "type": "number", "exclusiveMinimum": 1 } ],
              "additionalItems": false,
              "dependencies": {
                "a": [ "b" ],
                "c": { "required": [ "d" ] }
              }
            }
            """
        },
        {
            OpenApiJsonSchemaDialect.Draft201909,
            """
            {
              "$schema": "https://json-schema.org/draft/2019-09/schema",
              "$id": "urn:example:tuple",
              "$defs": {
                "item": { "type": "number", "exclusiveMinimum": 1 }
              },
              "type": "array",
              "items": [ { "type": "number", "exclusiveMinimum": 1 } ],
              "additionalItems": false,
              "dependentRequired": { "a": [ "b" ] },
              "dependentSchemas": { "c": { "required": [ "d" ] } }
            }
            """
        },
        {
            OpenApiJsonSchemaDialect.Draft202012,
            """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "$id": "urn:example:tuple",
              "$defs": {
                "item": { "type": "number", "exclusiveMinimum": 1 }
              },
              "type": "array",
              "prefixItems": [ { "type": "number", "exclusiveMinimum": 1 } ],
              "items": false,
              "dependentRequired": { "a": [ "b" ] },
              "dependentSchemas": { "c": { "required": [ "d" ] } }
            }
            """
        },
    };

    [Theory]
    [MemberData(nameof(DialectSchemas))]
    public async Task Import_NormalizesSupportedDialectsToEquivalentOpenApi31(
        OpenApiJsonSchemaDialect dialect,
        string source)
    {
        var registration = CreateRegistration(dialect, source);
        var normalized = registration.Evidence.NormalizedSchema;

        Assert.Equal("https://json-schema.org/draft/2020-12/schema", normalized.GetProperty("$schema").GetString());
        Assert.Equal("urn:example:tuple", normalized.GetProperty("$id").GetString());
        Assert.Equal(1, normalized.GetProperty("$defs").GetProperty("item").GetProperty("exclusiveMinimum").GetInt32());
        Assert.Equal(1, normalized.GetProperty("prefixItems")[0].GetProperty("exclusiveMinimum").GetInt32());
        Assert.False(normalized.GetProperty("items").GetBoolean());
        Assert.Equal("b", normalized.GetProperty("dependentRequired").GetProperty("a")[0].GetString());
        Assert.True(normalized.GetProperty("dependentSchemas").TryGetProperty("c", out _));

        foreach (var version in new[] { OpenApiSpecVersion.OpenApi3_1, OpenApiSpecVersion.OpenApi3_2 })
        {
            var imported = OpenApiValidatedJsonSchemaImporter.Import(registration.Evidence, version);
            var emitted = await SerializeSchemaAsync(imported, version);
            Assert.Single(emitted["prefixItems"]!.AsArray());
            Assert.Null(emitted["unrecognizedKeywords"]);
        }
    }

    [Fact]
    public void Registration_UsesDialectCapabilitiesAndValidatorConfigurationInIdentity()
    {
        const string schema = """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "type": "string"
            }
            """;
        var first = CreateRegistration(
            OpenApiJsonSchemaDialect.Draft202012,
            schema,
            OpenApiJsonSchemaValidationCapabilities.None,
            "configuration-a");
        var format = CreateRegistration(
            OpenApiJsonSchemaDialect.Draft202012,
            schema,
            OpenApiJsonSchemaValidationCapabilities.FormatAssertions,
            "configuration-a");
        var configured = CreateRegistration(
            OpenApiJsonSchemaDialect.Draft202012,
            schema,
            OpenApiJsonSchemaValidationCapabilities.None,
            "configuration-b");

        Assert.Equal(first.Evidence.SchemaIdentity, format.Evidence.SchemaIdentity);
        Assert.NotEqual(first.Evidence.Identity, format.Evidence.Identity);
        Assert.NotEqual(first.Evidence.Identity, configured.Evidence.Identity);
    }

    [Fact]
    public void Registration_RejectsDialectMismatchAndUnsupportedFactoryBeforeCompilation()
    {
        var mismatch = """
            {
              "$schema": "http://json-schema.org/draft-07/schema#",
              "type": "string"
            }
            """u8.ToArray();
        Assert.Throws<ArgumentException>(() => new OpenApiValidatedJsonSchemaRegistration(
            typeof(string),
            OpenApiSchemaEvidencePurpose.Input,
            mismatch,
            OpenApiJsonSchemaDialect.Draft4,
            OpenApiJsonSchemaValidationCapabilities.None,
            new ValidatorFactory(static _ => true)));

        var unsupported = new ValidatorFactory(
            static _ => true,
            supportsDialect: static dialect => dialect != OpenApiJsonSchemaDialect.Draft4);
        Assert.Throws<NotSupportedException>(() => new OpenApiValidatedJsonSchemaRegistration(
            typeof(string),
            OpenApiSchemaEvidencePurpose.Input,
            """
            {
              "$schema": "http://json-schema.org/draft-04/schema#",
              "type": "string"
            }
            """u8.ToArray(),
            OpenApiJsonSchemaDialect.Draft4,
            OpenApiJsonSchemaValidationCapabilities.None,
            unsupported));
    }

    [Theory]
    [InlineData(OpenApiJsonSchemaDialect.Draft201909, "$recursiveRef")]
    [InlineData(OpenApiJsonSchemaDialect.Draft201909, "$recursiveAnchor")]
    [InlineData(OpenApiJsonSchemaDialect.Draft202012, "$dynamicRef")]
    [InlineData(OpenApiJsonSchemaDialect.Draft202012, "$dynamicAnchor")]
    public void Registration_RejectsUnsupportedRecursiveAndDynamicKeywords(
        OpenApiJsonSchemaDialect dialect,
        string keyword)
    {
        var value = keyword.EndsWith("Anchor", StringComparison.Ordinal) ? "true" : "\"#\"";
        var schema = $$"""
            {
              "$schema": "{{OpenApiValidatedJsonSchemaNormalizer.GetDialectUri(dialect)}}",
              "{{keyword}}": {{value}}
            }
            """;

        Assert.Throws<ArgumentException>(() => CreateRegistration(dialect, schema));
    }

    [Fact]
    public void Registration_UsesDraftSpecificRefSiblingAndBooleanSchemaSemantics()
    {
        var draft7 = CreateRegistration(
            OpenApiJsonSchemaDialect.Draft7,
            """
            {
              "$schema": "http://json-schema.org/draft-07/schema#",
              "definitions": { "value": { "type": "string" } },
              "$ref": "#/definitions/value",
              "type": "integer"
            }
            """);
        Assert.False(draft7.Evidence.NormalizedSchema.TryGetProperty("type", out _));
        Assert.Equal(
            "#/$defs/value",
            draft7.Evidence.NormalizedSchema.GetProperty("allOf")[0].GetProperty("$ref").GetString());
        Assert.True(draft7.Evidence.NormalizedSchema.TryGetProperty("$defs", out _));

        var draft202012 = CreateRegistration(
            OpenApiJsonSchemaDialect.Draft202012,
            """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "$defs": { "value": { "type": "string" } },
              "$ref": "#/$defs/value",
              "type": "integer"
            }
            """);
        Assert.Equal("integer", draft202012.Evidence.NormalizedSchema.GetProperty("type").GetString());

        Assert.Throws<ArgumentException>(() => CreateRegistration(OpenApiJsonSchemaDialect.Draft4, "true"));
        Assert.Equal(JsonValueKind.True, CreateRegistration(OpenApiJsonSchemaDialect.Draft6, "true").Evidence.NormalizedSchema.ValueKind);
    }

    [Fact]
    public void Registration_DoesNotInterpretReferencesInAnnotationData()
    {
        var registration = CreateRegistration(
            OpenApiJsonSchemaDialect.Draft202012,
            """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "const": { "$ref": "https://example.com/not-a-schema-reference" },
              "examples": [ { "$ref": "#/not-a-schema-reference" } ]
            }
            """);

        Assert.Equal(
            "https://example.com/not-a-schema-reference",
            registration.Evidence.NormalizedSchema.GetProperty("const").GetProperty("$ref").GetString());
    }

    [Theory]
    [InlineData(
        OpenApiJsonSchemaDialect.Draft4,
        """
        {
          "$schema": "http://json-schema.org/draft-04/schema#",
          "prefixItems": []
        }
        """)]
    [InlineData(
        OpenApiJsonSchemaDialect.Draft202012,
        """
        {
          "$schema": "https://json-schema.org/draft/2020-12/schema",
          "type": 42
        }
        """)]
    [InlineData(
        OpenApiJsonSchemaDialect.Draft202012,
        """
        {
          "$schema": "https://json-schema.org/draft/2020-12/schema",
          "required": [ 42 ]
        }
        """)]
    [InlineData(
        OpenApiJsonSchemaDialect.Draft202012,
        """
        {
          "$schema": "https://json-schema.org/draft/2020-12/schema",
          "$vocabulary": { "https://example.com/custom": true }
        }
        """)]
    [InlineData(
        OpenApiJsonSchemaDialect.Draft202012,
        """
        {
          "$schema": "https://json-schema.org/draft/2020-12/schema",
          "$ref": "https://example.com/external"
        }
        """)]
    public void Registration_RejectsDialectIncompatibleOrMalformedSchemas(
        OpenApiJsonSchemaDialect dialect,
        string source)
    {
        Assert.Throws<ArgumentException>(() => CreateRegistration(dialect, source));
    }

    [Theory]
    [MemberData(nameof(DialectSchemas))]
    public async Task Import_OpenApi30ConservativelyWidensNormalizedTupleWithReferences(
        OpenApiJsonSchemaDialect dialect,
        string source)
    {
        var registration = CreateRegistration(dialect, source);
        var imported = OpenApiValidatedJsonSchemaImporter.Import(
            registration.Evidence,
            OpenApiSpecVersion.OpenApi3_0);
        Assert.Empty(await SerializeSchemaAsync(imported, OpenApiSpecVersion.OpenApi3_0));
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

    [Fact]
    public async Task GeneratedBinding_UsesSameEndpointExecutionSemantics()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .AddRouting()
            .BuildServiceProvider();
        var endpoints = new DefaultEndpointRouteBuilder(new ApplicationBuilder(services));
        endpoints.MapPost("/", (ValidatedNode node) => Results.Json(new { value = node.Value }))
            .WithValidatedJsonSchema<GeneratedBinding>(OpenApiSchemaEvidencePurpose.Input)
            .WithValidatedJsonSchema<GeneratedBinding>(OpenApiSchemaEvidencePurpose.Output);
        var endpoint = Assert.IsType<RouteEndpoint>(endpoints.DataSources.Single().Endpoints.Single());
        var context = new DefaultHttpContext
        {
            RequestServices = services,
        };
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = "application/json";
        context.Request.ContentLength = 11;
        context.Request.Body = new MemoryStream("""{"value":1}"""u8.ToArray());
        context.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature());
        context.Response.Body = new MemoryStream();
        context.SetEndpoint(endpoint);

        await endpoint.RequestDelegate!(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.True(IsValueOne(((MemoryStream)context.Response.Body).ToArray()));
    }

    [Fact]
    public void GeneratedBinding_CreatesIsolatedOpenApiSchemas()
    {
        var first = GeneratedBinding.CreateOpenApiSchema(OpenApiSpecVersion.OpenApi3_2);
        var second = GeneratedBinding.CreateOpenApiSchema(OpenApiSpecVersion.OpenApi3_2);

        Assert.NotSame(first, second);
        first.Title = "changed";
        Assert.Null(second.Title);
    }

    [Fact]
    public async Task MvcConvention_InvalidRequestStopsBeforeInputFormattingAndReturns400()
    {
        await using var app = await CreateMvcApplicationAsync((endpoints, state) =>
        {
            endpoints.WithValidatedJsonSchema(
                CreateRegistration(OpenApiSchemaEvidencePurpose.Input, static _ => false),
                static action => action.ActionName == nameof(ValidatedSchemaController.Echo));
        });
        var state = app.Services.GetRequiredService<MvcValidationState>();

        var response = await app.GetTestClient().PostAsync(
            "/mvc/echo",
            new StringContent("""{"value":1}""", Encoding.UTF8, "application/json"));

        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
        Assert.Equal(0, state.InputFormatterReads);
        Assert.Equal(0, state.ActionInvocations);
    }

    [Fact]
    public async Task GeneratedBinding_MvcConventionUsesActionPredicate()
    {
        await using var app = await CreateMvcApplicationAsync((endpoints, state) =>
        {
            endpoints.WithValidatedJsonSchema<GeneratedBinding>(
                OpenApiSchemaEvidencePurpose.Input,
                static action => action.ActionName == nameof(ValidatedSchemaController.Echo));
        });
        var client = app.GetTestClient();

        var invalidResponse = await client.PostAsync(
            "/mvc/echo",
            new StringContent("""{"value":2}""", Encoding.UTF8, "application/json"));
        var validResponse = await client.PostAsync(
            "/mvc/echo-two",
            new StringContent("""{"value":2}""", Encoding.UTF8, "application/json"));

        Assert.Equal(StatusCodes.Status400BadRequest, (int)invalidResponse.StatusCode);
        Assert.Equal(StatusCodes.Status200OK, (int)validResponse.StatusCode);
    }

    [Fact]
    public async Task MvcConvention_ValidRequestIsRewoundForInputFormatter()
    {
        await using var app = await CreateMvcApplicationAsync((endpoints, state) =>
        {
            endpoints.WithValidatedJsonSchema(
                CreateRegistration(OpenApiSchemaEvidencePurpose.Input, IsValueOne),
                static action => action.ActionName == nameof(ValidatedSchemaController.Echo));
        });
        var state = app.Services.GetRequiredService<MvcValidationState>();

        var response = await app.GetTestClient().PostAsync(
            "/mvc/echo",
            new StringContent("""{"value":1}""", Encoding.UTF8, "application/json"));

        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
        Assert.True(IsValueOne(await response.Content.ReadAsByteArrayAsync()));
        Assert.Equal(1, state.InputFormatterReads);
        Assert.Equal(1, state.ActionInvocations);
    }

    [Fact]
    public async Task MvcConvention_ResultFilterAndOutputFormatterAreValidatedBeforeWriting()
    {
        await using var app = await CreateMvcApplicationAsync((endpoints, state) =>
        {
            endpoints.WithValidatedJsonSchema(
                CreateRegistration(
                    OpenApiSchemaEvidencePurpose.Output,
                    static payload => HasValue(payload, 2)),
                static action => action.ActionName == nameof(ValidatedSchemaController.Filtered));
        });
        var state = app.Services.GetRequiredService<MvcValidationState>();

        var response = await app.GetTestClient().GetAsync("/mvc/filtered");

        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
        Assert.True(HasValue(await response.Content.ReadAsByteArrayAsync(), 2));
        Assert.Equal(1, state.ResultFilterInvocations);
    }

    [Fact]
    public async Task MvcConvention_InvalidFormattedResponseIsSuppressedAndReturns500()
    {
        await using var app = await CreateMvcApplicationAsync((endpoints, state) =>
        {
            endpoints.WithValidatedJsonSchema(
                CreateRegistration(OpenApiSchemaEvidencePurpose.Output, static _ => false),
                static action => action.ActionName == nameof(ValidatedSchemaController.ResponsePayload));
        });

        var response = await app.GetTestClient().GetAsync("/mvc/response");

        Assert.Equal(StatusCodes.Status500InternalServerError, (int)response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        Assert.Equal(0, response.Content.Headers.ContentLength);
    }

    [Fact]
    public async Task MvcConvention_SelectsActualStatusCodeAndContentType()
    {
        await using var app = await CreateMvcApplicationAsync((endpoints, state) =>
        {
            endpoints.WithValidatedJsonSchema(
                CreateRegistration(
                    OpenApiSchemaEvidencePurpose.Output,
                    static _ => false,
                    new() { ResponseStatusCode = StatusCodes.Status200OK }),
                static action => action.ActionName == nameof(ValidatedSchemaController.CreatedPayload));
            endpoints.WithValidatedJsonSchema(
                CreateRegistration(
                    OpenApiSchemaEvidencePurpose.Output,
                    static payload => HasValue(payload, 1),
                    new()
                    {
                        ResponseStatusCode = StatusCodes.Status201Created,
                        ContentType = "application/json",
                    }),
                static action => action.ActionName == nameof(ValidatedSchemaController.CreatedPayload));
        });

        var response = await app.GetTestClient().GetAsync("/mvc/created");

        Assert.Equal(StatusCodes.Status201Created, (int)response.StatusCode);
        Assert.True(IsValueOne(await response.Content.ReadAsByteArrayAsync()));
    }

    [Fact]
    public async Task MvcConvention_SelectsActualResponseContentType()
    {
        await using var app = await CreateMvcApplicationAsync((endpoints, state) =>
        {
            endpoints.WithValidatedJsonSchema(
                CreateRegistration(
                    OpenApiSchemaEvidencePurpose.Output,
                    static _ => false,
                    new()
                    {
                        ResponseStatusCode = StatusCodes.Status422UnprocessableEntity,
                        ContentType = "application/json",
                    }),
                static action => action.ActionName == nameof(ValidatedSchemaController.ProblemPayload));
            endpoints.WithValidatedJsonSchema(
                CreateRegistration(
                    OpenApiSchemaEvidencePurpose.Output,
                    static payload => IsValueOne(payload),
                    new()
                    {
                        ResponseStatusCode = StatusCodes.Status422UnprocessableEntity,
                        ContentType = "application/problem+json",
                    }),
                static action => action.ActionName == nameof(ValidatedSchemaController.ProblemPayload));
        });

        var response = await app.GetTestClient().GetAsync("/mvc/problem");

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.True(IsValueOne(await response.Content.ReadAsByteArrayAsync()));
    }

    [Fact]
    public async Task MvcConvention_AllowsConfiguredEmptyOptionalBody()
    {
        await using var app = await CreateMvcApplicationAsync((endpoints, state) =>
        {
            endpoints.WithValidatedJsonSchema(
                CreateRegistration(
                    OpenApiSchemaEvidencePurpose.Input,
                    static _ => false,
                    new() { AllowEmptyRequestBody = true }),
                static action => action.ActionName == nameof(ValidatedSchemaController.Optional));
        });
        var state = app.Services.GetRequiredService<MvcValidationState>();

        var response = await app.GetTestClient().PostAsync("/mvc/optional", content: null);

        Assert.Equal(StatusCodes.Status204NoContent, (int)response.StatusCode);
        Assert.Equal(1, state.ActionInvocations);
    }

    [Fact]
    public async Task MvcConvention_PropagatesRequestCancellationAndRemainsReusable()
    {
        await using var app = await CreateMvcApplicationAsync((endpoints, state) =>
        {
            endpoints.WithValidatedJsonSchema(
                CreateRegistration(OpenApiSchemaEvidencePurpose.Input, IsValueOne),
                static action => action.ActionName == nameof(ValidatedSchemaController.Cancel));
            endpoints.WithValidatedJsonSchema(
                CreateRegistration(OpenApiSchemaEvidencePurpose.Input, IsValueOne),
                static action => action.ActionName == nameof(ValidatedSchemaController.Echo));
        });
        var client = app.GetTestClient();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.PostAsync(
            "/mvc/cancel",
            new StringContent("""{"value":1}""", Encoding.UTF8, "application/json"),
            cancellation.Token));
        var response = await client.PostAsync(
            "/mvc/echo",
            new StringContent("""{"value":1}""", Encoding.UTF8, "application/json"));

        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task MvcConvention_SameTypeCanUseDifferentSchemasPerAction()
    {
        await using var app = await CreateMvcApplicationAsync((endpoints, state) =>
        {
            endpoints.WithValidatedJsonSchema(
                CreateRegistration(OpenApiSchemaEvidencePurpose.Input, IsValueOne),
                static action => action.ActionName == nameof(ValidatedSchemaController.Echo));
            endpoints.WithValidatedJsonSchema(
                CreateRegistration(
                    OpenApiSchemaEvidencePurpose.Input,
                    static payload => HasValue(payload, 2)),
                static action => action.ActionName == nameof(ValidatedSchemaController.EchoTwo));
        });
        var client = app.GetTestClient();

        var first = await client.PostAsync(
            "/mvc/echo",
            new StringContent("""{"value":1}""", Encoding.UTF8, "application/json"));
        var wrongSecond = await client.PostAsync(
            "/mvc/echo-two",
            new StringContent("""{"value":1}""", Encoding.UTF8, "application/json"));
        var second = await client.PostAsync(
            "/mvc/echo-two",
            new StringContent("""{"value":2}""", Encoding.UTF8, "application/json"));

        Assert.Equal(StatusCodes.Status200OK, (int)first.StatusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, (int)wrongSecond.StatusCode);
        Assert.Equal(StatusCodes.Status200OK, (int)second.StatusCode);
    }

    [Fact]
    public async Task MvcConvention_EnforcesRequestAndResponseLimits()
    {
        await using var app = await CreateMvcApplicationAsync((endpoints, state) =>
        {
            endpoints.WithValidatedJsonSchema(
                CreateRegistration(
                    OpenApiSchemaEvidencePurpose.Input,
                    static _ => true,
                    new() { MaxPayloadSize = 4 }),
                static action => action.ActionName == nameof(ValidatedSchemaController.Echo));
            endpoints.WithValidatedJsonSchema(
                CreateRegistration(
                    OpenApiSchemaEvidencePurpose.Output,
                    static _ => true,
                    new() { MaxPayloadSize = 4 }),
                static action => action.ActionName == nameof(ValidatedSchemaController.ResponsePayload));
        });
        var client = app.GetTestClient();

        var request = await client.PostAsync(
            "/mvc/echo",
            new StringContent("""{"value":1}""", Encoding.UTF8, "application/json"));
        var response = await client.GetAsync("/mvc/response");

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, (int)request.StatusCode);
        Assert.Equal(StatusCodes.Status500InternalServerError, (int)response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task MvcConvention_PropagatesActionExceptionsAndRemainsReusable()
    {
        await using var app = await CreateMvcApplicationAsync((endpoints, state) =>
        {
            var registration = CreateRegistration(OpenApiSchemaEvidencePurpose.Output, static _ => true);
            endpoints.WithValidatedJsonSchema(
                registration,
                static action => action.ActionName == nameof(ValidatedSchemaController.Throws));
            endpoints.WithValidatedJsonSchema(
                registration,
                static action => action.ActionName == nameof(ValidatedSchemaController.ResponsePayload));
        });
        var client = app.GetTestClient();

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("/mvc/throws"));
        var response = await client.GetAsync("/mvc/response");

        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
        Assert.True(IsValueOne(await response.Content.ReadAsByteArrayAsync()));
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
        => HasValue(payload, 1);

    private static bool HasValue(ReadOnlyMemory<byte> payload, int expected)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        return (root.TryGetProperty("value", out var value) || root.TryGetProperty("Value", out value)) &&
            value.GetInt32() == expected;
    }

    private static async Task<WebApplication> CreateMvcApplicationAsync(
        Action<ControllerActionEndpointConventionBuilder, MvcValidationState> configure)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var state = new MvcValidationState();
        builder.Services.AddSingleton(state);
        builder.Services.AddScoped<ReplaceValidatedResultFilter>();
        builder.Services
            .AddControllers()
            .AddApplicationPart(typeof(ValidatedSchemaController).Assembly)
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new CountingNodeConverter(state)));
        var app = builder.Build();
        var endpoints = app.MapControllers();
        configure(endpoints, state);
        await app.StartAsync();
        return app;
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

    private static OpenApiValidatedJsonSchemaRegistration CreateRegistration(
        OpenApiJsonSchemaDialect dialect,
        string schema,
        OpenApiJsonSchemaValidationCapabilities capabilities = OpenApiJsonSchemaValidationCapabilities.None,
        string configurationIdentity = "test-validator")
        => new(
            typeof(object),
            OpenApiSchemaEvidencePurpose.Input,
            Encoding.UTF8.GetBytes(schema),
            dialect,
            capabilities,
            new ValidatorFactory(static _ => true, configurationIdentity));

    private sealed class ValidatorFactory(
        Func<ReadOnlyMemory<byte>, bool> validate,
        string configurationIdentity = nameof(ValidatorFactory),
        Func<OpenApiJsonSchemaDialect, bool> supportsDialect = null) : IOpenApiJsonSchemaValidatorFactory
    {
        public string ConfigurationIdentity => configurationIdentity;

        public bool SupportsDialect(OpenApiJsonSchemaDialect dialect)
            => supportsDialect?.Invoke(dialect) ?? true;

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

    public sealed class MvcValidationState
    {
        public int ActionInvocations;
        public int InputFormatterReads;
        public int ResultFilterInvocations;
    }

    private sealed class CountingNodeConverter(MvcValidationState state) : System.Text.Json.Serialization.JsonConverter<ValidatedNode>
    {
        public override ValidatedNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            state.InputFormatterReads++;
            using var document = JsonDocument.ParseValue(ref reader);
            return new() { Value = document.RootElement.GetProperty("value").GetInt32() };
        }

        public override void Write(Utf8JsonWriter writer, ValidatedNode value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("value", value.Value);
            writer.WriteEndObject();
        }
    }

    private sealed class RequestBodyDetectionFeature : IHttpRequestBodyDetectionFeature
    {
        public bool CanHaveBody => true;
    }

    private sealed class GeneratedBinding : IOpenApiValidatedJsonSchemaBinding<GeneratedBinding>
    {
        public static Type Type => typeof(ValidatedNode);
        public static string SchemaIdentity => "generated-schema";
        public static string Identity => "generated-binding";
        public static OpenApiJsonSchemaDialect Dialect => OpenApiJsonSchemaDialect.Draft202012;
        public static OpenApiJsonSchemaValidationCapabilities Capabilities
            => OpenApiJsonSchemaValidationCapabilities.None;

        public static OpenApiSchema CreateOpenApiSchema(OpenApiSpecVersion openApiVersion)
            => new();

        public static ValueTask<OpenApiJsonSchemaValidationResult> ValidateAsync(
            ReadOnlyMemory<byte> utf8Json,
            OpenApiSchemaEvidencePurpose purpose,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(IsValueOne(utf8Json)
                ? OpenApiJsonSchemaValidationResult.Valid
                : new OpenApiJsonSchemaValidationResult(
                [
                    new OpenApiJsonSchemaValidationError("/value", "const", "The value must be one."),
                ]));
    }
}

[ApiController]
public sealed class ValidatedSchemaController(ValidatedJsonSchemaTests.MvcValidationState state) : ControllerBase
{
    [HttpPost("/mvc/echo")]
    public ActionResult<ValidatedJsonSchemaTests.ValidatedNode> Echo(ValidatedJsonSchemaTests.ValidatedNode node)
    {
        state.ActionInvocations++;
        return node;
    }

    [HttpPost("/mvc/echo-two")]
    public ActionResult<ValidatedJsonSchemaTests.ValidatedNode> EchoTwo(ValidatedJsonSchemaTests.ValidatedNode node)
    {
        state.ActionInvocations++;
        return node;
    }

    [HttpGet("/mvc/response")]
    public ActionResult<ValidatedJsonSchemaTests.ValidatedNode> ResponsePayload()
        => new ValidatedJsonSchemaTests.ValidatedNode { Value = 1 };

    [HttpGet("/mvc/created")]
    public IActionResult CreatedPayload()
        => StatusCode(StatusCodes.Status201Created, new ValidatedJsonSchemaTests.ValidatedNode { Value = 1 });

    [HttpGet("/mvc/filtered")]
    [ServiceFilter(typeof(ReplaceValidatedResultFilter))]
    public ActionResult<ValidatedJsonSchemaTests.ValidatedNode> Filtered()
        => new ValidatedJsonSchemaTests.ValidatedNode { Value = 1 };

    [HttpGet("/mvc/throws")]
    public IActionResult Throws()
        => throw new InvalidOperationException("Expected MVC action failure.");

    [HttpGet("/mvc/problem")]
    public IActionResult ProblemPayload()
    {
        var result = new ObjectResult(new ValidatedJsonSchemaTests.ValidatedNode { Value = 1 })
        {
            StatusCode = StatusCodes.Status422UnprocessableEntity,
        };
        result.ContentTypes.Add("application/problem+json");
        return result;
    }

    [HttpPost("/mvc/optional")]
    public IActionResult Optional(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ValidatedJsonSchemaTests.ValidatedNode node)
    {
        state.ActionInvocations++;
        return NoContent();
    }

    [HttpPost("/mvc/cancel")]
    public async Task<IActionResult> Cancel(
        ValidatedJsonSchemaTests.ValidatedNode node,
        CancellationToken cancellationToken)
    {
        state.ActionInvocations++;
        await Task.Delay(Timeout.Infinite, cancellationToken);
        return Ok();
    }
}

public sealed class ReplaceValidatedResultFilter(ValidatedJsonSchemaTests.MvcValidationState state) : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        state.ResultFilterInvocations++;
        context.Result = new JsonResult(new ValidatedJsonSchemaTests.ValidatedNode { Value = 2 });
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }
}
