// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Moq;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer.Configuration;

public class LocalApiDescriptorTests
{
    [Fact]
    public void LocalApiDescriptor_DefinesApiResources()
    {
        // Arrange
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.ApplicationName).Returns("Test");
        var descriptor = new IdentityServerJwtDescriptor(environment.Object);

        // Act
        var resources = descriptor.GetResourceDefinitions();

        // Assert
        var apiResource = Assert.Single(resources);
        Assert.Equal("TestAPI", apiResource.Key);
        Assert.NotNull(apiResource.Value);
        Assert.Equal(ApplicationProfiles.IdentityServerJwt, apiResource.Value.Profile);
    }
}
