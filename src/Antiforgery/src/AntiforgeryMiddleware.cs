// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Antiforgery.Internal;

internal sealed class AntiforgeryMiddleware(IAntiforgery antiforgery, RequestDelegate next)
{
    private readonly RequestDelegate _next = next;
    private readonly IAntiforgery _antiforgery = antiforgery;

    private const string AntiforgeryMiddlewareWithEndpointInvokedKey = "__AntiforgeryMiddlewareWithEndpointInvoked";
    private static readonly object AntiforgeryMiddlewareWithEndpointInvokedValue = new object();

    public Task Invoke(HttpContext context)
    {
        var endpoint = context.GetEndpoint();

        if (endpoint is not null)
        {
            context.Items[AntiforgeryMiddlewareWithEndpointInvokedKey] = AntiforgeryMiddlewareWithEndpointInvokedValue;
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
