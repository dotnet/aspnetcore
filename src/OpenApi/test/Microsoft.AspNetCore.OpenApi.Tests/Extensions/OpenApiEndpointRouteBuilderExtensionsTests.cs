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
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Reader;
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
        var endpoint = builder.DataSources.First().Endpoints[0];

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
        var endpoint = builder.DataSources.First().Endpoints[0];

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
        var endpoint = builder.DataSources.First().Endpoints[0];

        // Act
        var requestDelegate = endpoint.RequestDelegate;
        await requestDelegate(context);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Equal("No OpenAPI document with the name 'v2' was found.", Encoding.UTF8.GetString(responseBodyStream.ToArray()));
    }

    [Theory]
    [InlineData("CaseSensitive", "casesensitive")]
    [InlineData("casesensitive", "CaseSensitive")]
    public async Task MapOpenApi_ReturnsDocumentWhenPathIsCaseSensitive(string registeredDocumentName, string requestedDocumentName)
    {
        // Arrange
        var serviceProvider = CreateServiceProvider(registeredDocumentName);
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));
        builder.MapOpenApi("/openapi/{documentName}.json");
        var context = new DefaultHttpContext();
        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;
        context.RequestServices = serviceProvider;
        context.Request.RouteValues.Add("documentName", requestedDocumentName);
        var endpoint = builder.DataSources.First().Endpoints[0];

        // Act
        var requestDelegate = endpoint.RequestDelegate;
        await requestDelegate(context);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task MapOpenApi_ShouldRetrieveOptionsInACaseInsensitiveManner()
    {
        // Arrange
        var hostEnvironment = new HostEnvironment() { ApplicationName = nameof(OpenApiEndpointRouteBuilderExtensionsTests) };
        var serviceProviderIsService = new ServiceProviderIsService();
        var serviceProvider = CreateServiceProvider("casesensitive", OpenApiSpecVersion.OpenApi2_0);
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));
        builder.MapOpenApi("/openapi/{documentName}.json");
        var context = new DefaultHttpContext();
        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;
        context.RequestServices = serviceProvider;
        context.Request.RouteValues.Add("documentName", "CaseSensitive");
        var endpoint = builder.DataSources.First().Endpoints[0];

        // Act
        var requestDelegate = endpoint.RequestDelegate;
        await requestDelegate(context);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        var responseString = Encoding.UTF8.GetString(responseBodyStream.ToArray());

        // When we receive an OpenAPI document, we use an OptionsMonitor to retrieve OpenAPI options which are stored with a key equal the requested document name.
        // This key is case-sensitive. If the document doesn't exist, the options monitor return a default instance, in which the OpenAPI version is set to v3.
        // This could cause bugs! You'd get your document, but depending on the casing you used in the document name you passed to the function, you'll receive different OpenAPI document versions.
        // We want to prevent this from happening. Therefore:
        // By setting up a v2 document on the "casesensitive" route and requesting it on "CaseSensitive",
        // we can test that the we've configured the options monitor to retrieve the options in a case-insensitive manner.
        // If it is case-sensitive, it would return a default instance with OpenAPI version v3, which would cause this test to fail!
        // However, if it would return the v2 instance, which was configured on the lowercase - case-insensitive - documentname, the test would pass!
        // For more info, see OpenApiEndpointRouteBuilderExtensions.cs
        Assert.StartsWith("{\n  \"swagger\": \"2.0\"", responseString);
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
        var endpoint = builder.DataSources.First().Endpoints[0];

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
        Assert.Empty(result.Diagnostic.Errors);
        action(result.Document);
    }

    private static IServiceProvider CreateServiceProvider(string documentName = Microsoft.AspNetCore.OpenApi.OpenApiConstants.DefaultDocumentName, OpenApiSpecVersion openApiSpecVersion = OpenApiSpecVersion.OpenApi3_1)
    {
        var hostEnvironment = new HostEnvironment() { ApplicationName = nameof(OpenApiEndpointRouteBuilderExtensionsTests) };
        var serviceProviderIsService = new ServiceProviderIsService();
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IServiceProviderIsService>(serviceProviderIsService)
            .AddSingleton<IHostEnvironment>(hostEnvironment)
            .AddSingleton(CreateApiDescriptionGroupCollectionProvider())
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddOpenApi(documentName, x => x.OpenApiVersion = openApiSpecVersion)
            .BuildServiceProvider();
        return serviceProvider;
    }
}
