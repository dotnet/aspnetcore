// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Matching;

public class ZeroEntryJumpTableTest
{
    [Fact]
    public void GetDestination_ZeroLengthSegment_JumpsToExit()
    {
        // Arrange
        var table = new ZeroEntryJumpTable(0, 1);

        // Act
        var result = table.GetDestination("ignored", new PathSegment(0, 0));

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void GetDestination_SegmentWithLength_JumpsToDefault()
    {
        // Arrange
        var table = new ZeroEntryJumpTable(0, 1);

        // Act
        var result = table.GetDestination("ignored", new PathSegment(0, 1));

        // Assert
        Assert.Equal(0, result);
    }
}
