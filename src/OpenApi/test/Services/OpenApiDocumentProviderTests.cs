using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.ApiDescriptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using static Microsoft.AspNetCore.OpenApi.Tests.OpenApiOperationGeneratorTests;

public class OpenApiDocumentProviderTests
{
    [Fact]
    public async Task GenerateAsync_ReturnsDocument()
    {
        // Arrange
        var documentName = "v1";
        var hostEnvironment = new HostEnvironment() { ApplicationName = nameof(OpenApiDocumentProviderTests) };
        var documentService = new OpenApiDocumentService(hostEnvironment);
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddOpenApi(documentName)
            .AddSingleton<IHostEnvironment>(hostEnvironment)
            .AddSingleton(documentService);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var documentProvider = new OpenApiDocumentProvider(serviceProvider);
        var stringWriter = new StringWriter();

        // Act
        await documentProvider.GenerateAsync(documentName, stringWriter);

        // Assert
        ValidateOpenApiDocument(stringWriter, document =>
        {
            Assert.Equal(hostEnvironment.ApplicationName, document.Info.Title);
            Assert.Equal("1.0.0", document.Info.Version);
        });
    }

    [Fact]
    public void GetDocumentNames_ReturnsAllRegisteredDocumentName()
    {
        // Arrange
        var hostEnvironment = new HostEnvironment() { ApplicationName = nameof(OpenApiDocumentProviderTests) };
        var documentService = new OpenApiDocumentService(hostEnvironment);
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddOpenApi("v2")
            .AddOpenApi("internal")
            .AddOpenApi("public")
            .AddOpenApi("v1")
            .AddSingleton<IHostEnvironment>(hostEnvironment)
            .AddSingleton(documentService);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var documentProvider = new OpenApiDocumentProvider(serviceProvider);

        // Act
        var documentNames = documentProvider.GetDocumentNames();

        // Assert
        Assert.Equal(4, documentNames.Count());
        Assert.Collection(documentNames,
            x => Assert.Equal("v2", x),
            x => Assert.Equal("internal", x),
            x => Assert.Equal("public", x),
            x => Assert.Equal("v1", x));
    }

    private static void ValidateOpenApiDocument(StringWriter stringWriter, Action<OpenApiDocument> action)
    {
        var document = new OpenApiStringReader().Read(stringWriter.ToString(), out var diagnostic);
        Assert.Empty(diagnostic.Errors);
        action(document);
    }
}
