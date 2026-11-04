// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http.Extensions.Tests;

public class ValidationEndpointConventionBuilderExtensionsTests : LoggedTest
{
    [Fact]
    public async Task DisableValidation_PreventsValidationFilterRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddValidation();
        services.AddSingleton(LoggerFactory);
        var serviceProvider = services.BuildServiceProvider();

        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));

        // Act - Create two endpoints - one with validation disabled, one without
        var regularBuilder = builder.MapGet("test-enabled", ([Range(5, 10)] int param) => "Validation enabled here.");
        var disabledBuilder = builder.MapGet("test-disabled", ([Range(5, 10)] int param) => "Validation disabled here.");

        disabledBuilder.DisableValidation();

        // Build the endpoints
        var dataSource = Assert.Single(builder.DataSources);
        var endpoints = dataSource.Endpoints;

        // Assert
        Assert.Equal(2, endpoints.Count);

        // Get filter factories from both endpoints
        var regularEndpoint = endpoints[0];
        var disabledEndpoint = endpoints[1];

        // Verify the disabled endpoint has the IDisableValidationMetadata
        Assert.Contains(disabledEndpoint.Metadata, m => m is IDisableValidationMetadata);

        // Verify that invalid arguments on the disabled endpoint do not trigger validation
        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        context.Request.Method = "GET";
        context.Request.QueryString = new QueryString("?param=15");
        var ms = new MemoryStream();
        context.Response.Body = ms;

        await disabledEndpoint.RequestDelegate(context);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("Validation disabled here.", Encoding.UTF8.GetString(ms.ToArray()));

        context = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        context.Request.Method = "GET";
        context.Request.QueryString = new QueryString("?param=15");
        await regularEndpoint.RequestDelegate(context);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    private class DefaultEndpointRouteBuilder(IApplicationBuilder applicationBuilder) : IEndpointRouteBuilder
    {
        private IApplicationBuilder ApplicationBuilder { get; } = applicationBuilder ?? throw new ArgumentNullException(nameof(applicationBuilder));
        public IApplicationBuilder CreateApplicationBuilder() => ApplicationBuilder.New();
        public ICollection<EndpointDataSource> DataSources { get; } = [];
        public IServiceProvider ServiceProvider => ApplicationBuilder.ApplicationServices;
    }
}
