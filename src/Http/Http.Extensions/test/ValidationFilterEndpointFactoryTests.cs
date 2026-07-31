#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http.Extensions.Tests;

public class ValidationEndpointFilterFactoryTests : LoggedTest
{
    [Fact]
    public async Task GetHttpValidationProblemDetailsWhenProblemDetailsServiceNotRegistered()
    {
        var services = new ServiceCollection();
        services.AddValidation();
        var serviceProvider = services.BuildServiceProvider();

        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));

        // Act - Create one endpoint with validation
        builder.MapGet("validation-test", ([Range(5, 10)] int param) => "Validation enabled here.");

        // Build the endpoints
        var dataSource = Assert.Single(builder.DataSources);
        var endpoints = dataSource.Endpoints;

        // Get filter factories from endpoint
        var endpoint = endpoints[0];

        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        context.Request.Method = "GET";
        context.Request.QueryString = new QueryString("?param=15");
        using var ms = new MemoryStream();
        context.Response.Body = ms;

        await endpoint.RequestDelegate(context);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.StartsWith(MediaTypeNames.Application.Json, context.Response.ContentType, StringComparison.OrdinalIgnoreCase);

        ms.Seek(0, SeekOrigin.Begin);
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(ms, JsonSerializerOptions.Web);
        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);

        Assert.Equal("One or more validation errors occurred.", problemDetails.Title);

        // Check that ProblemDetails contains the errors object with 1 validation error
        Assert.True(problemDetails.Extensions.TryGetValue("errors", out var errorsObj));
        var errors = Assert.IsType<JsonElement>(errorsObj);
        Assert.True(errors.EnumerateObject().Count() == 1);
    }

    [Fact]
    public async Task UseProblemDetailsServiceWhenAddedInServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddValidation();
        services.AddProblemDetails();
        var serviceProvider = services.BuildServiceProvider();

        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));

        // Act - Create one endpoint with validation
        builder.MapGet("validation-test", ([Range(5, 10)] int param) => "Validation enabled here.");

        // Build the endpoints
        var dataSource = Assert.Single(builder.DataSources);
        var endpoints = dataSource.Endpoints;

        // Get filter factories from endpoint
        var endpoint = endpoints[0];

        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        context.Request.Method = "GET";
        context.Request.QueryString = new QueryString("?param=15");
        using var ms = new MemoryStream();
        context.Response.Body = ms;

        await endpoint.RequestDelegate(context);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.StartsWith(MediaTypeNames.Application.ProblemJson, context.Response.ContentType, StringComparison.OrdinalIgnoreCase);

        ms.Seek(0, SeekOrigin.Begin);
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(ms, JsonSerializerOptions.Web);

        // Check if the response is an actual ProblemDetails object
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.1", problemDetails.Type);
        Assert.Equal("One or more validation errors occurred.", problemDetails.Title);
        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);

        // Check that ProblemDetails contains the errors object with 1 validation error
        Assert.True(problemDetails.Extensions.TryGetValue("errors", out var errorsObj));
        var errors = Assert.IsType<JsonElement>(errorsObj);
        Assert.True(errors.EnumerateObject().Count() == 1);
    }

    [Fact]
    public async Task UseProblemDetailsServiceWithCallbackWhenAddedInServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddValidation();

        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions.Add("timestamp", DateTimeOffset.Now);
            };
        });

        var serviceProvider = services.BuildServiceProvider();

        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));

        // Act - Create one endpoint with validation
        builder.MapGet("validation-test", ([Range(5, 10)] int param) => "Validation enabled here.");

        // Build the endpoints
        var dataSource = Assert.Single(builder.DataSources);
        var endpoints = dataSource.Endpoints;

        // Get filter factories from endpoint
        var endpoint = endpoints[0];

        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        context.Request.Method = "GET";
        context.Request.QueryString = new QueryString("?param=15");
        using var ms = new MemoryStream();
        context.Response.Body = ms;

        await endpoint.RequestDelegate(context);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.StartsWith(MediaTypeNames.Application.ProblemJson, context.Response.ContentType, StringComparison.OrdinalIgnoreCase);

        ms.Seek(0, SeekOrigin.Begin);
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(ms, JsonSerializerOptions.Web);

        // Check if the response is an actual ProblemDetails object
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.1", problemDetails.Type);
        Assert.Equal("One or more validation errors occurred.", problemDetails.Title);
        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);

        // Check that ProblemDetails contains the errors object with 1 validation error
        Assert.True(problemDetails.Extensions.TryGetValue("errors", out var errorsObj));
        var errors = Assert.IsType<JsonElement>(errorsObj);
        Assert.True(errors.EnumerateObject().Count() == 1);

        // Check that ProblemDetails customizations are applied in the response
        Assert.True(problemDetails.Extensions.ContainsKey("timestamp"));
    }

    [Fact]
    public async Task ValidatesFromBodyIEnumerableParameter_NotSkippedAsInjectedService()
    {
        // Regression test for https://github.com/dotnet/aspnetcore/issues/65084.
        // IServiceProviderIsService.IsService returns true for every IEnumerable<T>, which previously
        // caused the validation filter to skip a [FromBody] IEnumerable<string> parameter even though
        // ApiExplorer/OpenAPI correctly bind it from the body. A [FromBody] parameter must be validated.
        var services = new ServiceCollection();
        services.AddValidation();
        var serviceProvider = services.BuildServiceProvider();

        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));

        // Act - Create one endpoint with a [FromBody] collection parameter that has a validation attribute
        builder.MapPost("validation-test", ([FromBody][MinLength(2)] IEnumerable<string> items) => "Validation enabled here.");

        // Build the endpoints
        var dataSource = Assert.Single(builder.DataSources);
        var endpoint = dataSource.Endpoints[0];

        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        context.Request.Method = "POST";
        context.Request.ContentType = MediaTypeNames.Application.Json;
        // A single-element array is bound from the body and violates [MinLength(2)].
        var requestBody = Encoding.UTF8.GetBytes("[\"a\"]");
        context.Request.Body = new MemoryStream(requestBody);
        context.Request.ContentLength = requestBody.Length;
        context.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(canHaveBody: true));
        using var ms = new MemoryStream();
        context.Response.Body = ms;

        await endpoint.RequestDelegate(context);

        // Assert - validation ran and reported the MinLength failure instead of being skipped
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        ms.Seek(0, SeekOrigin.Begin);
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(ms, JsonSerializerOptions.Web);
        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
        Assert.Equal("One or more validation errors occurred.", problemDetails.Title);
        Assert.True(problemDetails.Extensions.TryGetValue("errors", out var errorsObj));
        var errors = Assert.IsType<JsonElement>(errorsObj);
        Assert.Single(errors.EnumerateObject());
        Assert.True(errors.TryGetProperty("items", out _));
    }

    [Fact]
    public async Task SkipsValidationForIEnumerableParameterWithoutFromBody()
    {
        // Mirrors ApiExplorer/OpenAPI: without [FromBody], an IEnumerable<T> parameter is inferred as
        // an injected service (RDF resolves it from the container), so the validation filter skips it.
        var services = new ServiceCollection();
        services.AddValidation();
        var serviceProvider = services.BuildServiceProvider();

        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));

        builder.MapPost("validation-test", ([MinLength(2)] IEnumerable<string> items) => "Validation enabled here.");

        var dataSource = Assert.Single(builder.DataSources);
        var endpoint = dataSource.Endpoints[0];

        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        context.Request.Method = "POST";
        using var ms = new MemoryStream();
        context.Response.Body = ms;

        await endpoint.RequestDelegate(context);

        // The parameter is treated as a service (not a request parameter), so validation does not run.
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task SkipsValidationForGenuinelyInjectedServiceCollection()
    {
        // An IEnumerable<T> whose element type IS a registered service should be treated as an
        // injected service and skipped, so its validation attribute must not run.
        var services = new ServiceCollection();
        services.AddValidation();
        services.AddSingleton<IInjectedService, InjectedService>();
        var serviceProvider = services.BuildServiceProvider();

        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));

        builder.MapPost("validation-test", ([MinLength(2)] IEnumerable<IInjectedService> services) => "Validation enabled here.");

        var dataSource = Assert.Single(builder.DataSources);
        var endpoint = dataSource.Endpoints[0];

        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        context.Request.Method = "POST";
        using var ms = new MemoryStream();
        context.Response.Body = ms;

        await endpoint.RequestDelegate(context);

        // The single registered service violates [MinLength(2)], but because the collection is a
        // genuine service injection the validation filter must skip it, so the endpoint runs.
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task ValidatesFromRouteParameter()
    {
        // Registering the parameter type as a service makes IServiceProviderIsService.IsService return
        // true for it (as it always does for IEnumerable<T>). The explicit [FromRoute] attribute must
        // still force validation; before the fix the parameter was incorrectly skipped as a service.
        var (endpoint, services) = BuildValidationEndpoint(
            ([FromRoute][MinLength(2)] string value) => "Validation enabled here.",
            configureServices: static services => services.AddSingleton("registered"),
            pattern: "validation-test/{value}");

        var (statusCode, body) = await InvokeAsync(endpoint, services, context =>
            context.Request.RouteValues = new RouteValueDictionary { ["value"] = "a" });

        Assert.Equal(StatusCodes.Status400BadRequest, statusCode);
        await AssertSingleValidationErrorAsync(body, "value");
    }

    [Fact]
    public async Task ValidatesFromQueryParameter()
    {
        // See ValidatesFromRouteParameter: registering the type as a service proves the explicit
        // [FromQuery] attribute forces validation instead of the parameter being skipped as a service.
        var (endpoint, services) = BuildValidationEndpoint(
            ([FromQuery][MinLength(2)] string value) => "Validation enabled here.",
            configureServices: static services => services.AddSingleton("registered"));

        var (statusCode, body) = await InvokeAsync(endpoint, services, context =>
            context.Request.QueryString = new QueryString("?value=a"));

        Assert.Equal(StatusCodes.Status400BadRequest, statusCode);
        await AssertSingleValidationErrorAsync(body, "value");
    }

    [Fact]
    public async Task ValidatesFromHeaderParameter()
    {
        // See ValidatesFromRouteParameter: registering the type as a service proves the explicit
        // [FromHeader] attribute forces validation instead of the parameter being skipped as a service.
        var (endpoint, services) = BuildValidationEndpoint(
            ([FromHeader(Name = "x-value")][MinLength(2)] string value) => "Validation enabled here.",
            configureServices: static services => services.AddSingleton("registered"));

        var (statusCode, body) = await InvokeAsync(endpoint, services, context =>
            context.Request.Headers["x-value"] = "a");

        Assert.Equal(StatusCodes.Status400BadRequest, statusCode);
        await AssertSingleValidationErrorAsync(body, "value");
    }

    [Fact]
    public async Task ValidatesFromFormParameter()
    {
        // See ValidatesFromRouteParameter: registering the type as a service proves the explicit
        // [FromForm] attribute forces validation instead of the parameter being skipped as a service.
        var (endpoint, services) = BuildValidationEndpoint(
            ([FromForm][MinLength(2)] string value) => "Validation enabled here.",
            configureServices: static services => services.AddSingleton("registered"));

        var (statusCode, body) = await InvokeAsync(endpoint, services, context =>
        {
            context.Request.ContentType = "application/x-www-form-urlencoded";
            context.Request.Form = new FormCollection(new Dictionary<string, StringValues> { ["value"] = "a" });
        });

        Assert.Equal(StatusCodes.Status400BadRequest, statusCode);
        await AssertSingleValidationErrorAsync(body, "value");
    }

    [Fact]
    public async Task SkipsValidationForFromServicesParameter()
    {
        // [FromServices] is an explicit service source, so the parameter must be skipped even though it
        // carries a validation attribute that would fail (the resolved collection is empty).
        var (endpoint, services) = BuildValidationEndpoint(
            ([FromServices][MinLength(5)] IEnumerable<string> values) => "Validation enabled here.");

        var (statusCode, _) = await InvokeAsync(endpoint, services, _ => { });

        Assert.Equal(StatusCodes.Status200OK, statusCode);
    }

    [Fact]
    public async Task SkipsValidationForFromKeyedServicesParameter()
    {
        // [FromKeyedServices] is an explicit service source. The keyed value "a" violates [MinLength(5)],
        // so the endpoint returning 200 proves the parameter was skipped rather than validated.
        var (endpoint, services) = BuildValidationEndpoint(
            ([FromKeyedServices("key")][MinLength(5)] string value) => "Validation enabled here.",
            configureServices: services => services.AddKeyedSingleton("key", "a"));

        var (statusCode, _) = await InvokeAsync(endpoint, services, _ => { });

        Assert.Equal(StatusCodes.Status200OK, statusCode);
    }

    [Fact]
    public async Task SkipsValidationForFrameworkProvidedParameters()
    {
        // Framework-provided parameters (HttpContext, HttpRequest, HttpResponse, ClaimsPrincipal,
        // CancellationToken) are not request parameters and must never be validated. If any of them
        // were validated there would be more than the single expected error for the query parameter.
        var (endpoint, services) = BuildValidationEndpoint((
            HttpContext httpContext,
            HttpRequest request,
            HttpResponse response,
            ClaimsPrincipal user,
            CancellationToken cancellationToken,
            [FromQuery][MinLength(2)] string value) => "Validation enabled here.");

        var (statusCode, body) = await InvokeAsync(endpoint, services, context =>
            context.Request.QueryString = new QueryString("?value=a"));

        Assert.Equal(StatusCodes.Status400BadRequest, statusCode);
        await AssertSingleValidationErrorAsync(body, "value");
    }

    [Fact]
    public async Task ValidatesBindAsyncParameter_CurrentBehavior()
    {
        // Characterization test: IsServiceParameter does not (yet) account for BindAsync, unlike
        // ApiExplorer which treats a BindAsync parameter as a non-request (service-like) parameter.
        // As a result the parameter is currently validated. If BindAsync handling is added to
        // IsServiceParameter, this behavior would change to a skip and this test should be updated.
        var (endpoint, services) = BuildValidationEndpoint(
            ([AlwaysInvalid] BindAsyncParameter value) => "Validation enabled here.");

        var (statusCode, body) = await InvokeAsync(endpoint, services, _ => { });

        Assert.Equal(StatusCodes.Status400BadRequest, statusCode);
        await AssertSingleValidationErrorAsync(body, "value");
    }

    private static (Endpoint Endpoint, IServiceProvider Services) BuildValidationEndpoint(
        Delegate handler,
        Action<IServiceCollection> configureServices = null,
        string pattern = "validation-test")
    {
        var services = new ServiceCollection();
        services.AddValidation();
        configureServices?.Invoke(services);
        var serviceProvider = services.BuildServiceProvider();

        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));
        builder.MapPost(pattern, handler);

        var dataSource = Assert.Single(builder.DataSources);
        return (dataSource.Endpoints[0], serviceProvider);
    }

    private static async Task<(int StatusCode, MemoryStream Body)> InvokeAsync(
        Endpoint endpoint,
        IServiceProvider services,
        Action<HttpContext> configureRequest)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = services
        };
        context.Request.Method = "POST";
        configureRequest(context);
        var body = new MemoryStream();
        context.Response.Body = body;

        await endpoint.RequestDelegate(context);

        return (context.Response.StatusCode, body);
    }

    private static async Task AssertSingleValidationErrorAsync(MemoryStream body, string expectedKey)
    {
        body.Seek(0, SeekOrigin.Begin);
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(body, JsonSerializerOptions.Web);
        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
        Assert.True(problemDetails.Extensions.TryGetValue("errors", out var errorsObj));
        var errors = Assert.IsType<JsonElement>(errorsObj);
        var error = Assert.Single(errors.EnumerateObject());
        Assert.Equal(expectedKey, error.Name);
    }

    private interface IInjectedService;

    private sealed class InjectedService : IInjectedService;

    private sealed class BindAsyncParameter
    {
        public static ValueTask<BindAsyncParameter> BindAsync(HttpContext context)
            => ValueTask.FromResult(new BindAsyncParameter());
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    private sealed class AlwaysInvalidAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            => new ValidationResult("Always invalid.", [validationContext.DisplayName]);
    }

    private sealed class RequestBodyDetectionFeature(bool canHaveBody) : IHttpRequestBodyDetectionFeature
    {
        public bool CanHaveBody { get; } = canHaveBody;
    }

    private class DefaultEndpointRouteBuilder(IApplicationBuilder applicationBuilder) : IEndpointRouteBuilder
    {
        private IApplicationBuilder ApplicationBuilder { get; } = applicationBuilder ?? throw new ArgumentNullException(nameof(applicationBuilder));
        public IApplicationBuilder CreateApplicationBuilder() => ApplicationBuilder.New();
        public ICollection<EndpointDataSource> DataSources { get; } = [];
        public IServiceProvider ServiceProvider => ApplicationBuilder.ApplicationServices;
    }
}
