// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.ApiDescriptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

public class OpenApiServiceCollectionExtensions
{
    [Fact]
    public void AddOpenApi_WithDocumentName_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var documentName = "v2";

        // Act
        var returnedServices = services.AddOpenApi(documentName);

        // Assert
        Assert.IsAssignableFrom<IServiceCollection>(returnedServices);
    }

    [Fact]
    public void AddOpenApi_WithDocumentName_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var documentName = "v2";

        // Act
        services.AddOpenApi(documentName);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.Contains(services, sd => sd.ServiceType == typeof(OpenApiSchemaService) && sd.Lifetime == ServiceLifetime.Singleton && (string)sd.ServiceKey == documentName);
        Assert.Contains(services, sd => sd.ServiceType == typeof(OpenApiDocumentService) && sd.Lifetime == ServiceLifetime.Singleton && (string)sd.ServiceKey == documentName);
        Assert.Contains(services, sd => sd.ServiceType == typeof(IDocumentProvider) && sd.Lifetime == ServiceLifetime.Singleton);
        var options = serviceProvider.GetRequiredService<IOptionsSnapshot<OpenApiOptions>>();
        var namedOption = options.Get(documentName);
        Assert.Equal(documentName, namedOption.DocumentName);
    }

    [Fact]
    public void AddOpenApi_WithDocumentNameAndConfigureOptions_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var documentName = "v2";

        // Act
        var returnedServices = services.AddOpenApi(documentName, options => { });

        // Assert
        Assert.IsAssignableFrom<IServiceCollection>(returnedServices);
    }

    [Fact]
    public void AddOpenApi_WithDocumentNameAndConfigureOptions_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var documentName = "v2";

        // Act
        services.AddOpenApi(documentName, options => { });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.Contains(services, sd => sd.ServiceType == typeof(OpenApiSchemaService) && sd.Lifetime == ServiceLifetime.Singleton && (string)sd.ServiceKey == documentName);
        Assert.Contains(services, sd => sd.ServiceType == typeof(OpenApiDocumentService) && sd.Lifetime == ServiceLifetime.Singleton && (string)sd.ServiceKey == documentName);
        Assert.Contains(services, sd => sd.ServiceType == typeof(IDocumentProvider) && sd.Lifetime == ServiceLifetime.Singleton);
        var options = serviceProvider.GetRequiredService<IOptionsSnapshot<OpenApiOptions>>();
        var namedOption = options.Get(documentName);
        Assert.Equal(documentName, namedOption.DocumentName);
    }

    [Fact]
    public void AddOpenApi_WithoutDocumentName_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returnedServices = services.AddOpenApi();

        // Assert
        Assert.IsAssignableFrom<IServiceCollection>(returnedServices);
    }

    [Fact]
    public void AddOpenApi_WithoutDocumentName_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var documentName = "v1";

        // Act
        services.AddOpenApi();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.Contains(services, sd => sd.ServiceType == typeof(OpenApiSchemaService) && sd.Lifetime == ServiceLifetime.Singleton && (string)sd.ServiceKey == documentName);
        Assert.Contains(services, sd => sd.ServiceType == typeof(OpenApiDocumentService) && sd.Lifetime == ServiceLifetime.Singleton && (string)sd.ServiceKey == documentName);
        Assert.Contains(services, sd => sd.ServiceType == typeof(IDocumentProvider) && sd.Lifetime == ServiceLifetime.Singleton);
        var options = serviceProvider.GetRequiredService<IOptionsSnapshot<OpenApiOptions>>();
        var namedOption = options.Get(documentName);
        Assert.Equal(documentName, namedOption.DocumentName);
    }

    [Fact]
    public void AddOpenApi_WithConfigureOptions_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returnedServices = services.AddOpenApi(options => { });

        // Assert
        Assert.IsAssignableFrom<IServiceCollection>(returnedServices);
    }

    [Fact]
    public void AddOpenApi_WithConfigureOptions_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var documentName = "v1";

        // Act
        services.AddOpenApi(options => { });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.Contains(services, sd => sd.ServiceType == typeof(OpenApiSchemaService) && sd.Lifetime == ServiceLifetime.Singleton && (string)sd.ServiceKey == documentName);
        Assert.Contains(services, sd => sd.ServiceType == typeof(OpenApiDocumentService) && sd.Lifetime == ServiceLifetime.Singleton && (string)sd.ServiceKey == documentName);
        Assert.Contains(services, sd => sd.ServiceType == typeof(IDocumentProvider) && sd.Lifetime == ServiceLifetime.Singleton);
        var options = serviceProvider.GetRequiredService<IOptionsSnapshot<OpenApiOptions>>();
        var namedOption = options.Get(documentName);
        Assert.Equal(documentName, namedOption.DocumentName);
    }

    [Fact]
    public void AddOpenApi_WithDuplicateDocumentNames_UsesLastRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var documentName = "v2";

        // Act
        services
        .AddOpenApi(documentName, options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi2_0)
        .AddOpenApi(documentName, options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.Contains(services, sd => sd.ServiceType == typeof(OpenApiSchemaService) && sd.Lifetime == ServiceLifetime.Singleton && (string)sd.ServiceKey == documentName);
        Assert.Contains(services, sd => sd.ServiceType == typeof(OpenApiDocumentService) && sd.Lifetime == ServiceLifetime.Singleton && (string)sd.ServiceKey == documentName);
        Assert.Contains(services, sd => sd.ServiceType == typeof(IDocumentProvider) && sd.Lifetime == ServiceLifetime.Singleton);
        var options = serviceProvider.GetRequiredService<IOptionsSnapshot<OpenApiOptions>>();
        var namedOption = options.Get(documentName);
        Assert.Equal(documentName, namedOption.DocumentName);
        // Verify last registration is used
        Assert.Equal(OpenApiSpecVersion.OpenApi3_0, namedOption.OpenApiVersion);
    }

    [Fact]
    public void AddOpenApi_WithDuplicateDocumentNames_UsesLastRegistration_ValidateOptionsOverride()
    {
        // Arrange
        var services = new ServiceCollection();
        var documentName = "v2";

        // Act
        services
        .AddOpenApi(documentName, options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi2_0)
        .AddOpenApi(documentName);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.Contains(services, sd => sd.ServiceType == typeof(OpenApiSchemaService) && sd.Lifetime == ServiceLifetime.Singleton && (string)sd.ServiceKey == documentName);
        Assert.Contains(services, sd => sd.ServiceType == typeof(OpenApiDocumentService) && sd.Lifetime == ServiceLifetime.Singleton && (string)sd.ServiceKey == documentName);
        Assert.Contains(services, sd => sd.ServiceType == typeof(IDocumentProvider) && sd.Lifetime == ServiceLifetime.Singleton);
        var options = serviceProvider.GetRequiredService<IOptionsSnapshot<OpenApiOptions>>();
        var namedOption = options.Get(documentName);
        Assert.Equal(documentName, namedOption.DocumentName);
        Assert.Equal(OpenApiSpecVersion.OpenApi2_0, namedOption.OpenApiVersion);
    }

    [Fact]
    public void AddOpenApi_WithDefaultDocumentName_RegistersIOpenApiDocumentProviderInterface()
    {
        // Arrange
        var services = new ServiceCollection();
        // Include dependencies for OpenApiDocumentService
        services.AddSingleton<IHostEnvironment>(new HostingEnvironment
        {
            EnvironmentName = Environments.Development,
            ApplicationName = "Test Application"
        });
        services.AddLogging();
        services.AddRouting();

        // Act
        services.AddOpenApi();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var documentProvider = serviceProvider.GetRequiredKeyedService<IOpenApiDocumentProvider>(Microsoft.AspNetCore.OpenApi.OpenApiConstants.DefaultDocumentName);
        Assert.NotNull(documentProvider);
        Assert.IsType<OpenApiDocumentService>(documentProvider);
    }

    [Fact]
    public void AddOpenApi_WithCustomDocumentName_RegistersIOpenApiDocumentProviderInterface()
    {
        // Arrange
        var services = new ServiceCollection();
        // Include dependencies for OpenApiDocumentService
        services.AddSingleton<IHostEnvironment>(new HostingEnvironment
        {
            EnvironmentName = Environments.Development,
            ApplicationName = "Test Application"
        });
        services.AddLogging();
        services.AddRouting();
        var documentName = "v1";

        // Act
        services.AddOpenApi(documentName);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var documentProvider = serviceProvider.GetRequiredKeyedService<IOpenApiDocumentProvider>(documentName.ToLowerInvariant());
        Assert.NotNull(documentProvider);
        Assert.IsType<OpenApiDocumentService>(documentProvider);
    }

    [Fact]
    public async Task GetOpenApiDocumentAsync_ReturnsDocument()
    {
        // Arrange
        var services = new ServiceCollection();
        // Include dependencies for OpenApiDocumentService
        services.AddSingleton<IHostEnvironment>(new HostingEnvironment
        {
            EnvironmentName = Environments.Development,
            ApplicationName = "Test Application"
        });
        services.AddLogging();
        services.AddRouting();

        var documentName = "v1";
        services.AddOpenApi(documentName);
        var serviceProvider = services.BuildServiceProvider();
        var documentProvider = serviceProvider.GetRequiredKeyedService<IOpenApiDocumentProvider>(documentName.ToLowerInvariant());

        // Act
        var document = await documentProvider.GetOpenApiDocumentAsync();

        // Assert
        Assert.NotNull(document);
        Assert.IsType<OpenApiDocument>(document);

        // Verify basic document structure
        Assert.NotNull(document.Info);
        Assert.Equal($"Test Application | {documentName.ToLowerInvariant()}", document.Info.Title);
        Assert.Equal("1.0.0", document.Info.Version);
    }

    [Fact]
    public async Task GetOpenApiDocumentAsync_HandlesCancellation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new HostingEnvironment
        {
            EnvironmentName = Environments.Development,
            ApplicationName = "Test Application"
        });
        services.AddLogging();
        services.AddRouting();
        var documentName = "v1";
        services.AddOpenApi(documentName);
        var serviceProvider = services.BuildServiceProvider();
        var documentProvider = serviceProvider.GetRequiredKeyedService<IOpenApiDocumentProvider>(documentName.ToLowerInvariant());

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await documentProvider.GetOpenApiDocumentAsync(cts.Token);
        });
    }
}
