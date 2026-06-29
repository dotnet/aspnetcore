// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// Auto-injected middleware that enforces <see cref="ICsrfProtection"/> on incoming requests.
/// Validation only runs when the matched endpoint opts in via <see cref="IAntiforgeryMetadata"/>
/// with <see cref="IAntiforgeryMetadata.RequiresValidation"/> = <see langword="true"/>. Endpoints
/// without any <see cref="IAntiforgeryMetadata"/> (e.g. unannotated minimal-API handlers) and
/// endpoints that opted out via <c>DisableAntiforgery()</c> pass through unchanged.
/// </summary>
internal sealed partial class CsrfProtectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICsrfProtection _csrfProtection;
    private readonly ILogger<CsrfProtectionMiddleware> _logger;

    private static readonly CsrfValidationException ValidationFailedException
        = new("Cross-site request forgery validation via Fetch Metadata headers failed");

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
        // The sentinel is set unconditionally even when validation is skipped: the FormFeature
        // antiforgery backstop relies on it to detect that some antiforgery-style middleware ran
        // on this request (see PR #67082). Without it, a re-executed pipeline (e.g. status code
        // re-execute into an antiforgery-required endpoint) would throw "missing middleware".
        context.Items[MiddlewareInvokedKeys.CsrfProtection] = MiddlewareInvokedKeys.Sentinel;

        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAntiforgeryMetadata>() is not { RequiresValidation: true })
        {
            // No antiforgery metadata, or the endpoint opted out via DisableAntiforgery():
            // mirror AntiforgeryMiddleware and pass through without recording a verdict.
            await _next(context);
            return;
        }

        // This middleware does not short-circuit, but only records the verdict.
        // When the application also calls UseAntiforgery(),
        // the later AntiforgeryMiddleware may overwrite this verdict with the result of token-based validation.
        if (await _csrfProtection.ValidateAsync(context) is { IsAllowed: false })
        {
            RequestFailedValidation(_logger, context.Request.Method, context.Request.Path, context.Request.Headers.Origin.ToString());
            context.Features.Set<IAntiforgeryValidationFeature>(new AntiforgeryValidationFeature(false, ValidationFailedException));
        }
        else
        {
            context.Features.Set(AntiforgeryValidationFeature.Valid);
        }

        await _next(context);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug,
        Message = "Cross-origin CSRF protection marked request {Method} {Path} from origin '{Origin}' as invalid.",
        EventName = "CsrfValidationFailed")]
    private static partial void RequestFailedValidation(ILogger logger, string method, PathString path, string origin);
}

