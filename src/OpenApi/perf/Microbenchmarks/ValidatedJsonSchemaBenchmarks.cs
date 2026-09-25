// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

    [MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    public class ValidatedJsonSchemaMvcBenchmarks
    {
        private static readonly byte[] s_payload = """{"value":1}"""u8.ToArray();
        private static readonly byte[] s_schema = """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "type": "object"
            }
            """u8.ToArray();

        private WebApplication _baselineApplication;
        private WebApplication _validatedApplication;
        private DefaultHttpContext _baselineRequestContext;
        private DefaultHttpContext _validatedRequestContext;
        private DefaultHttpContext _baselineResponseContext;
        private DefaultHttpContext _validatedResponseContext;
        private RequestDelegate _baselineRequest;
        private RequestDelegate _validatedRequest;
        private RequestDelegate _baselineResponse;
        private RequestDelegate _validatedResponse;

        [GlobalSetup]
        public void Setup()
        {
            _baselineApplication = CreateApplication(addValidation: false);
            _validatedApplication = CreateApplication(addValidation: true);
            _baselineRequest = GetEndpoint(_baselineApplication, "/mvc/echo");
            _validatedRequest = GetEndpoint(_validatedApplication, "/mvc/echo");
            _baselineResponse = GetEndpoint(_baselineApplication, "/mvc/response");
            _validatedResponse = GetEndpoint(_validatedApplication, "/mvc/response");
            _baselineRequestContext = CreateRequestContext(_baselineApplication.Services);
            _validatedRequestContext = CreateRequestContext(_validatedApplication.Services);
            _baselineResponseContext = CreateResponseContext(_baselineApplication.Services);
            _validatedResponseContext = CreateResponseContext(_validatedApplication.Services);

            MvcRequestPassThrough().GetAwaiter().GetResult();
            MvcRequestValidated().GetAwaiter().GetResult();
            MvcResponsePassThrough().GetAwaiter().GetResult();
            MvcResponseValidated().GetAwaiter().GetResult();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _baselineApplication.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _validatedApplication.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("MVC request")]
        public Task MvcRequestPassThrough()
        {
            _baselineRequestContext.Request.Body.Position = 0;
            return _baselineRequest(_baselineRequestContext);
        }

        [Benchmark]
        [BenchmarkCategory("MVC request")]
        public Task MvcRequestValidated()
        {
            _validatedRequestContext.Request.Body.Position = 0;
            return _validatedRequest(_validatedRequestContext);
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("MVC response")]
        public Task MvcResponsePassThrough()
            => _baselineResponse(_baselineResponseContext);

        [Benchmark]
        [BenchmarkCategory("MVC response")]
        public Task MvcResponseValidated()
            => _validatedResponse(_validatedResponseContext);

        private static WebApplication CreateApplication(bool addValidation)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
            builder.Services.AddSingleton(new ValidatedJsonSchemaTests.MvcValidationState());
            builder.Services.AddScoped<ReplaceValidatedResultFilter>();
            builder.Services.AddControllers().AddApplicationPart(typeof(ValidatedSchemaController).Assembly);
            var app = builder.Build();
            var endpoints = app.MapControllers();
            if (addValidation)
            {
                endpoints.WithValidatedJsonSchema(
                    CreateRegistration(OpenApiSchemaEvidencePurpose.Input),
                    static action => action.ActionName == nameof(ValidatedSchemaController.Echo));
                endpoints.WithValidatedJsonSchema(
                    CreateRegistration(OpenApiSchemaEvidencePurpose.Output),
                    static action => action.ActionName == nameof(ValidatedSchemaController.ResponsePayload));
            }
            app.StartAsync().GetAwaiter().GetResult();
            return app;
        }

        private static OpenApiValidatedJsonSchemaRegistration CreateRegistration(OpenApiSchemaEvidencePurpose purpose)
            => new(
                typeof(ValidatedJsonSchemaTests.ValidatedNode),
                purpose,
                s_schema,
                OpenApiJsonSchemaDialect.Draft202012,
                OpenApiJsonSchemaValidationCapabilities.None,
                NoOpValidatorFactory.Instance);

        private static RequestDelegate GetEndpoint(WebApplication app, string route)
            => app.Services.GetRequiredService<EndpointDataSource>().Endpoints
                .OfType<RouteEndpoint>()
                .Single(endpoint => string.Equals(endpoint.RoutePattern.RawText, route.TrimStart('/'), StringComparison.Ordinal))
                .RequestDelegate!;

        private static DefaultHttpContext CreateRequestContext(IServiceProvider services)
        {
            var context = CreateResponseContext(services);
            context.Request.ContentType = "application/json";
            context.Request.ContentLength = s_payload.Length;
            context.Request.Body = new MemoryStream(s_payload, writable: false);
            return context;
        }

        private static DefaultHttpContext CreateResponseContext(IServiceProvider services)
        {
            var context = new DefaultHttpContext
            {
                RequestServices = services,
            };
            context.Response.Body = Stream.Null;
            return context;
        }

        private sealed class NoOpValidatorFactory : IOpenApiJsonSchemaValidatorFactory
        {
            public static readonly NoOpValidatorFactory Instance = new();

            public string ConfigurationIdentity => nameof(NoOpValidatorFactory);

            public bool SupportsDialect(OpenApiJsonSchemaDialect dialect) => true;

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

        public string ConfigurationIdentity => nameof(NoOpValidatorFactory);

        public bool SupportsDialect(OpenApiJsonSchemaDialect dialect) => true;

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
