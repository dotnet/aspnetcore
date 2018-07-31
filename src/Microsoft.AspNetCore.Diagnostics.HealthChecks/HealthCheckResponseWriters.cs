// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Diagnostics.HealthChecks
{
    public static class HealthCheckResponseWriters
    {
        public static Task WriteMinimalPlaintext(HttpContext httpContext, CompositeHealthCheckResult result)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            httpContext.Response.ContentType = "text/plain";
            return httpContext.Response.WriteAsync(result.Status.ToString());
        }

        public static Task WriteDetailedJson(HttpContext httpContext, CompositeHealthCheckResult result)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            httpContext.Response.ContentType = "application/json";

            var json = new JObject(
                new JProperty("status", result.Status.ToString()),
                new JProperty("results", new JObject(result.Results.Select(pair =>
                    new JProperty(pair.Key, new JObject(
                        new JProperty("status", pair.Value.Status.ToString()),
                        new JProperty("description", pair.Value.Description),
                        new JProperty("data", new JObject(pair.Value.Data.Select(p => new JProperty(p.Key, p.Value))))))))));
            return httpContext.Response.WriteAsync(json.ToString(Formatting.Indented));
        }
    }
}
