// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Matching;

public class ILEmitTrieFactoryTest
{
    // We never vectorize on 32bit, so that's part of the test.
    [Fact]
    public void ShouldVectorize_ReturnsTrue_ForLargeEnoughStrings()
    {
        // Arrange
        var is64Bit = IntPtr.Size == 8;
        var expected = is64Bit;

        var entries = new[]
        {
                ("foo", 0),
                ("badr", 0),
                ("", 0),
            };

        // Act
        var actual = ILEmitTrieFactory.ShouldVectorize(entries);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ShouldVectorize_ReturnsFalseForSmallStrings()
    {
        // Arrange
        var entries = new[]
        {
                ("foo", 0),
                ("sma", 0),
                ("", 0),
            };

        // Act
        var actual = ILEmitTrieFactory.ShouldVectorize(entries);

        // Assert
        Assert.False(actual);
    }
}
