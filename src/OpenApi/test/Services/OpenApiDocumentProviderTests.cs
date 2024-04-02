// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.ApiDescriptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using static Microsoft.AspNetCore.OpenApi.Tests.OpenApiOperationGeneratorTests;

public class OpenApiDocumentProviderTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public async Task GenerateAsync_ReturnsDocument()
    {
        // Arrange
        var documentName = "v1";
        var serviceProvider = CreateServiceProvider([documentName]);
        var documentProvider = new OpenApiDocumentProvider(serviceProvider);
        var stringWriter = new StringWriter();

        // Act
        await documentProvider.GenerateAsync(documentName, stringWriter);

        // Assert
        ValidateOpenApiDocument(stringWriter, document =>
        {
            Assert.Equal($"{nameof(OpenApiDocumentProviderTests)} | {documentName}", document.Info.Title);
            Assert.Equal("1.0.0", document.Info.Version);
        });
    }

    [Fact]
    public void GetDocumentNames_ReturnsAllRegisteredDocumentName()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider(["v2", "internal", "public", "v1"]);
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

    private static IServiceProvider CreateServiceProvider(string[] documentNames)
    {
        var hostEnvironment = new HostEnvironment() { ApplicationName = nameof(OpenApiDocumentProviderTests) };
        var serviceProviderIsService = new ServiceProviderIsService();
        var serviceCollection = new ServiceCollection()
            .AddSingleton<IServiceProviderIsService>(serviceProviderIsService)
            .AddSingleton<IHostEnvironment>(hostEnvironment)
            .AddSingleton(CreateApiDescriptionGroupCollectionProvider());
        foreach (var documentName in documentNames)
        {
            serviceCollection.AddOpenApi(documentName);
        }
        var serviceProvider = serviceCollection.BuildServiceProvider();
        return serviceProvider;
    }
}
