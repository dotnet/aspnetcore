// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Abstractions.Tests;

public class FragmentStringTests
{
    [Fact]
    public void Equals_EmptyFragmentStringAndDefaultFragmentString()
    {
        // Act and Assert
        Assert.Equal(default(FragmentString), FragmentString.Empty);
        Assert.Equal(default(FragmentString), FragmentString.Empty);
        // explicitly checking == operator
        Assert.True(FragmentString.Empty == default(FragmentString));
        Assert.True(default(FragmentString) == FragmentString.Empty);
    }

    [Fact]
    public void NotEquals_DefaultFragmentStringAndNonNullFragmentString()
    {
        // Arrange
        var fragmentString = new FragmentString("#col=1");

        // Act and Assert
        Assert.NotEqual(default(FragmentString), fragmentString);
    }

    [Fact]
    public void NotEquals_EmptyFragmentStringAndNonNullFragmentString()
    {
        // Arrange
        var fragmentString = new FragmentString("#col=1");

        // Act and Assert
        Assert.NotEqual(fragmentString, FragmentString.Empty);
    }
}
