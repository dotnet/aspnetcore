// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Diagnostics.HealthChecks
{
    public class HealthCheckMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly HealthCheckOptions _healthCheckOptions;
        private readonly HealthCheckService _healthCheckService;

        public HealthCheckMiddleware(
            RequestDelegate next,
            IOptions<HealthCheckOptions> healthCheckOptions,
            HealthCheckService healthCheckService)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (healthCheckOptions == null)
            {
                throw new ArgumentNullException(nameof(healthCheckOptions));
            }

            if (healthCheckService == null)
            {
                throw new ArgumentNullException(nameof(healthCheckService));
            }

            _next = next;
            _healthCheckOptions = healthCheckOptions.Value;
            _healthCheckService = healthCheckService;
        }

        /// <summary>
        /// Processes a request.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            // Get results
            var result = await _healthCheckService.CheckHealthAsync(_healthCheckOptions.Predicate, httpContext.RequestAborted);

            // Map status to response code - this is customizable via options. 
            if (!_healthCheckOptions.ResultStatusCodes.TryGetValue(result.Status, out var statusCode))
            {
                var message =
                    $"No status code mapping found for {nameof(HealthStatus)} value: {result.Status}." +
                    $"{nameof(HealthCheckOptions)}.{nameof(HealthCheckOptions.ResultStatusCodes)} must contain" +
                    $"an entry for {result.Status}.";

                throw new InvalidOperationException(message);
            }

            httpContext.Response.StatusCode = statusCode;

            if (!_healthCheckOptions.AllowCachingResponses)
            {
                // Similar to: https://github.com/aspnet/Security/blob/7b6c9cf0eeb149f2142dedd55a17430e7831ea99/src/Microsoft.AspNetCore.Authentication.Cookies/CookieAuthenticationHandler.cs#L377-L379
                var headers = httpContext.Response.Headers;
                headers[HeaderNames.CacheControl] = "no-store, no-cache";
                headers[HeaderNames.Pragma] = "no-cache";
                headers[HeaderNames.Expires] = "Thu, 01 Jan 1970 00:00:00 GMT";
            }

            if (_healthCheckOptions.ResponseWriter != null)
            {
                await _healthCheckOptions.ResponseWriter(httpContext, result);
            }
        }
    }
}
