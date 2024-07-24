// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;
using Moq;

public partial class OpenApiDocumentServiceTests
{
    [Fact]
    public void GetOpenApiInfo_RespectsHostEnvironmentName()
    {
        // Arrange
        var hostEnvironment = new HostingEnvironment
        {
            ApplicationName = "TestApplication"
        };
        var docService = new OpenApiDocumentService(
            "v1",
            new Mock<IApiDescriptionGroupCollectionProvider>().Object,
            hostEnvironment,
            new Mock<IOptionsMonitor<OpenApiOptions>>().Object,
            new Mock<IKeyedServiceProvider>().Object,
            new OpenApiTestServer());

        // Act
        var info = docService.GetOpenApiInfo();

        // Assert
        Assert.Equal("TestApplication | v1", info.Title);
    }

    [Fact]
    public void GetOpenApiInfo_RespectsDocumentName()
    {
        // Arrange
        var hostEnvironment = new HostingEnvironment
        {
            ApplicationName = "TestApplication"
        };
        var docService = new OpenApiDocumentService(
            "v2",
            new Mock<IApiDescriptionGroupCollectionProvider>().Object,
            hostEnvironment,
            new Mock<IOptionsMonitor<OpenApiOptions>>().Object,
            new Mock<IKeyedServiceProvider>().Object,
            new OpenApiTestServer());

        // Act
        var info = docService.GetOpenApiInfo();

        // Assert
        Assert.Equal("TestApplication | v2", info.Title);
    }
}
