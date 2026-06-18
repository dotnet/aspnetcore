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

    private static readonly CsrfValidationException ValidationFailedException
        = new("Cross-site request forgery validation via Fetch Metadata headers failed");

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
        // Endpoint already matched (auto-routing) or no matcher available: record the verdict in place.
        if (context.GetEndpoint() is not null || _endpointResolver is null)
        {
            await RecordVerdictAsync(context);
            await _next(context);
            return;
        }

        // The app called UseRouting() explicitly, so routing runs after this middleware. Borrow routing state
        // to resolve per-endpoint CORS/antiforgery metadata, then restore it: middleware ordered before the
        // app's UseRouting() must observe a null endpoint. The recorded verdict and sentinel live on
        // HttpContext.Features/Items, so they survive the restore for downstream consumers to read.
        var originalRouteValues = context.Request.RouteValues;
        try
        {
            await _endpointResolver.MatchEndpoint(context);
            await RecordVerdictAsync(context);
        }
        finally
        {
            context.SetEndpoint(null);
            context.Request.RouteValues = originalRouteValues;
        }

        await _next(context);
    }

    private async ValueTask RecordVerdictAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint is not null)
        {
            // Mark that CSRF protection observed this endpoint, even if it opts out below, so
            // EndpointMiddleware's antiforgery-metadata check passes.
            context.Items[MiddlewareInvokedKeys.CsrfProtection] = MiddlewareInvokedKeys.Sentinel;
        }

        if (endpoint?.Metadata.GetMetadata<IAntiforgeryMetadata>() is { RequiresValidation: false })
        {
            return;
        }

        if (await _csrfProtection.ValidateAsync(context) is { IsAllowed: false })
        {
            RequestFailedValidation(_logger, context.Request.Method, context.Request.Path, context.Request.Headers.Origin.ToString());
            context.Features.Set<IAntiforgeryValidationFeature>(new AntiforgeryValidationFeature(false, ValidationFailedException));
        }
        else
        {
            context.Features.Set(AntiforgeryValidationFeature.Valid);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug,
        Message = "Cross-origin CSRF protection marked request {Method} {Path} from origin '{Origin}' as invalid.",
        EventName = "CsrfValidationFailed")]
    private static partial void RequestFailedValidation(ILogger logger, string method, PathString path, string origin);
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

