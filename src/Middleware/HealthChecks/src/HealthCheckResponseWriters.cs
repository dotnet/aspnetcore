// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.AspNetCore.Diagnostics.HealthChecks;

internal static class HealthCheckResponseWriters
{
    private static readonly byte[] DegradedBytes = Encoding.UTF8.GetBytes(HealthStatus.Degraded.ToString());
    private static readonly byte[] HealthyBytes = Encoding.UTF8.GetBytes(HealthStatus.Healthy.ToString());
    private static readonly byte[] UnhealthyBytes = Encoding.UTF8.GetBytes(HealthStatus.Unhealthy.ToString());

    public static Task WriteMinimalPlaintext(HttpContext httpContext, HealthReport result)
    {
        httpContext.Response.ContentType = "text/plain";
        return result.Status switch
        {
            HealthStatus.Degraded => httpContext.Response.Body.WriteAsync(DegradedBytes.AsMemory()).AsTask(),
            HealthStatus.Healthy => httpContext.Response.Body.WriteAsync(HealthyBytes.AsMemory()).AsTask(),
            HealthStatus.Unhealthy => httpContext.Response.Body.WriteAsync(UnhealthyBytes.AsMemory()).AsTask(),
            _ => httpContext.Response.WriteAsync(result.Status.ToString())
        };
    }
}
