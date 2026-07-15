// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints;

public class RazorComponentsServiceOptionsTest
{
    [Fact]
    public void CacheViewSizeLimit_DefaultsTo100MiB()
    {
        var options = new RazorComponentsServiceOptions();

        Assert.Equal(100 * 1024 * 1024, options.CacheViewSizeLimit);
    }

    [Fact]
    public void CacheViewSizeLimit_PositiveValue_RoundTrips()
    {
        var options = new RazorComponentsServiceOptions
        {
            CacheViewSizeLimit = 1234,
        };

        Assert.Equal(1234, options.CacheViewSizeLimit);
    }

    [Fact]
    public void CacheViewSizeLimit_Zero_IsAllowed_AndDisablesCaching()
    {
        var options = new RazorComponentsServiceOptions
        {
            CacheViewSizeLimit = 0,
        };

        Assert.Equal(0, options.CacheViewSizeLimit);
    }

    [Fact]
    public void CacheViewSizeLimit_Negative_Throws()
    {
        var options = new RazorComponentsServiceOptions();

        Assert.Throws<ArgumentOutOfRangeException>(() => options.CacheViewSizeLimit = -1);
    }
}
