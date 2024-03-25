using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Routing;
using static Microsoft.AspNetCore.OpenApi.Tests.OpenApiOperationGeneratorTests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using System.Text;

public class OpenApiEndpointRouteBuilderExtensionsTests
{
    [Fact]
    public void MapOpenApi_ReturnsEndpointConventionBuilder()
    {
        // Arrange
        var hostEnvironment = new HostEnvironment() { ApplicationName = nameof(OpenApiEndpointRouteBuilderExtensionsTests) };
        var serviceProviderIsService = new ServiceProviderIsService();
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IServiceProviderIsService>(serviceProviderIsService)
            .AddSingleton<IHostEnvironment>(hostEnvironment)
            .AddOpenApi()
            .BuildServiceProvider();
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
        var hostEnvironment = new HostEnvironment() { ApplicationName = nameof(OpenApiEndpointRouteBuilderExtensionsTests) };
        var serviceProviderIsService = new ServiceProviderIsService();
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IServiceProviderIsService>(serviceProviderIsService)
            .AddSingleton<IHostEnvironment>(hostEnvironment)
            .AddOpenApi()
            .BuildServiceProvider();
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
        var hostEnvironment = new HostEnvironment() { ApplicationName = nameof(OpenApiEndpointRouteBuilderExtensionsTests) };
        var serviceProviderIsService = new ServiceProviderIsService();
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IServiceProviderIsService>(serviceProviderIsService)
            .AddSingleton<IHostEnvironment>(hostEnvironment)
            .AddOpenApi()
            .BuildServiceProvider();
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
            Assert.Equal("1.0.0", document.Info.Version);
        });
    }

    private static void ValidateOpenApiDocument(MemoryStream documentStream, Action<OpenApiDocument> action)
    {
        var document = new OpenApiStringReader().Read(Encoding.UTF8.GetString(documentStream.ToArray()), out var diagnostic);
        Assert.Empty(diagnostic.Errors);
        action(document);
    }
}
