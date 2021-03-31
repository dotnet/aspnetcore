// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.AspNetCore.Diagnostics.HealthChecks
{
    internal static class HealthCheckResponseWriters
    {
        public static Task WriteMinimalPlaintext(HttpContext httpContext, HealthReport result)
        {
            httpContext.Response.ContentType = "text/plain";
            return httpContext.Response.WriteAsync(result.Status.ToString());
        }
    }
}
