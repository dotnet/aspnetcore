// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Xunit;

namespace Microsoft.AspNetCore.Components.Web;

public class PersistenceReasonFiltersTest
{
    [Fact]
    public void PersistOnPrerenderingFilter_AllowsByDefault()
    {
        // Arrange
        var filter = new PersistOnPrerenderingFilter();
        var reason = new PersistOnPrerendering();

        // Act
        var result = filter.ShouldPersist(reason);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void PersistOnPrerenderingFilter_CanBlock()
    {
        // Arrange
        var filter = new PersistOnPrerenderingFilter(persist: false);
        var reason = new PersistOnPrerendering();

        // Act
        var result = filter.ShouldPersist(reason);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void PersistOnEnhancedNavigationFilter_AllowsByDefault()
    {
        // Arrange
        var filter = new PersistOnEnhancedNavigationFilter();
        var reason = new PersistOnEnhancedNavigation();

        // Act
        var result = filter.ShouldPersist(reason);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void PersistOnEnhancedNavigationFilter_DoesNotMatchDifferentReason()
    {
        // Arrange
        var filter = new PersistOnEnhancedNavigationFilter();
        var reason = new PersistOnPrerendering();

        // Act
        var result = filter.ShouldPersist(reason);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PersistOnCircuitPauseFilter_AllowsByDefault()
    {
        // Arrange
        var filter = new PersistOnCircuitPauseFilter();
        var reason = new PersistOnCircuitPause();

        // Act
        var result = filter.ShouldPersist(reason);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void PersistOnCircuitPauseFilter_CanBlock()
    {
        // Arrange
        var filter = new PersistOnCircuitPauseFilter(persist: false);
        var reason = new PersistOnCircuitPause();

        // Act
        var result = filter.ShouldPersist(reason);

        // Assert
        Assert.False(result);
    }
}