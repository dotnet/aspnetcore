// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !FORWARDCOMPAT

using Microsoft.AspNetCore.Server.HttpSys;

namespace TestSite;

public partial class Startup
{
    private async Task HttpSysRequestTimingInfo(HttpContext ctx)
    {
        await ctx.Response.WriteAsJsonAsync(GetTimings(ctx));

        static long[] GetTimings(HttpContext ctx)
        {
            var timingFeature = ctx.Features.Get<IHttpSysRequestTimingFeature>()
                ?? throw new NotSupportedException($"Failed to get {nameof(IHttpSysRequestTimingFeature)}");

            return timingFeature.Timestamps.ToArray();
        }
    }
}
#endif
