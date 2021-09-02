// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.AspNetCore.Diagnostics.HealthChecks
{
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
                HealthStatus.Degraded => Write(httpContext, DegradedBytes),
                HealthStatus.Healthy => Write(httpContext, HealthyBytes),
                HealthStatus.Unhealthy => Write(httpContext, UnhealthyBytes),
                _ => httpContext.Response.WriteAsync(result.Status.ToString())
            };

            async static Task Write(HttpContext context, byte[] bytes)
            {
                await context.Response.Body.WriteAsync(bytes.AsMemory());
                await context.Response.Body.FlushAsync();
            }
        }
    }
}
