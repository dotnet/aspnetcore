// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http.Tests;

public class HttpServiceCollectionExtensionsTests
{
    [Fact]
    public void AddHttpContextAccessor_AddsWithCorrectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddHttpContextAccessor();

        // Assert
        var descriptor = services[0];
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(HttpContextAccessor), descriptor.ImplementationType);
    }

    [Fact]
    public void AddHttpContextAccessor_ThrowsWithoutServices()
    {
        Assert.Throws<ArgumentNullException>("services", () => HttpServiceCollectionExtensions.AddHttpContextAccessor(null));
    }
}
