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
    private readonly CsrfEndpointResolver? _endpointResolver;

    public CsrfProtectionMiddleware(
        RequestDelegate next,
        ICsrfProtection csrfProtection,
        ILogger<CsrfProtectionMiddleware> logger,
        CsrfEndpointResolver? endpointResolver = null)
    {
        _next = next;
        _csrfProtection = csrfProtection;
        _logger = logger;
        _endpointResolver = endpointResolver;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (await IsRequestAllowedAsync(context))
        {
            await _next(context);
            return;
        }

        RequestDenied(_logger, context.Request.Method, context.Request.Path, context.Request.Headers.Origin.ToString());
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
    }

    private async ValueTask<bool> IsRequestAllowedAsync(HttpContext context)
    {
        // When the endpoint is already matched (auto-routing) or no matcher is available, evaluate in place.
        if (context.GetEndpoint() is not null || _endpointResolver is null)
        {
            return await EvaluateAsync(context);
        }

        // The app called UseRouting() explicitly, so routing runs after this middleware and the endpoint is
        // not yet matched. Match it on demand so per-endpoint CORS/antiforgery metadata is honored, then
        // restore the pre-routing state: middleware ordered before the app's UseRouting() must still observe
        // a null endpoint, and the downstream routing middleware re-runs matching as usual.
        var originalRouteValues = context.Request.RouteValues;
        try
        {
            await _endpointResolver.MatchEndpoint(context);
            return await EvaluateAsync(context);
        }
        finally
        {
            context.SetEndpoint(null);
            context.Request.RouteValues = originalRouteValues;
        }
    }

    private ValueTask<bool> EvaluateAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAntiforgeryMetadata>() is { RequiresValidation: false })
        {
            return ValueTask.FromResult(true);
        }

        return CheckCsrfProtectionAsync(context);
    }

    private async ValueTask<bool> CheckCsrfProtectionAsync(HttpContext context)
        => await _csrfProtection.ValidateAsync(context) is { IsAllowed: true };

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug,
        Message = "Cross-origin CSRF protection denied request {Method} {Path} from origin '{Origin}'.",
        EventName = "CsrfRequestDenied")]
    private static partial void RequestDenied(ILogger logger, string method, PathString path, string origin);
}

/// <summary>
/// Matches the endpoint for the current request so <see cref="CsrfProtectionMiddleware"/> can resolve a
/// per-endpoint CORS policy even when the app placed <c>UseRouting()</c> after the auto-injected middleware.
/// The wrapped delegate only performs endpoint matching; it never executes the endpoint.
/// </summary>
internal sealed class CsrfEndpointResolver
{
    private readonly RequestDelegate _matchEndpoint;

    public CsrfEndpointResolver(RequestDelegate matchEndpoint)
    {
        _matchEndpoint = matchEndpoint;
    }

    public Task MatchEndpoint(HttpContext context)
        => _matchEndpoint(context);
}

