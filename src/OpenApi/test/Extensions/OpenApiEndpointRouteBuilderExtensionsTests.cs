// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Routing;
using static Microsoft.AspNetCore.OpenApi.Tests.OpenApiOperationGeneratorTests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using System.Text;

public class OpenApiEndpointRouteBuilderExtensionsTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public void MapOpenApi_ReturnsEndpointConventionBuilder()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));

        // Act
        var returnedBuilder = builder.MapOpenApi();

        // Assert
        Assert.IsAssignableFrom<IEndpointConventionBuilder>(returnedBuilder);
    }

    [Fact]
    public void MapOpenApi_SupportsCustomizingPath()
    {
        // Arrange
        var expectedPath = "/custom/{documentName}/openapi.json";
        var serviceProvider = CreateServiceProvider();
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));

        // Act
        builder.MapOpenApi(expectedPath);

        // Assert
        var generatedEndpoint = Assert.IsType<RouteEndpoint>(builder.DataSources.First().Endpoints.First());
        Assert.Equal(expectedPath, generatedEndpoint.RoutePattern.RawText);
    }

    [Fact]
    public async Task MapOpenApi_ReturnsRenderedDocument()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));
        builder.MapOpenApi();
        var context = new DefaultHttpContext();
        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;
        context.RequestServices = serviceProvider;
        context.Request.RouteValues.Add("documentName", "v1");
        var endpoint = builder.DataSources.First().Endpoints.First();

        // Act
        var requestDelegate = endpoint.RequestDelegate;
        await requestDelegate(context);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        ValidateOpenApiDocument(responseBodyStream, document =>
        {
            Assert.Equal("OpenApiEndpointRouteBuilderExtensionsTests | v1", document.Info.Title);
            Assert.Equal("1.0.0", document.Info.Version);
        });
    }

    [Fact]
    public async Task MapOpenApi_ReturnsDefaultDocumentIfNoNameProvided()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));
        builder.MapOpenApi("/openapi.json");
        var context = new DefaultHttpContext();
        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;
        context.RequestServices = serviceProvider;
        var endpoint = builder.DataSources.First().Endpoints.First();

        // Act
        var requestDelegate = endpoint.RequestDelegate;
        await requestDelegate(context);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        ValidateOpenApiDocument(responseBodyStream, document =>
        {
            Assert.Equal("OpenApiEndpointRouteBuilderExtensionsTests | v1", document.Info.Title);
            Assert.Equal("1.0.0", document.Info.Version);
        });
    }

    [Fact]
    public async Task MapOpenApi_Returns404ForUnresolvedDocument()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));
        builder.MapOpenApi();
        var context = new DefaultHttpContext();
        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;
        context.RequestServices = serviceProvider;
        context.Request.RouteValues.Add("documentName", "v2");
        var endpoint = builder.DataSources.First().Endpoints.First();

        // Act
        var requestDelegate = endpoint.RequestDelegate;
        await requestDelegate(context);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Equal("No OpenAPI document with the name 'v2' was found.", Encoding.UTF8.GetString(responseBodyStream.ToArray()));
    }

    [Fact]
    public async Task MapOpenApi_ReturnsDocumentIfNameProvidedInQuery()
    {
        // Arrange
        var documentName = "v2";
        var hostEnvironment = new HostEnvironment() { ApplicationName = nameof(OpenApiEndpointRouteBuilderExtensionsTests) };
        var serviceProviderIsService = new ServiceProviderIsService();
        var serviceProvider = CreateServiceProvider(documentName);
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));
        builder.MapOpenApi("/openapi.json");
        var context = new DefaultHttpContext();
        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;
        context.RequestServices = serviceProvider;
        context.Request.QueryString = new QueryString($"?documentName={documentName}");
        var endpoint = builder.DataSources.First().Endpoints.First();

        // Act
        var requestDelegate = endpoint.RequestDelegate;
        await requestDelegate(context);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        ValidateOpenApiDocument(responseBodyStream, document =>
        {
            Assert.Equal($"OpenApiEndpointRouteBuilderExtensionsTests | {documentName}", document.Info.Title);
            Assert.Equal("1.0.0", document.Info.Version);
        });
    }

    private static void ValidateOpenApiDocument(MemoryStream documentStream, Action<OpenApiDocument> action)
    {
        var document = new OpenApiStringReader().Read(Encoding.UTF8.GetString(documentStream.ToArray()), out var diagnostic);
        Assert.Empty(diagnostic.Errors);
        action(document);
    }

    private static IServiceProvider CreateServiceProvider(string documentName = Microsoft.AspNetCore.OpenApi.OpenApiConstants.DefaultDocumentName)
    {
        var hostEnvironment = new HostEnvironment() { ApplicationName = nameof(OpenApiEndpointRouteBuilderExtensionsTests) };
        var serviceProviderIsService = new ServiceProviderIsService();
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IServiceProviderIsService>(serviceProviderIsService)
            .AddSingleton<IHostEnvironment>(hostEnvironment)
            .AddSingleton(CreateApiDescriptionGroupCollectionProvider())
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddOpenApi(documentName)
            .BuildServiceProvider();
        return serviceProvider;
    }
}
