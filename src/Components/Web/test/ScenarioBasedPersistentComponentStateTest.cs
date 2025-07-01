// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace Microsoft.AspNetCore.Components;

public class ScenarioBasedPersistentComponentStateTest
{
    [Fact]
    public void WebPersistenceContext_Properties_SetCorrectly()
    {
        // Arrange & Act
        var enhancedNavContext = new WebPersistenceContext(WebPersistenceReason.EnhancedNavigation);
        var prerenderingContext = new WebPersistenceContext(WebPersistenceReason.Prerendering);
        var reconnectionContext = new WebPersistenceContext(WebPersistenceReason.Reconnection);

        // Assert
        Assert.Equal(WebPersistenceReason.EnhancedNavigation, enhancedNavContext.Reason);
        Assert.True(enhancedNavContext.IsRecurring);
        
        Assert.Equal(WebPersistenceReason.Prerendering, prerenderingContext.Reason);
        Assert.False(prerenderingContext.IsRecurring);
        
        Assert.Equal(WebPersistenceReason.Reconnection, reconnectionContext.Reason);
        Assert.False(reconnectionContext.IsRecurring);
    }

    [Fact]
    public void WebPersistenceContext_StaticProperties_ReturnCorrectInstances()
    {
        // Act
        var enhancedNav = WebPersistenceContext.EnhancedNavigation;
        var prerendering = WebPersistenceContext.Prerendering;
        var reconnection = WebPersistenceContext.Reconnection;

        // Assert
        Assert.Equal(WebPersistenceReason.EnhancedNavigation, enhancedNav.Reason);
        Assert.Equal(WebPersistenceReason.Prerendering, prerendering.Reason);
        Assert.Equal(WebPersistenceReason.Reconnection, reconnection.Reason);
    }

    [Fact]
    public void WebPersistenceContext_Equals_WorksCorrectly()
    {
        // Arrange
        var context1 = new WebPersistenceContext(WebPersistenceReason.EnhancedNavigation);
        var context2 = new WebPersistenceContext(WebPersistenceReason.EnhancedNavigation);
        var context3 = new WebPersistenceContext(WebPersistenceReason.Prerendering);

        // Act & Assert
        Assert.True(context1.Equals(context2));
        Assert.False(context1.Equals(context3));
        Assert.False(context1.Equals(null));
    }

    [Fact]
    public void WebPersistenceContext_GetHashCode_WorksCorrectly()
    {
        // Arrange
        var context1 = new WebPersistenceContext(WebPersistenceReason.EnhancedNavigation);
        var context2 = new WebPersistenceContext(WebPersistenceReason.EnhancedNavigation);
        var context3 = new WebPersistenceContext(WebPersistenceReason.Prerendering);

        // Act & Assert
        Assert.Equal(context1.GetHashCode(), context2.GetHashCode());
        Assert.NotEqual(context1.GetHashCode(), context3.GetHashCode());
    }

    [Fact]
    public void FilterAttributes_ShouldRestore_WorksCorrectly()
    {
        // Arrange
        var enhancedNavContext = new WebPersistenceContext(WebPersistenceReason.EnhancedNavigation, new InteractiveServerRenderMode());
        var prerenderingContext = new WebPersistenceContext(WebPersistenceReason.Prerendering, new InteractiveServerRenderMode());
        var reconnectionContext = new WebPersistenceContext(WebPersistenceReason.Reconnection, new InteractiveServerRenderMode());

        var enhancedNavFilter = new UpdateStateOnEnhancedNavigationAttribute();
        var prerenderingFilter = new RestoreStateOnPrerenderingAttribute();
        var reconnectionFilter = new RestoreStateOnReconnectionAttribute();

        // Act & Assert
        Assert.True(enhancedNavFilter.ShouldRestore(enhancedNavContext));
        Assert.False(enhancedNavFilter.ShouldRestore(prerenderingContext));
        Assert.False(enhancedNavFilter.ShouldRestore(reconnectionContext));

        Assert.False(prerenderingFilter.ShouldRestore(enhancedNavContext));
        Assert.True(prerenderingFilter.ShouldRestore(prerenderingContext));
        Assert.False(prerenderingFilter.ShouldRestore(reconnectionContext));

        Assert.False(reconnectionFilter.ShouldRestore(enhancedNavContext));
        Assert.False(reconnectionFilter.ShouldRestore(prerenderingContext));
        Assert.True(reconnectionFilter.ShouldRestore(reconnectionContext));
    }
}