// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.OpenApi;
using ValidatedSchemaAdapters;

#pragma warning disable ASP0040

var schema = """
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
      "$ref": "#/$defs/node"
    }
    """u8.ToArray();

var corpus = new (byte[] Payload, bool Valid)[]
{
    ("""{"value":1}"""u8.ToArray(), true),
    ("""{"value":1,"next":{"value":2}}"""u8.ToArray(), true),
    ("""{"value":0}"""u8.ToArray(), false),
    ("""{"value":1,"extra":true}"""u8.ToArray(), false),
};

foreach (var factory in new IOpenApiJsonSchemaValidatorFactory[]
{
    new CorvusValidatorFactory(),
    new JsonSchemaNetValidatorFactory(),
})
{
    var registration = new OpenApiValidatedJsonSchemaRegistration(
        typeof(Node),
        OpenApiSchemaEvidencePurpose.Input,
        schema,
        OpenApiJsonSchemaDialect.Draft202012,
        OpenApiJsonSchemaValidationCapabilities.FormatAssertions,
        factory);
    var validator = factory.CreateValidator(registration.Evidence);
    if (!ReferenceEquals(validator, factory.CreateValidator(registration.Evidence)))
    {
        throw new InvalidOperationException($"{factory.GetType().Name}: compiled validator cache was not reused.");
    }
    var failures = new ConcurrentQueue<string>();
    await Parallel.ForEachAsync(
        Enumerable.Range(0, 32),
        async (_, cancellationToken) =>
        {
            foreach (var (payload, expected) in corpus)
            {
                var result = await validator.ValidateAsync(
                    payload,
                    new(registration.Evidence, OpenApiSchemaEvidencePurpose.Input),
                    cancellationToken);
                if (result.IsValid != expected)
                {
                    failures.Enqueue($"{factory.GetType().Name}: expected {expected} for {payload.Length} bytes.");
                }
            }
        });

    if (!failures.IsEmpty)
    {
        throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
    }

    Console.WriteLine($"{factory.GetType().Name}: corpus and concurrent cache reuse passed; schema {registration.Evidence.Identity}");

    var formatSchema = """
        {
          "$schema": "https://json-schema.org/draft/2020-12/schema",
          "type": "string",
          "format": "date"
        }
        """u8.ToArray();
    var annotationOnly = new OpenApiValidatedJsonSchemaRegistration(
        typeof(string),
        OpenApiSchemaEvidencePurpose.Input,
        formatSchema,
        OpenApiJsonSchemaDialect.Draft202012,
        OpenApiJsonSchemaValidationCapabilities.None,
        factory);
    var asserting = new OpenApiValidatedJsonSchemaRegistration(
        typeof(string),
        OpenApiSchemaEvidencePurpose.Input,
        formatSchema,
        OpenApiJsonSchemaDialect.Draft202012,
        OpenApiJsonSchemaValidationCapabilities.FormatAssertions,
        factory);
    var invalidDate = "\"not-a-date\""u8.ToArray();
    var annotationResult = await factory.CreateValidator(annotationOnly.Evidence).ValidateAsync(
        invalidDate,
        new(annotationOnly.Evidence, OpenApiSchemaEvidencePurpose.Input));
    var assertionResult = await factory.CreateValidator(asserting.Evidence).ValidateAsync(
        invalidDate,
        new(asserting.Evidence, OpenApiSchemaEvidencePurpose.Input));
    if (!annotationResult.IsValid || assertionResult.IsValid)
    {
        throw new InvalidOperationException($"{factory.GetType().Name}: format assertion capability was not honored.");
    }
    Console.WriteLine($"{factory.GetType().Name}: format annotation/assertion toggle passed.");

    await RunMinimalApiEvidenceAsync(factory, schema);
}

