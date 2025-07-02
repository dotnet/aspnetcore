// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace Microsoft.AspNetCore.Components;

public class ScenarioBasedPersistentComponentStateTest
{
    [Fact]
    public void WebPersistenceScenario_Properties_SetCorrectly()
    {
        // Arrange & Act
        var enhancedNavScenario = WebPersistenceScenario.EnhancedNavigation();
        var prerenderingScenario = WebPersistenceScenario.Prerendering();
        var reconnectionScenario = WebPersistenceScenario.Reconnection();

        // Assert
        Assert.Equal(WebPersistenceScenario.ScenarioType.EnhancedNavigation, enhancedNavScenario.Type);
        Assert.True(((IPersistentComponentStateScenario)enhancedNavScenario).IsRecurring);
        
        Assert.Equal(WebPersistenceScenario.ScenarioType.Prerendering, prerenderingScenario.Type);
        Assert.False(((IPersistentComponentStateScenario)prerenderingScenario).IsRecurring);
        
        Assert.Equal(WebPersistenceScenario.ScenarioType.Reconnection, reconnectionScenario.Type);
        Assert.False(((IPersistentComponentStateScenario)reconnectionScenario).IsRecurring);
    }

    [Fact]
    public void WebPersistenceScenario_EnhancedNavigation_WithRenderMode()
    {
        // Arrange
        var serverRenderMode = new InteractiveServerRenderMode();
        var wasmRenderMode = new InteractiveWebAssemblyRenderMode();

        // Act
        var serverScenario = WebPersistenceScenario.EnhancedNavigation(serverRenderMode);
        var wasmScenario = WebPersistenceScenario.EnhancedNavigation(wasmRenderMode);
        var defaultScenario = WebPersistenceScenario.EnhancedNavigation();

        // Assert
        Assert.Equal(serverRenderMode, serverScenario.RenderMode);
        Assert.Equal(wasmRenderMode, wasmScenario.RenderMode);
        Assert.Null(defaultScenario.RenderMode);
    }

    [Fact]
    public void WebPersistenceScenario_Equals_WorksCorrectly()
    {
        // Arrange
        var scenario1 = WebPersistenceScenario.EnhancedNavigation();
        var scenario2 = WebPersistenceScenario.EnhancedNavigation();
        var scenario3 = WebPersistenceScenario.Prerendering();

        // Act & Assert
        Assert.True(scenario1.Equals(scenario2));
        Assert.False(scenario1.Equals(scenario3));
        Assert.False(scenario1.Equals(null));
    }

    [Fact]
    public void WebPersistenceScenario_GetHashCode_WorksCorrectly()
    {
        // Arrange
        var scenario1 = WebPersistenceScenario.EnhancedNavigation();
        var scenario2 = WebPersistenceScenario.EnhancedNavigation();
        var scenario3 = WebPersistenceScenario.Prerendering();

        // Act & Assert
        Assert.Equal(scenario1.GetHashCode(), scenario2.GetHashCode());
        Assert.NotEqual(scenario1.GetHashCode(), scenario3.GetHashCode());
    }

    [Fact]
    public void WebPersistenceFilter_ShouldRestore_WorksCorrectly()
    {
        // Arrange
        var enhancedNavScenario = WebPersistenceScenario.EnhancedNavigation();
        var prerenderingScenario = WebPersistenceScenario.Prerendering();
        var reconnectionScenario = WebPersistenceScenario.Reconnection();

        var enhancedNavFilter = WebPersistenceFilter.EnhancedNavigation;
        var prerenderingFilter = WebPersistenceFilter.Prerendering;
        var reconnectionFilter = WebPersistenceFilter.Reconnection;

        // Act & Assert
        Assert.True(enhancedNavFilter.ShouldRestore(enhancedNavScenario));
        Assert.False(enhancedNavFilter.ShouldRestore(prerenderingScenario));
        Assert.False(enhancedNavFilter.ShouldRestore(reconnectionScenario));

        Assert.False(prerenderingFilter.ShouldRestore(enhancedNavScenario));
        Assert.True(prerenderingFilter.ShouldRestore(prerenderingScenario));
        Assert.False(prerenderingFilter.ShouldRestore(reconnectionScenario));

        Assert.False(reconnectionFilter.ShouldRestore(enhancedNavScenario));
        Assert.False(reconnectionFilter.ShouldRestore(prerenderingScenario));
        Assert.True(reconnectionFilter.ShouldRestore(reconnectionScenario));
    }

    [Fact]
    public void FilterAttributes_ShouldRestore_WorksCorrectly()
    {
        // Arrange
        var enhancedNavScenario = WebPersistenceScenario.EnhancedNavigation();
        var prerenderingScenario = WebPersistenceScenario.Prerendering();
        var reconnectionScenario = WebPersistenceScenario.Reconnection();

        var enhancedNavFilter = new UpdateStateOnEnhancedNavigationAttribute();
        var prerenderingFilter = new RestoreStateOnPrerenderingAttribute();
        var reconnectionFilter = new RestoreStateOnReconnectionAttribute();

        // Act & Assert
        Assert.True(enhancedNavFilter.ShouldRestore(enhancedNavScenario));
        Assert.False(enhancedNavFilter.ShouldRestore(prerenderingScenario));
        Assert.False(enhancedNavFilter.ShouldRestore(reconnectionScenario));

        Assert.False(prerenderingFilter.ShouldRestore(enhancedNavScenario));
        Assert.True(prerenderingFilter.ShouldRestore(prerenderingScenario));
        Assert.False(prerenderingFilter.ShouldRestore(reconnectionScenario));

        Assert.False(reconnectionFilter.ShouldRestore(enhancedNavScenario));
        Assert.False(reconnectionFilter.ShouldRestore(prerenderingScenario));
        Assert.True(reconnectionFilter.ShouldRestore(reconnectionScenario));
    }
}