// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.RateLimiting;
public class RateLimitingMiddleware
{

    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    public RateLimitingMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<RateLimitingOptions> options)
    {
        _next = next;
        _logger = loggerFactory.CreateLogger<RateLimitingMiddleware>();
    }

    public async Task Invoke(HttpContext context)
    {
    }
}
