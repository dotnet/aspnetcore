// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Matching;

// Note that while we don't intend for this code to be used with non-ASCII test,
// we still call into these methods with some non-ASCII characters so that
// we are sure of how it behaves.
public class AsciiTest
{
    [Fact]
    public void IsAscii_ReturnsTrueForAscii()
    {
        // Arrange
        var text = "abcd\u007F";

        // Act
        var result = Ascii.IsAscii(text);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAscii_ReturnsFalseForNonAscii()
    {
        // Arrange
        var text = "abcd\u0080";

        // Act
        var result = Ascii.IsAscii(text);

        // Assert
        Assert.False(result);
    }

    [Theory]

    // Identity
    [InlineData('c', 'c')]
    [InlineData('C', 'C')]
    [InlineData('#', '#')]
    [InlineData('\u0080', '\u0080')]

    // Case-insensitive
    [InlineData('c', 'C')]
    public void AsciiIgnoreCaseEquals_ReturnsTrue(char x, char y)
    {
        // Arrange

        // Act
        var result = Ascii.AsciiIgnoreCaseEquals(x, y);

        // Assert
        Assert.True(result);
    }

    [Theory]

    // Different letter
    [InlineData('c', 'd')]
    [InlineData('C', 'D')]

    // Non-letter + casing difference - 'a' and 'A' are 32 bits apart and so are ' ' and '@'
    [InlineData(' ', '@')]
    [InlineData('\u0080', '\u0080' + 32)] // Outside of ASCII range
    public void AsciiIgnoreCaseEquals_ReturnsFalse(char x, char y)
    {
        // Arrange

        // Act
        var result = Ascii.AsciiIgnoreCaseEquals(x, y);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("", "", 0)]
    [InlineData("abCD", "abcF", 3)]
    [InlineData("ab#\u0080-$%", "Ab#\u0080-$%", 7)]
    public void UnsafeAsciiIgnoreCaseEquals_ReturnsTrue(string x, string y, int length)
    {
        // Arrange
        var spanX = x.AsSpan();
        var spanY = y.AsSpan();

        // Act
        var result = Ascii.AsciiIgnoreCaseEquals(spanX, spanY, length);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("abcD", "abCE", 4)]
    [InlineData("ab#\u0080-$%", "Ab#\u0081-$%", 7)]
    public void UnsafeAsciiIgnoreCaseEquals_ReturnsFalse(string x, string y, int length)
    {
        // Arrange
        var spanX = x.AsSpan();
        var spanY = y.AsSpan();

        // Act
        var result = Ascii.AsciiIgnoreCaseEquals(spanX, spanY, length);

        // Assert
        Assert.False(result);
    }
}
