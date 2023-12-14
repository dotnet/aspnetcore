// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

public class ReverseStringBuilderTest
{
    [Fact]
    public void ToString_ReturnsEmptyString_WhenNoWritesOccur()
    {
        // Arrange
        Span<char> initialBuffer = stackalloc char[128];
        using var builder = new ReverseStringBuilder(initialBuffer);

        // Act
        var result = builder.ToString();

        // Assert
        Assert.Equal(string.Empty, result);
        Assert.Equal(0, builder.SequenceSegmentCount);
    }

    [Fact]
    public void ToString_ReturnsEmptyString_WhenBufferIsEmpty()
    {
        // Arrange
        using var builder = new ReverseStringBuilder(Span<char>.Empty);

        // Act
        var result = builder.ToString();

        // Assert
        Assert.Equal(string.Empty, result);
        Assert.Equal(0, builder.SequenceSegmentCount);
    }

    [Fact]
    public void ToString_Works_WhenOnlyUsingStackAllocatedBuffer()
    {
        // Arrange
        Span<char> initialBuffer = stackalloc char[128];
        using var builder = new ReverseStringBuilder(initialBuffer);

        // Act
        builder.InsertFront("world!");
        builder.InsertFront(" ");
        builder.InsertFront(",");
        builder.InsertFront("Hello");
        var result = builder.ToString();

        // Assert
        Assert.Equal("Hello, world!", result);
        Assert.Equal(0, builder.SequenceSegmentCount);
    }

    [Fact]
    public void ToString_Works_WithNumbers()
    {
        // Arrange
        Span<char> initialBuffer = stackalloc char[128];
        using var builder = new ReverseStringBuilder(initialBuffer);

        // Act
        builder.InsertFront("worlds!");
        builder.InsertFront(" ");
        builder.InsertFront(123);
        builder.InsertFront(", ");
        builder.InsertFront("Hello");
        var result = builder.ToString();

        // Assert
        Assert.Equal("Hello, 123 worlds!", result);
        Assert.Equal(0, builder.SequenceSegmentCount);
    }

    [Fact]
    public void ToString_Works_AfterExceedingStackAllocatedBuffer()
    {
        // Arrange
        Span<char> initialBuffer = stackalloc char[8];
        using var builder = new ReverseStringBuilder(initialBuffer);

        // Act
        builder.InsertFront("world!");
        builder.InsertFront(" ");
        builder.InsertFront(",");
        builder.InsertFront("Hello");
        var result = builder.ToString();

        // Assert
        Assert.Equal("Hello, world!", result);
        Assert.Equal(1, builder.SequenceSegmentCount);
    }

    [Fact]
    public void ToString_Works_AfterExpandingIntoMultipleBuffersFromEstimatedStringSize()
    {
        // Arrange
        using var builder = new ReverseStringBuilder(8);
        var padding = new string('A', ReverseStringBuilder.MinimumRentedArraySize - 10);
        var expected = padding + "Hello, world!";

        // Act
        builder.InsertFront("world!");
        builder.InsertFront(" ");
        builder.InsertFront(",");
        builder.InsertFront("Hello");
        builder.InsertFront(padding);
        var result = builder.ToString();

        // Assert
        Assert.Equal(expected, result);
        Assert.Equal(2, builder.SequenceSegmentCount);
    }

    [Fact]
    public void ToString_Works_AfterUsingFallbackBuffer()
    {
        // Arrange
        using var builder = new ReverseStringBuilder(ReverseStringBuilder.MinimumRentedArraySize);
        var segmentCount = 5;
        var expected = string.Empty;

        // Act
        for (var i = 0; i < segmentCount; i++)
        {
            var c = (char)(i + 65);

            // Update the expected string.
            expected = new string(c, ReverseStringBuilder.MinimumRentedArraySize) + expected;

            // Append just one character to ensure we get a buffer with the minimum possible
            // length.
            builder.InsertFront(c.ToString());

            // Fill up the rest of the buffer.
            var s = new string(c, ReverseStringBuilder.MinimumRentedArraySize - 1);
            builder.InsertFront(s);
        }

        var actual = builder.ToString();
        Assert.Equal(expected, actual);
        Assert.Equal(segmentCount, builder.SequenceSegmentCount);
    }
}
