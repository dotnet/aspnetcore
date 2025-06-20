// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.WebView;

public class ComponentsWebViewServiceCollectionExtensionsTests
{
    [Fact]
    public void AddBlazorWebView_RegistersPersistentComponentStateServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBlazorWebView();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var persistenceManager = serviceProvider.GetService<ComponentStatePersistenceManager>();
        var persistentState = serviceProvider.GetService<PersistentComponentState>();

        Assert.NotNull(persistenceManager);
        Assert.NotNull(persistentState);
        Assert.Same(persistenceManager.State, persistentState);
    }

    [Fact]
    public void AddBlazorWebView_RegistersServicesAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBlazorWebView();

        // Assert
        var persistenceManagerDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ComponentStatePersistenceManager));
        var persistentStateDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(PersistentComponentState));

        Assert.NotNull(persistenceManagerDescriptor);
        Assert.NotNull(persistentStateDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, persistenceManagerDescriptor.Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, persistentStateDescriptor.Lifetime);
    }
}