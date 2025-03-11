// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;
using Moq;

public partial class OpenApiDocumentServiceTests
{
    [Theory]
    [InlineData("Development", "localhost:5001", "", "http", "http://localhost:5001/")]
    [InlineData("Development", "example.com", "/api", "https", "https://example.com/api")]
    [InlineData("Staging", "localhost:5002", "/v1", "http", "http://localhost:5002/v1")]
    [InlineData("Staging", "api.example.com", "/base/path", "https", "https://api.example.com/base/path")]
    [InlineData("Development", "localhost", "/", "http", "http://localhost/")]
    public void GetOpenApiServers_FavorsHttpContextRequestOverServerAddress(string environment, string host, string pathBase, string scheme, string expectedUri)
    {
        // Arrange
        var hostEnvironment = new HostingEnvironment
        {
            ApplicationName = "TestApplication",
            EnvironmentName = environment
        };
        var docService = new OpenApiDocumentService(
            "v1",
            new Mock<IApiDescriptionGroupCollectionProvider>().Object,
            hostEnvironment,
            GetMockOptionsMonitor(),
            new Mock<IKeyedServiceProvider>().Object,
            new OpenApiTestServer(["http://localhost:5000"]));
        var httpContext = new DefaultHttpContext()
        {
            Request =
            {
                Host = new HostString(host),
                PathBase = pathBase,
                Scheme = scheme

            }
        };

        // Act
        var servers = docService.GetOpenApiServers(httpContext.Request);

        // Assert
        Assert.Contains(expectedUri, servers.Select(s => s.Url));
    }

    [Fact]
    public void GetOpenApiServers_HandlesServerAddressFeatureWithValues()
    {
        // Arrange
        var hostEnvironment = new HostingEnvironment
        {
            ApplicationName = "TestApplication",
            EnvironmentName = "Development"
        };
        var docService = new OpenApiDocumentService(
            "v1",
            new Mock<IApiDescriptionGroupCollectionProvider>().Object,
            hostEnvironment,
            GetMockOptionsMonitor(),
            new Mock<IKeyedServiceProvider>().Object,
            new OpenApiTestServer(["http://localhost:5000"]));

        // Act
        var servers = docService.GetOpenApiServers();

        // Assert
        Assert.Contains("http://localhost:5000", servers.Select(s => s.Url));
    }

    [Fact]
    public void GetOpenApiServers_HandlesServerAddressFeatureWithMultipleValues()
    {
        // Arrange
        var hostEnvironment = new HostingEnvironment
        {
            ApplicationName = "TestApplication",
            EnvironmentName = "Development"
        };
        var docService = new OpenApiDocumentService(
            "v1",
            new Mock<IApiDescriptionGroupCollectionProvider>().Object,
            hostEnvironment,
            GetMockOptionsMonitor(),
            new Mock<IKeyedServiceProvider>().Object,
            new OpenApiTestServer(["http://localhost:5000", "http://localhost:5002"]));

        // Act
        var servers = docService.GetOpenApiServers();

        // Assert
        Assert.Contains("http://localhost:5000", servers.Select(s => s.Url));
        Assert.Contains("http://localhost:5002", servers.Select(s => s.Url));
    }

    [Fact]
    public void GetOpenApiServers_HandlesNonDevelopmentEnvironment()
    {
        // Arrange
        var hostEnvironment = new HostingEnvironment
        {
            ApplicationName = "TestApplication",
            EnvironmentName = "Production"
        };
        var docService = new OpenApiDocumentService(
            "v1",
            new Mock<IApiDescriptionGroupCollectionProvider>().Object,
            hostEnvironment,
            GetMockOptionsMonitor(),
            new Mock<IKeyedServiceProvider>().Object,
            new OpenApiTestServer(["http://localhost:5000"]));

        // Act
        var servers = docService.GetOpenApiServers();

        // Assert
        Assert.Empty(servers);
    }

    [Fact]
    public void GetOpenApiServers_HandlesServerAddressFeatureWithNoValues()
    {
        // Arrange
        var hostEnvironment = new HostingEnvironment
        {
            ApplicationName = "TestApplication",
            EnvironmentName = "Development"
        };
        var docService = new OpenApiDocumentService(
            "v2",
            new Mock<IApiDescriptionGroupCollectionProvider>().Object,
            hostEnvironment,
            GetMockOptionsMonitor(),
            new Mock<IKeyedServiceProvider>().Object,
            new OpenApiTestServer());

        // Act
        var servers = docService.GetOpenApiServers();

        // Assert
        Assert.Empty(servers);
    }
}
