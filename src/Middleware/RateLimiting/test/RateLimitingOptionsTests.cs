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
        var ex = Assert.Throws<ArgumentNullException>(() => options.AddLimiter<HttpContext>(null));
    }
}
