#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

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

    private class DefaultEndpointRouteBuilder(IApplicationBuilder applicationBuilder) : IEndpointRouteBuilder
    {
        private IApplicationBuilder ApplicationBuilder { get; } = applicationBuilder ?? throw new ArgumentNullException(nameof(applicationBuilder));
        public IApplicationBuilder CreateApplicationBuilder() => ApplicationBuilder.New();
        public ICollection<EndpointDataSource> DataSources { get; } = [];
        public IServiceProvider ServiceProvider => ApplicationBuilder.ApplicationServices;
    }
}
