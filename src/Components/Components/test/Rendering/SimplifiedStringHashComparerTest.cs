// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Rendering;

public class SimplifiedStringHashComparerTest
{
    [Fact]
    public void EqualityIsCaseInsensitive()
    {
        Assert.True(SimplifiedStringHashComparer.Instance.Equals("abc", "ABC"));
    }

    [Fact]
    public void HashCodesAreCaseInsensitive()
    {
        var hash1 = SimplifiedStringHashComparer.Instance.GetHashCode("abc");
        var hash2 = SimplifiedStringHashComparer.Instance.GetHashCode("ABC");
        Assert.Equal(hash1, hash2);
    }
}
