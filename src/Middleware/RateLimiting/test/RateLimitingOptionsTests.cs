// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RateLimiting;

public class RateLimitingOptionsTests
{
    [Fact]
    public void ThrowsOnNullLimiter()
    {
        var options = new RateLimitingOptions();
        Assert.Throws<ArgumentNullException>(() => options.Limiter = null);
    }

    [Fact]
    public void ThrowsOnNullOnRejected()
    {
        var options = new RateLimitingOptions();
        Assert.Throws<ArgumentNullException>(() => options.OnRejected = null);
    }
}
