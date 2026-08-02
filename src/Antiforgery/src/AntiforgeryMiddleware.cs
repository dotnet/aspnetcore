// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Antiforgery.Internal;

internal sealed class AntiforgeryMiddleware(IAntiforgery antiforgery, RequestDelegate next)
{
    private readonly RequestDelegate _next = next;
    private readonly IAntiforgery _antiforgery = antiforgery;

    public Task Invoke(HttpContext context)
    {
        var endpoint = context.GetEndpoint();

        if (endpoint is not null)
        {
            context.Items[MiddlewareInvokedKeys.Antiforgery] = MiddlewareInvokedKeys.Sentinel;
        }

        var method = context.Request.Method;
        if (!HttpExtensions.IsValidHttpMethodForForm(method))
        {
            return _next(context);
        }

        if (endpoint?.Metadata.GetMetadata<IAntiforgeryMetadata>() is { RequiresValidation: true })
        {
            return InvokeAwaited(context);
        }

        return _next(context);
    }

    public async Task InvokeAwaited(HttpContext context)
    {
        // An earlier middleware (e.g. the auto-injected CSRF protection) may have already recorded a verdict on
        // IAntiforgeryValidationFeature. Token validation here is authoritative and overrides that verdict, so we
        // clear any prior result first. This also prevents the FormFeature antiforgery backstop from blocking this
        // middleware's own read of the form while it looks for the request token.
        context.Features.Set<IAntiforgeryValidationFeature?>(null);
        try
        {
            await _antiforgery.ValidateRequestAsync(context);
            context.Features.Set(AntiforgeryValidationFeature.Valid);
        }
        catch (AntiforgeryValidationException e)
        {
            context.Features.Set<IAntiforgeryValidationFeature>(new AntiforgeryValidationFeature(false, e));
        }
        await _next(context);
    }
}
