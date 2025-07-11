// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

public class WebPersistenceFilterTest
{
    [Fact]
    public void SupportsScenario_WithMatchingScenarioType_ReturnsTrue()
    {
        // Arrange
        var filter = new WebPersistenceFilter(WebPersistenceScenarioType.Reconnection, enabled: true);
        var scenario = WebPersistenceScenario.Reconnection;

        // Act
        var result = filter.SupportsScenario(scenario);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void SupportsScenario_WithDifferentScenarioType_ReturnsFalse()
    {
        // Arrange
        var filter = new WebPersistenceFilter(WebPersistenceScenarioType.Reconnection, enabled: true);
        var scenario = WebPersistenceScenario.Prerendering;

        // Act
        var result = filter.SupportsScenario(scenario);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void SupportsScenario_WithNonWebScenario_ReturnsFalse()
    {
        // Arrange
        var filter = new WebPersistenceFilter(WebPersistenceScenarioType.Reconnection, enabled: true);
        var scenario = new TestPersistentComponentStateScenario();

        // Act
        var result = filter.SupportsScenario(scenario);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldRestore_WhenEnabled_ReturnsTrue()
    {
        // Arrange
        var filter = new WebPersistenceFilter(WebPersistenceScenarioType.Reconnection, enabled: true);
        var scenario = WebPersistenceScenario.Reconnection;

        // Act
        var result = filter.ShouldRestore(scenario);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldRestore_WhenDisabled_ReturnsFalse()
    {
        // Arrange
        var filter = new WebPersistenceFilter(WebPersistenceScenarioType.Reconnection, enabled: false);
        var scenario = WebPersistenceScenario.Reconnection;

        // Act
        var result = filter.ShouldRestore(scenario);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void StaticProperty_Prerendering_IsEnabledAndCorrectType()
    {
        // Arrange & Act
        var filter = WebPersistenceFilter.Prerendering;

        // Assert
        Assert.True(filter.SupportsScenario(WebPersistenceScenario.Prerendering));
        Assert.False(filter.SupportsScenario(WebPersistenceScenario.Reconnection));
        Assert.True(filter.ShouldRestore(WebPersistenceScenario.Prerendering));
    }

    [Fact]
    public void StaticProperty_Reconnection_IsEnabledAndCorrectType()
    {
        // Arrange & Act
        var filter = WebPersistenceFilter.Reconnection;

        // Assert
        Assert.True(filter.SupportsScenario(WebPersistenceScenario.Reconnection));
        Assert.False(filter.SupportsScenario(WebPersistenceScenario.Prerendering));
        Assert.True(filter.ShouldRestore(WebPersistenceScenario.Reconnection));
    }

    private class TestPersistentComponentStateScenario : IPersistentComponentStateScenario
    {
        public bool IsRecurring => false;
    }
}
