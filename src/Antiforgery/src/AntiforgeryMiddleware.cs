// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Antiforgery.CrossOrigin;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Antiforgery.Internal;

internal sealed class AntiforgeryMiddleware(ICrossOriginAntiforgery crossOriginAntiforgery, IAntiforgery antiforgery, RequestDelegate next)
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
            var crossOriginResult = crossOriginAntiforgery.Validate(context);

            return crossOriginResult switch
            {
                CrossOriginValidationResult.Allowed => _next(context),
                CrossOriginValidationResult.Denied => HandleDenied(context),
                CrossOriginValidationResult.Unknown or _ => InvokeTokenValidation(context)
            };
        }

        return _next(context);
    }

    private async Task HandleDenied(HttpContext context)
    {
        // todo change to null
        context.Features.Set<IAntiforgeryValidationFeature>(new AntiforgeryValidationFeature(isValid: false, exception: null));

        await _next(context);
    }

    private async Task InvokeTokenValidation(HttpContext context)
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
