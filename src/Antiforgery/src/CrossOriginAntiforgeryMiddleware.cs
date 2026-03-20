// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Antiforgery.CrossOrigin;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Antiforgery.Internal;

/// <summary>
/// Middleware that performs cross-origin-only antiforgery validation.
/// Unknown results are treated as denied (strict mode).
/// </summary>
internal sealed class CrossOriginAntiforgeryMiddleware(ICrossOriginAntiforgery crossOriginAntiforgery, RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

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
            var result = crossOriginAntiforgery.Validate(context);

            return result switch
            {
                CrossOriginValidationResult.Allowed => _next(context),
                _ => HandleDenied(context)
            };
        }

        return _next(context);
    }

    private async Task HandleDenied(HttpContext context)
    {
        context.Features.Set<IAntiforgeryValidationFeature>(new AntiforgeryValidationFeature(isValid: false, exception: null));
        await _next(context);
    }
}