static async Task RunMinimalApiEvidenceAsync(
    IOpenApiJsonSchemaValidatorFactory factory,
    byte[] schema)
{
    var input = new OpenApiValidatedJsonSchemaRegistration(
        typeof(Node),
        OpenApiSchemaEvidencePurpose.Input,
        schema,
        OpenApiJsonSchemaDialect.Draft202012,
        OpenApiJsonSchemaValidationCapabilities.FormatAssertions,
        factory);
    var output = new OpenApiValidatedJsonSchemaRegistration(
        typeof(Node),
        OpenApiSchemaEvidencePurpose.Output,
        schema,
        OpenApiJsonSchemaDialect.Draft202012,
        OpenApiJsonSchemaValidationCapabilities.FormatAssertions,
        factory);
    var smallInput = new OpenApiValidatedJsonSchemaRegistration(
        typeof(Node),
        OpenApiSchemaEvidencePurpose.Input,
        schema,
        OpenApiJsonSchemaDialect.Draft202012,
        OpenApiJsonSchemaValidationCapabilities.FormatAssertions,
        factory,
        new() { MaxPayloadSize = 4 });
    var smallOutput = new OpenApiValidatedJsonSchemaRegistration(
        typeof(Node),
        OpenApiSchemaEvidencePurpose.Output,
        schema,
        OpenApiJsonSchemaDialect.Draft202012,
        OpenApiJsonSchemaValidationCapabilities.FormatAssertions,
        factory,
        new() { MaxPayloadSize = 4 });

    var builder = WebApplication.CreateSlimBuilder();
    builder.WebHost.UseUrls("http://127.0.0.1:0");
    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
    var app = builder.Build();
    app.MapPost("/node", (Node node) => node)
        .WithValidatedJsonSchema(input)
        .WithValidatedJsonSchema(output);
    app.MapPost("/small-request", (Node node) => node)
        .WithValidatedJsonSchema(smallInput);
    app.MapGet("/invalid-response", () => new Node(0, null))
        .WithValidatedJsonSchema(output);
    app.MapGet("/small-response", () => new Node(1, null))
        .WithValidatedJsonSchema(smallOutput);

    await app.StartAsync();
    try
    {
        var addresses = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()?.Addresses;
        using var client = new HttpClient
        {
            BaseAddress = new Uri(addresses?.Single() ??
                throw new InvalidOperationException("The evidence server did not publish an address.")),
        };

        using var valid = await client.PostAsync(
            "/node",
            new StringContent("""{"value":1}""", Encoding.UTF8, "application/json"));
        var validPayload = await valid.Content.ReadAsStringAsync();
        if (valid.StatusCode != HttpStatusCode.OK || !validPayload.Contains("\"value\":1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{factory.GetType().Name}: valid request/response evidence failed ({valid.StatusCode}, '{validPayload}').");
        }

        using var invalidRequest = await client.PostAsync(
            "/node",
            new StringContent("""{"value":0}""", Encoding.UTF8, "application/json"));
        using var invalidResponse = await client.GetAsync("/invalid-response");
        using var oversizedRequest = await client.PostAsync(
            "/small-request",
            new StringContent("""{"value":1}""", Encoding.UTF8, "application/json"));
        using var oversizedResponse = await client.GetAsync("/small-response");
        if (invalidRequest.StatusCode != HttpStatusCode.BadRequest ||
            invalidResponse.StatusCode != HttpStatusCode.InternalServerError ||
            oversizedRequest.StatusCode != HttpStatusCode.RequestEntityTooLarge ||
            oversizedResponse.StatusCode != HttpStatusCode.InternalServerError)
        {
            throw new InvalidOperationException(
                $"{factory.GetType().Name}: Minimal API enforcement evidence failed " +
                $"({invalidRequest.StatusCode}, {invalidResponse.StatusCode}, " +
                $"{oversizedRequest.StatusCode}, {oversizedResponse.StatusCode}).");
        }

        Console.WriteLine($"{factory.GetType().Name}: Minimal API request/response and size-limit evidence passed.");
    }
    finally
    {
        await app.StopAsync();
        await app.DisposeAsync();
    }
}

internal sealed record Node(int Value, Node? Next);
