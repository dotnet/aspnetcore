// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Routing.Matching
{
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
}
