// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.ApiDescriptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
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
    public async Task GenerateAsync_ShouldRetrieveOptionsInACaseInsensitiveManner()
    {
        // Arrange
        var documentName = "CaseSensitive";
        var serviceProvider = CreateServiceProvider(["casesensitive"], OpenApiSpecVersion.OpenApi2_0);
        var documentProvider = new OpenApiDocumentProvider(serviceProvider);
        var stringWriter = new StringWriter();

        // Act
        await documentProvider.GenerateAsync(documentName, stringWriter);

        // Assert
        var document = stringWriter.ToString();

        // When we generate an OpenAPI document, we use an OptionsMonitor to retrieve OpenAPI options which are stored with a key equal the requested document name.
        // This key is case-sensitive. If the document doesn't exist, the options monitor return a default instance, in which the OpenAPI version is set to v3.
        // This could cause bugs! You'd get your document, but depending on the casing you used in the document name you passed to the function, you'll receive different OpenAPI document versions.
        // We want to prevent this from happening. Therefore:
        // By setting up a v2 document on the "casesensitive" route and requesting it on "CaseSensitive",
        // we can test that the we've configured the options monitor to retrieve the options in a case-insensitive manner.
        // If it is case-sensitive, it would return a default instance with OpenAPI version v3, which would cause this test to fail!
        // However, if it would return the v2 instance, which was configured on the lowercase - case-insensitive - documentname, the test would pass!
        Assert.StartsWith("{\n  \"swagger\": \"2.0\"", document);
    }

    [Fact]
    public async Task GenerateAsync_ShouldRetrieveOpenApiDocumentServiceWithACaseInsensitiveKey()
    {
        // Arrange
        var documentName = "CaseSensitive";
        var serviceProvider = CreateServiceProvider(["casesensitive"]);
        var documentProvider = new OpenApiDocumentProvider(serviceProvider);
        var stringWriter = new StringWriter();

        // Act
        await documentProvider.GenerateAsync(documentName, stringWriter, OpenApiSpecVersion.OpenApi3_0);

        // Assert
        var document = stringWriter.ToString();

        // If the Document Service is retrieved with a non-existent (case-sensitive) key, it would throw:
        // https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.serviceproviderkeyedserviceextensions.getrequiredkeyedservice?view=net-9.0-pp

        // In this test's case, we're testing that the document service is retrieved with a case-insensitive key.
        // It's registered as "casesensitive" but we're passing in "CaseSensitive", which doesn't exist.
        // Therefore, if the test doesn't throw, we know it has passed correctly.
        // We still do a small check to validate the document, just in case. But the main test is that it doesn't throw.
        ValidateOpenApiDocument(stringWriter, _ => { });
        Assert.StartsWith("{\n  \"openapi\": \"3.0.4\"", document);
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
        var result = OpenApiDocument.Parse(stringWriter.ToString());
        Assert.Empty(result.Diagnostic.Errors);
        action(result.Document);
    }

    private static IServiceProvider CreateServiceProvider(string[] documentNames, OpenApiSpecVersion openApiSpecVersion = OpenApiSpecVersion.OpenApi3_1)
    {
        var hostEnvironment = new HostEnvironment() { ApplicationName = nameof(OpenApiDocumentProviderTests) };
        var serviceProviderIsService = new ServiceProviderIsService();
        var serviceCollection = new ServiceCollection()
            .AddSingleton<IServiceProviderIsService>(serviceProviderIsService)
            .AddSingleton<IHostEnvironment>(hostEnvironment)
            .AddSingleton(CreateApiDescriptionGroupCollectionProvider());
        foreach (var documentName in documentNames)
        {
            serviceCollection.AddOpenApi(documentName, x => x.OpenApiVersion = openApiSpecVersion);
        }
        var serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);
        return serviceProvider;
    }
}
