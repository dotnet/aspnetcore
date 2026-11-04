// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Middleware that performs authentication.
/// </summary>
public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of <see cref="AuthenticationMiddleware"/>.
    /// </summary>
    /// <param name="next">The next item in the middleware pipeline.</param>
    /// <param name="schemes">The <see cref="IAuthenticationSchemeProvider"/>.</param>
    public AuthenticationMiddleware(RequestDelegate next, IAuthenticationSchemeProvider schemes)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(schemes);

        _next = next;
        Schemes = schemes;
    }

    /// <summary>
    /// Gets or sets the <see cref="IAuthenticationSchemeProvider"/>.
    /// </summary>
    public IAuthenticationSchemeProvider Schemes { get; set; }

    /// <summary>
    /// Invokes the middleware performing authentication.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    public async Task Invoke(HttpContext context)
    {
        context.Features.Set<IAuthenticationFeature>(new AuthenticationFeature
        {
            OriginalPath = context.Request.Path,
            OriginalPathBase = context.Request.PathBase
        });

        // Give any IAuthenticationRequestHandler schemes a chance to handle the request
        var handlers = context.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
        foreach (var scheme in await Schemes.GetRequestHandlerSchemesAsync())
        {
            var handler = await handlers.GetHandlerAsync(context, scheme.Name) as IAuthenticationRequestHandler;
            if (handler != null && await handler.HandleRequestAsync())
            {
                return;
            }
        }

        var defaultAuthenticate = await Schemes.GetDefaultAuthenticateSchemeAsync();
        if (defaultAuthenticate != null)
        {
            var result = await context.AuthenticateAsync(defaultAuthenticate.Name);
            if (result?.Principal != null)
            {
                context.User = result.Principal;
            }
            if (result?.Succeeded ?? false)
            {
                var authFeatures = new AuthenticationFeatures(result);
                context.Features.Set<IHttpAuthenticationFeature>(authFeatures);
                context.Features.Set<IAuthenticateResultFeature>(authFeatures);
            }
        }

        await _next(context);
    }
}
