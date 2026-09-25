// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ValidatedSchemaAdapters;

namespace Microsoft.AspNetCore.OpenApi.Microbenchmarks;

#pragma warning disable ASP0040

[MemoryDiagnoser]
public class ValidatedJsonSchemaBenchmarks
{
    private static readonly byte[] s_payload = """{"value":1}"""u8.ToArray();
    private static readonly byte[] s_invalidPayload = """[]"""u8.ToArray();
    private static readonly byte[] s_schema = """
        {
          "$schema": "https://json-schema.org/draft/2020-12/schema",
          "type": "object"
        }
        """u8.ToArray();
    private static readonly RequestDelegate s_next = Next;

    private DefaultHttpContext _context;
    private DefaultHttpContext _responseContext;
    private OpenApiValidatedJsonSchemaEndpointPlan _requestPlan;
    private OpenApiValidatedJsonSchemaEndpointPlan _responsePlan;
    private IOpenApiJsonSchemaValidator _noOpValidator;
    private IOpenApiJsonSchemaValidator _corvusValidator;
    private IOpenApiJsonSchemaValidator _jsonSchemaNetValidator;
    private OpenApiJsonSchemaValidationContext _validationContext;

    [GlobalSetup]
    public void Setup()
    {
        var input = new OpenApiValidatedJsonSchemaRegistration(
            typeof(object),
            OpenApiSchemaEvidencePurpose.Input,
            s_schema,
            OpenApiJsonSchemaDialect.Draft202012,
            OpenApiJsonSchemaValidationCapabilities.None,
            NoOpValidatorFactory.Instance);
        _context = new DefaultHttpContext();
        _context.Request.ContentType = "application/json";
        _context.Request.ContentLength = s_payload.Length;
        _context.Request.Body = new MemoryStream(s_payload, writable: false);
        _requestPlan = new(s_next);
        _requestPlan.Add(input);

        var output = new OpenApiValidatedJsonSchemaRegistration(
            typeof(object),
            OpenApiSchemaEvidencePurpose.Output,
            s_schema,
            OpenApiJsonSchemaDialect.Draft202012,
            OpenApiJsonSchemaValidationCapabilities.None,
            NoOpValidatorFactory.Instance);
        _responseContext = new DefaultHttpContext();
        _responseContext.Response.Body = Stream.Null;
        _responsePlan = new(s_next);
        _responsePlan.Add(output);

        _noOpValidator = NoOpValidator.Instance;
        _corvusValidator = new CorvusValidatorFactory().CreateValidator(input.Evidence);
        _jsonSchemaNetValidator = new JsonSchemaNetValidatorFactory().CreateValidator(input.Evidence);
        _validationContext = input.ValidationContext;

        _requestPlan.ExecuteAsync(_context).GetAwaiter().GetResult();
        _responsePlan.ExecuteAsync(_responseContext).GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true)]
    public Task PassThrough()
    {
        _context.Request.Body.Position = 0;
        return s_next(_context);
    }

    [Benchmark]
    public Task ValidatedFrameworkOnly()
    {
        _context.Request.Body.Position = 0;
        return _requestPlan.ExecuteAsync(_context);
    }

    [Benchmark]
    public Task ValidatedFrameworkOnlyResponse()
        => _responsePlan.ExecuteAsync(_responseContext);

    [Benchmark]
    public bool ValidatorNoOp()
        => _noOpValidator.ValidateAsync(s_payload, _validationContext).GetAwaiter().GetResult().IsValid;

    [Benchmark]
    public bool CorvusValid()
        => _corvusValidator.ValidateAsync(s_payload, _validationContext).GetAwaiter().GetResult().IsValid;

    [Benchmark]
    public bool JsonSchemaNetValid()
        => _jsonSchemaNetValidator.ValidateAsync(s_payload, _validationContext).GetAwaiter().GetResult().IsValid;

    [Benchmark]
    public bool CorvusInvalidDiagnostics()
        => _corvusValidator.ValidateAsync(s_invalidPayload, _validationContext).GetAwaiter().GetResult().IsValid;

    [Benchmark]
    public bool JsonSchemaNetInvalidDiagnostics()
        => _jsonSchemaNetValidator.ValidateAsync(s_invalidPayload, _validationContext).GetAwaiter().GetResult().IsValid;

    private static Task Next(HttpContext context)
    {
        context.Response.ContentType = "application/json";
        context.Response.Body.Write(s_payload);
        return Task.CompletedTask;
    }

    private sealed class NoOpValidatorFactory : IOpenApiJsonSchemaValidatorFactory
    {
        public static readonly NoOpValidatorFactory Instance = new();

        public IOpenApiJsonSchemaValidator CreateValidator(OpenApiValidatedJsonSchemaEvidence evidence)
            => NoOpValidator.Instance;
    }

    private sealed class NoOpValidator : IOpenApiJsonSchemaValidator
    {
        public static readonly NoOpValidator Instance = new();

        public ValueTask<OpenApiJsonSchemaValidationResult> ValidateAsync(
            ReadOnlyMemory<byte> utf8Json,
            OpenApiJsonSchemaValidationContext context,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(OpenApiJsonSchemaValidationResult.Valid);
    }
}
