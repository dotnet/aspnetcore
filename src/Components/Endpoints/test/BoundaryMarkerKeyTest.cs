// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints;

public class BoundaryMarkerKeyTest
{
    [Fact]
    public void BoundaryMarkerKey_ToString_Works_WhenAllKeyComponentsArePresent()
    {
        // Arrange
        var componentNameTypeHash = "abc";
        var sequenceString = "123";
        var formattedComponentKey = "Foo { Bar = 789 }";
        var boundaryMarkerKey = new BoundaryMarkerKey(
            componentNameTypeHash.AsMemory(),
            sequenceString.AsMemory(),
            formattedComponentKey.AsMemory());

        // Act
        var key = boundaryMarkerKey.ToString();

        // Assert
        Assert.Equal("abc:123:Foo { Bar = 789 }", key);
    }

    [Fact]
    public void BoundaryMarkerKey_HasComponentKey_ReturnsFalse_WhenFormattedComponentKeyIsEmpty()
    {
        // Arrange
        var componentNameTypeHash = "abc";
        var sequenceString = "123";
        var boundaryMarkerKey = new BoundaryMarkerKey(
            componentNameTypeHash.AsMemory(),
            sequenceString.AsMemory(),
            ReadOnlyMemory<char>.Empty);

        // Act
        var hasComponentKey = boundaryMarkerKey.HasComponentKey;

        // Assert
        Assert.False(hasComponentKey);
    }

    [Fact]
    public void BoundaryMarkerKey_HasComponentKey_ReturnsTrue_WhenFormattedComponentKeyIsNotEmpty()
    {
        // Arrange
        var componentNameTypeHash = "abc";
        var sequenceString = "123";
        var formattedComponentKey = "Foo { Bar = 789 }";
        var boundaryMarkerKey = new BoundaryMarkerKey(
            componentNameTypeHash.AsMemory(),
            sequenceString.AsMemory(),
            formattedComponentKey.AsMemory());

        // Act
        var hasComponentKey = boundaryMarkerKey.HasComponentKey;

        // Assert
        Assert.True(hasComponentKey);
    }

    [Fact]
    public void BoundaryMarkerKey_TryParse_ReturnsFalse_WhenValueIsEmpty()
    {
        // Arrange
        var value = ReadOnlyMemory<char>.Empty;

        // Act
        var result = BoundaryMarkerKey.TryParse(value, out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void BoundaryMarkerKey_TryParse_ReturnsFalse_WhenValueHasTooFewSeparators()
    {
        // Arrange
        var value = "abc:123";

        // Act
        var result = BoundaryMarkerKey.TryParse(value.AsMemory(), out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void BoundaryMarkerKey_TryParse_ReturnsFalse_WhenValueHasTooManySeparators()
    {
        // Arrange
        var value = "abc:123:Foo { Bar = 789 }:Extra";

        // Act
        var result = BoundaryMarkerKey.TryParse(value.AsMemory(), out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void BoundaryMarkerKey_TryParse_ReturnsTrue_WhenValueHasExactlyThreeSeparators()
    {
        // Arrange
        var value = "abc:123:Foo { Bar = 789 }";

        // Act
        var result = BoundaryMarkerKey.TryParse(value.AsMemory(), out var boundaryMarkerKey);
        var key = boundaryMarkerKey.ToString();

        // Assert
        Assert.True(result);
        Assert.Equal(value, key);
    }
}
