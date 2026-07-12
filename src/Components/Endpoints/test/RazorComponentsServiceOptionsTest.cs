// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints;

public class RazorComponentsServiceOptionsTest
{
    [Fact]
    public void CacheBoundarySizeLimit_DefaultsTo100MiB()
    {
        var options = new RazorComponentsServiceOptions();

        Assert.Equal(100 * 1024 * 1024, options.CacheBoundarySizeLimit);
    }

    [Fact]
    public void CacheBoundarySizeLimit_PositiveValue_RoundTrips()
    {
        var options = new RazorComponentsServiceOptions
        {
            CacheBoundarySizeLimit = 1234,
        };

        Assert.Equal(1234, options.CacheBoundarySizeLimit);
    }

    [Fact]
    public void CacheBoundarySizeLimit_Zero_IsAllowed_AndDisablesCaching()
    {
        var options = new RazorComponentsServiceOptions
        {
            CacheBoundarySizeLimit = 0,
        };

        Assert.Equal(0, options.CacheBoundarySizeLimit);
    }

    [Fact]
    public void CacheBoundarySizeLimit_Negative_Throws()
    {
        var options = new RazorComponentsServiceOptions();

        Assert.Throws<ArgumentOutOfRangeException>(() => options.CacheBoundarySizeLimit = -1);
    }
}
