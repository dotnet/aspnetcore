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
using Microsoft.OpenApi.Reader;

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

    [Theory]
    [InlineData("/custom/{documentName}/openapi.json")]
    [InlineData("/custom/{documentName}/openapi.yaml")]
    [InlineData("/custom/{documentName}/openapi.yml")]
    public void MapOpenApi_SupportsCustomizingPath(string expectedPath)
    {
        // Arrange
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
        await ValidateOpenApiDocumentAsync(responseBodyStream, document =>
        {
            Assert.Equal("OpenApiEndpointRouteBuilderExtensionsTests | v1", document.Info.Title);
            Assert.Equal("1.0.0", document.Info.Version);
        });
    }

    [Theory]
    [InlineData("/openapi.json", "application/json;charset=utf-8", false)]
    [InlineData("/openapi.toml", "application/json;charset=utf-8", false)]
    [InlineData("/openapi.yaml", "text/plain+yaml;charset=utf-8", true)]
    [InlineData("/openapi.yml", "text/plain+yaml;charset=utf-8", true)]
    public async Task MapOpenApi_ReturnsDefaultDocumentIfNoNameProvided(string expectedPath, string expectedContentType, bool isYaml)
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));
        builder.MapOpenApi(expectedPath);
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
        Assert.Equal(expectedContentType, context.Response.ContentType);
        var responseString = Encoding.UTF8.GetString(responseBodyStream.ToArray());
        // String check to validate that generated document starts with YAML syntax
        Assert.Equal(isYaml, responseString.StartsWith("openapi: '3.1.1'", StringComparison.OrdinalIgnoreCase));
        responseBodyStream.Position = 0;
        await ValidateOpenApiDocumentAsync(responseBodyStream, document =>
        {
            Assert.Equal("OpenApiEndpointRouteBuilderExtensionsTests | v1", document.Info.Title);
            Assert.Equal("1.0.0", document.Info.Version);
        }, isYaml ? "yaml" : "json");
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

    [Theory]
    [InlineData("/openapi.json", "application/json;charset=utf-8", false)]
    [InlineData("/openapi.yaml", "text/plain+yaml;charset=utf-8", true)]
    [InlineData("/openapi.yml", "text/plain+yaml;charset=utf-8", true)]
    public async Task MapOpenApi_ReturnsDocumentIfNameProvidedInQuery(string expectedPath, string expectedContentType, bool isYaml)
    {
        // Arrange
        var documentName = "v2";
        var hostEnvironment = new HostEnvironment() { ApplicationName = nameof(OpenApiEndpointRouteBuilderExtensionsTests) };
        var serviceProviderIsService = new ServiceProviderIsService();
        var serviceProvider = CreateServiceProvider(documentName);
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));
        builder.MapOpenApi(expectedPath);
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
        Assert.Equal(expectedContentType, context.Response.ContentType);
        var responseString = Encoding.UTF8.GetString(responseBodyStream.ToArray());
        // String check to validate that generated document starts with YAML syntax
        Assert.Equal(isYaml, responseString.StartsWith("openapi: '3.1.1'", StringComparison.OrdinalIgnoreCase));
        responseBodyStream.Position = 0;
        await ValidateOpenApiDocumentAsync(responseBodyStream, document =>
        {
            Assert.Equal($"OpenApiEndpointRouteBuilderExtensionsTests | {documentName}", document.Info.Title);
            Assert.Equal("1.0.0", document.Info.Version);
        }, isYaml ? "yaml" : "json");
    }

    private static async Task ValidateOpenApiDocumentAsync(MemoryStream documentStream, Action<OpenApiDocument> action, string format = "json")
    {
        documentStream.Position = 0;
        OpenApiReaderRegistry.RegisterReader(OpenApiConstants.Yaml, new OpenApiYamlReader());
        var result = await OpenApiDocument.LoadAsync(documentStream, format);
        Assert.Empty(result.OpenApiDiagnostic.Errors);
        action(result.OpenApiDocument);
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
