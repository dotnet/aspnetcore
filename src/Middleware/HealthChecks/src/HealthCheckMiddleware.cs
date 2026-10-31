// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics.HealthChecks;

/// <summary>
/// Middleware that exposes a health checks response with a URL endpoint.
/// </summary>
public class HealthCheckMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HealthCheckOptions _healthCheckOptions;
    private readonly HealthCheckService _healthCheckService;

    /// <summary>
    /// Creates a new instance of <see cref="HealthCheckMiddleware"/>.
    /// </summary>
    public HealthCheckMiddleware(
        RequestDelegate next,
        IOptions<HealthCheckOptions> healthCheckOptions,
        HealthCheckService healthCheckService)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(healthCheckOptions);
        ArgumentNullException.ThrowIfNull(healthCheckService);

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
        ArgumentNullException.ThrowIfNull(httpContext);

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
            headers.CacheControl = "no-store, no-cache";
            headers.Pragma = "no-cache";
            headers.Expires = "Thu, 01 Jan 1970 00:00:00 GMT";
        }

        if (_healthCheckOptions.ResponseWriter != null)
        {
            await _healthCheckOptions.ResponseWriter(httpContext, result);
        }
    }
}
