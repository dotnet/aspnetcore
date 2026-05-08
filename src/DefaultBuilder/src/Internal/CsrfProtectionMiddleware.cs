// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// Auto-injected middleware that enforces <see cref="ICsrfProtection"/> on incoming requests.
/// Skips validation when the matched endpoint opted out via <c>DisableAntiforgery()</c>
/// (i.e. carries <see cref="IAntiforgeryMetadata"/> with <see cref="IAntiforgeryMetadata.RequiresValidation"/> = <see langword="false"/>).
/// </summary>
internal sealed partial class CsrfProtectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICsrfProtection _csrfProtection;
    private readonly ILogger<CsrfProtectionMiddleware> _logger;

    public CsrfProtectionMiddleware(
        RequestDelegate next,
        ICsrfProtection csrfProtection,
        ILogger<CsrfProtectionMiddleware> logger)
    {
        _next = next;
        _csrfProtection = csrfProtection;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAntiforgeryMetadata>() is { RequiresValidation: false })
        {
            await _next(context);
            return;
        }

        if (await _csrfProtection.ValidateAsync(context) == CsrfProtectionResult.Denied)
        {
            RequestDenied(_logger, context.Request.Method, context.Request.Path, context.Request.Headers.Origin.ToString());
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        await _next(context);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug,
        Message = "Cross-origin CSRF protection denied request {Method} {Path} from origin '{Origin}'.")]
    private static partial void RequestDenied(ILogger logger, string method, PathString path, string origin);
}

