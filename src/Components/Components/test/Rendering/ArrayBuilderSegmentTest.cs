// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components.RenderTree;
using Xunit;

namespace Microsoft.AspNetCore.Components.Rendering
{
    public class ArrayBuilderSegmentTest
    {
        [Fact]
        public void BasicPropertiesWork()
        {
            // Arrange: builder containing 1..5
            using var builder = new ArrayBuilder<int>();
            builder.Append(new[] { 1, 2, 3, 4, 5 }, 0, 5);

            // Act: take segment containing 2..3
            var segment = builder.ToSegment(1, 3);

            // Act
            Assert.Same(builder.Buffer, segment.Array);
            Assert.Equal(1, segment.Offset);
            Assert.Equal(2, segment.Count);
            Assert.Equal(2, segment[0]);
            Assert.Equal(3, segment[1]);
            Assert.Equal(new[] { 2, 3 }, segment);
        }

        [Fact]
        public void StillWorksAfterUnderlyingCapacityChange()
        {
            // Arrange: builder containing 1..8
            using var builder = new ArrayBuilder<int>(minCapacity: 10, new TestArrayPool<int>());
            builder.Append(new[] { 1, 2, 3, 4, 5, 6, 7, 8 }, 0, 8);
            var originalBuffer = builder.Buffer;

            // Act/Assert 1: take segment containing 1..5
            var segment = builder.ToSegment(0, 5);
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, segment);
            Assert.Same(originalBuffer, segment.Array);

            // Act 2: grow the builder enough to force a resize
            builder.Append(new[] { 9, 10, 11 }, 0, 3);
            Array.Clear(originalBuffer, 0, originalBuffer.Length); // Extra proof that we're not using the original storage

            // Assert 2
            Assert.Same(builder.Buffer, segment.Array);
            Assert.NotSame(originalBuffer, segment.Array); // Since there was a resize
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, segment);
            Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 }, builder.ToSegment(0, builder.Count));
        }
    }
}
