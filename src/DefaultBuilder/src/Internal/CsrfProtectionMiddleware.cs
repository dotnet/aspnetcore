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

    public Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var antiforgeryMetadata = endpoint?.Metadata.GetMetadata<IAntiforgeryMetadata>();

        // Only stamp the "CSRF middleware ran" marker when it can actually be consumed downstream.
        // The marker has exactly two consumers:
        //   1. EndpointMiddleware (src/Http/Routing/src/EndpointMiddleware.cs) — checks the marker
        //      only when the matched endpoint carries IAntiforgeryMetadata{RequiresValidation:true}
        //      and throws "missing middleware" if absent.
        //   2. FormFeature.ResolveHasInvalidAntiforgeryValidationFeature
        //      (src/Http/Http/src/Features/FormFeature.cs) — checks the marker combined with an
        //      invalid IAntiforgeryValidationFeature; that feature is only ever recorded by
        //      InvokeCoreAsync below, which only runs on the RequiresValidation:true path.
        //
        // For endpoints with no antiforgery metadata at all, the marker has NO consumer. Stamping
        // it anyway forces the lazy HttpContext.Items dictionary to allocate on the hot path
        // (regression introduced by #67119; see PR comment 4835979504 — Brennan's TechEmpower
        // plaintext alloc explosion).
        //
        // The marker must still be set in two cases to keep the re-execute scenarios from #67119
        // working — the rerouted pipeline does NOT re-invoke this middleware (the post-routing
        // chain composed inside EndpointRoutingMiddleware via PostRoutingPipeline is not re-wired
        // on reroute), so the marker must already be on Items by the time the rerouted
        // EndpointMiddleware looks for it:
        //   - endpoint is null: a downstream UseStatusCodePagesWithReExecute may re-route into an
        //     antiforgery-required endpoint.
        //     Covered by CsrfProtection_ReExecutedNotFound_WithAntiforgeryMetadata_DoesNotThrowMissingMiddleware.
        //   - endpoint carries IAntiforgeryMetadata{RequiresValidation:false} (DisableAntiforgery):
        //     the original endpoint matches but skips validation, then a status-code re-execute
        //     lands on an antiforgery-required page.
        //     Covered by CsrfProtection_DisabledEndpointReExecutesIntoAntiforgeryRequiredPage_DoesNotThrowMissingMiddleware.
        if (endpoint is null || antiforgeryMetadata is not null)
        {
            context.Items[MiddlewareInvokedKeys.CsrfProtection] = MiddlewareInvokedKeys.Sentinel;
        }

        if (antiforgeryMetadata is not { RequiresValidation: true })
        {
            return _next(context);
        }

        return InvokeCoreAsync(context);
    }

    private async Task InvokeCoreAsync(HttpContext context)
    {
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

