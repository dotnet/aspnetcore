// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Matching;

public abstract class MultipleEntryJumpTableTest
{
    internal abstract JumpTable CreateTable(
        int defaultDestination,
        int exitDestination,
        params (string text, int destination)[] entries);

    [Fact]
    public void GetDestination_ZeroLengthSegment_JumpsToExit()
    {
        // Arrange
        var table = CreateTable(0, 1, ("text", 2));

        // Act
        var result = table.GetDestination("ignored", new PathSegment(0, 0));

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void GetDestination_NonMatchingSegment_JumpsToDefault()
    {
        // Arrange
        var table = CreateTable(0, 1, ("text", 2));

        // Act
        var result = table.GetDestination("text", new PathSegment(1, 2));

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetDestination_SegmentMatchingText_JumpsToDestination()
    {
        // Arrange
        var table = CreateTable(0, 1, ("text", 2));

        // Act
        var result = table.GetDestination("some-text", new PathSegment(5, 4));

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public void GetDestination_SegmentMatchingTextIgnoreCase_JumpsToDestination()
    {
        // Arrange
        var table = CreateTable(0, 1, ("text", 2));

        // Act
        var result = table.GetDestination("some-tExt", new PathSegment(5, 4));

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public void GetDestination_SegmentMatchingTextIgnoreCase_MultipleEntries()
    {
        // Arrange
        var table = CreateTable(0, 1, ("tezt", 2), ("text", 3));

        // Act
        var result = table.GetDestination("some-tExt", new PathSegment(5, 4));

        // Assert
        Assert.Equal(3, result);
    }
}
