// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class SourceLocationTrackerTest
{
    private static readonly SourceLocation TestStartLocation = new SourceLocation(10, 42, 45);

    [Theory]
    [InlineData(null)]
    [InlineData("path-to-file")]
    public void Advance_PreservesSourceLocationFilePath(string path)
    {
        // Arrange
        var sourceLocation = new SourceLocation(path, 15, 2, 8);

        // Act
        var result = SourceLocationTracker.Advance(sourceLocation, "Hello world");

        // Assert
        Assert.Equal(path, result.FilePath);
        Assert.Equal(26, result.AbsoluteIndex);
        Assert.Equal(2, result.LineIndex);
        Assert.Equal(19, result.CharacterIndex);
    }

    [Fact]
    public void UpdateLocationAdvancesCorrectlyForMultiLineString()
    {
        // Arrange
        var location = TestStartLocation;

        // Act
        var currentLocation = SourceLocationTracker.Advance(location, "foo\nbar\rbaz\r\nbox");

        // Assert
        Assert.Equal(26, currentLocation.AbsoluteIndex);
        Assert.Equal(45, currentLocation.LineIndex);
        Assert.Equal(3, currentLocation.CharacterIndex);
    }


    [Fact]
    public void UpdateLocationAdvancesCharacterIndexOnNonNewlineCharacter()
    {
        // Arrange
        var location = TestStartLocation;

        // Act
        var currentLocation = UpdateLocation(location, 'f', 'o');

        // Assert
        Assert.Equal(46, currentLocation.CharacterIndex);
    }


    [Fact]
    public void UpdateLocationDoesNotAdvanceLineIndexOnNonNewlineCharacter()
    {
        // Arrange
        var location = TestStartLocation;

        // Act
        var currentLocation = UpdateLocation(location, 'f', 'o');

        // Assert
        Assert.Equal(42, currentLocation.LineIndex);
    }

    [Fact]
    public void UpdateLocationAdvancesLineIndexOnSlashN()
    {
        // Arrange
        var location = TestStartLocation;

        // Act
        var currentLocation = UpdateLocation(location, '\n', 'o');

        // Assert
        Assert.Equal(43, currentLocation.LineIndex);
    }

    [Fact]
    public void UpdateLocationAdvancesAbsoluteIndexOnSlashN()
    {
        // Arrange
        var location = TestStartLocation;

        // Act
        var currentLocation = UpdateLocation(location, '\n', 'o');

        // Assert
        Assert.Equal(11, currentLocation.AbsoluteIndex);
    }

    [Fact]
    public void UpdateLocationResetsCharacterIndexOnSlashN()
    {
        // Arrange
        var location = TestStartLocation;

        // Act
        var currentLocation = UpdateLocation(location, '\n', 'o');

        // Assert
        Assert.Equal(0, currentLocation.CharacterIndex);
    }

    [Fact]
    public void UpdateLocationAdvancesLineIndexOnSlashRFollowedByNonNewlineCharacter()
    {
        // Arrange
        var location = TestStartLocation;

        // Act
        var currentLocation = UpdateLocation(location, '\r', 'o');

        // Assert
        Assert.Equal(43, currentLocation.LineIndex);
    }

    [Fact]
    public void UpdateLocationAdvancesAbsoluteIndexOnSlashRFollowedByNonNewlineCharacter()
    {
        // Arrange
        var location = TestStartLocation;

        // Act
        var currentLocation = UpdateLocation(location, '\r', 'o');

        // Assert
        Assert.Equal(11, currentLocation.AbsoluteIndex);
    }

    [Fact]
    public void UpdateLocationResetsCharacterIndexOnSlashRFollowedByNonNewlineCharacter()
    {
        // Arrange
        var location = TestStartLocation;

        // Act
        var currentLocation = UpdateLocation(location, '\r', 'o');

        // Assert
        Assert.Equal(0, currentLocation.CharacterIndex);
    }

    [Fact]
    public void UpdateLocationDoesNotAdvanceLineIndexOnSlashRFollowedBySlashN()
    {
        // Arrange
        var location = TestStartLocation;

        // Act
        var currentLocation = UpdateLocation(location, '\r', '\n');

        // Assert
        Assert.Equal(42, currentLocation.LineIndex);
    }

    [Fact]
    public void UpdateLocationAdvancesAbsoluteIndexOnSlashRFollowedBySlashN()
    {
        // Arrange
        var location = TestStartLocation;

        // Act
        var currentLocation = UpdateLocation(location, '\r', '\n');

        // Assert
        Assert.Equal(11, currentLocation.AbsoluteIndex);
    }

    [Fact]
    public void UpdateLocationAdvancesCharacterIndexOnSlashRFollowedBySlashN()
    {
        // Arrange
        var location = TestStartLocation;

        // Act
        var currentLocation = UpdateLocation(location, '\r', '\n');

        // Assert
        Assert.Equal(46, currentLocation.CharacterIndex);
    }

    private static SourceLocation UpdateLocation(SourceLocation location, char v1, char v2)
    {
        var absoluteIndex = location.AbsoluteIndex;
        var lineIndex = location.LineIndex;
        var characterIndex = location.CharacterIndex;

        SourceLocationTracker.UpdateCharacterCore(v1, v2, ref absoluteIndex, ref lineIndex, ref characterIndex);

        return new SourceLocation(location.FilePath, absoluteIndex, lineIndex, characterIndex);
    }
}
