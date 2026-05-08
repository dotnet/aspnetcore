// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// Auto-injected middleware that enforces <see cref="ICsrfProtection"/> on incoming requests.
/// Skips validation when the matched endpoint opted out via <c>DisableAntiforgery()</c>
/// (i.e. carries <see cref="IAntiforgeryMetadata"/> with <see cref="IAntiforgeryMetadata.RequiresValidation"/> = <see langword="false"/>).
/// </summary>
internal sealed class CsrfProtectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICsrfProtection _csrfProtection;

    public CsrfProtectionMiddleware(RequestDelegate next, ICsrfProtection csrfProtection)
    {
        _next = next;
        _csrfProtection = csrfProtection;
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
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        await _next(context);
    }
}
