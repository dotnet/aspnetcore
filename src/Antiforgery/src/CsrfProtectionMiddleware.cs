// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Antiforgery.Internal;

internal sealed class CsrfProtectionMiddleware(ICsrfProtection csrfProtection, ILogger<CsrfProtectionMiddleware> logger, RequestDelegate next)
{
    private readonly ICsrfProtection _csrfProtection = csrfProtection;
    private readonly ILogger<CsrfProtectionMiddleware> _logger = logger;
    private readonly RequestDelegate _next = next;

    internal const string CsrfProtectionMiddlewareInvokedKey = "__CsrfProtectionMiddlewareInvoked";
    private static readonly object CsrfProtectionMiddlewareInvokedValue = new object();

    public Task Invoke(HttpContext context)
    {
        var endpoint = context.GetEndpoint();

        if (endpoint is not null)
        {
            context.Items[CsrfProtectionMiddlewareInvokedKey] = CsrfProtectionMiddlewareInvokedValue;
        }

        // Only validate endpoints that require antiforgery protection.
        if (endpoint?.Metadata.GetMetadata<IAntiforgeryMetadata>() is { RequiresValidation: true })
        {
            var result = _csrfProtection.Validate(context);
            if (result == CsrfProtectionResult.Denied)
            {
                _logger.LogWarning("Cross-origin request blocked for {Method} {Path}", context.Request.Method, context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Task.CompletedTask;
            }
        }

        return _next(context);
    }
}
