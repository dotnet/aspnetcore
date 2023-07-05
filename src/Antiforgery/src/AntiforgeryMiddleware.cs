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
        if (HttpMethods.IsGet(method) ||
            HttpMethods.IsHead(method) ||
            HttpMethods.IsTrace(method) ||
            HttpMethods.IsOptions(method))
        {
            return _next(context);
        }

        return InvokeAwaited(context, endpoint);
    }

    public async Task InvokeAwaited(HttpContext context, Endpoint? endpoint)
    {
        if (endpoint?.Metadata.GetMetadata<IAntiforgeryMetadata>() is { RequiresValidation: true })
        {
            try
            {
                await _antiforgery.ValidateRequestAsync(context);
            }
            catch (AntiforgeryValidationException e)
            {
                context.Features.Set<IAntiforgeryValidationFeature>(new AntiforgeryValidationFeature(false, e));
                await _next(context);
                return;
            }
            context.Features.Set(AntiforgeryValidationFeature.Valid);
        }

        await _next(context);
    }
}
