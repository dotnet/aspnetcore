// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods to add authentication capabilities to an HTTP application pipeline.
/// </summary>
public static class AuthAppBuilderExtensions
{
    internal const string AuthenticationMiddlewareSetKey = "__AuthenticationMiddlewareSet";

    /// <summary>
    /// Adds the <see cref="AuthenticationMiddleware"/> to the specified <see cref="IApplicationBuilder"/>, which enables authentication capabilities.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IApplicationBuilder UseAuthentication(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.Properties[AuthenticationMiddlewareSetKey] = true;

        // The authentication middleware adds annotation to HttpContext.Items to indicate that it has run
        // that will be validated by the EndpointsRoutingMiddleware later. To do this, we need to ensure
        // that routing has run and set the endpoint feature on the HttpContext associated with the request.
        if (app.Properties.TryGetValue(RerouteHelper.GlobalRouteBuilderKey, out var routeBuilder) && routeBuilder is not null)
        {
            return app.Use(next =>
            {
                var newNext = RerouteHelper.Reroute(app, routeBuilder, next);
                var authenticationSchemeProvider = app.ApplicationServices.GetRequiredService<IAuthenticationSchemeProvider>();
                return new AuthenticationMiddleware(newNext, authenticationSchemeProvider).Invoke;
            });
        }

        return app.UseMiddleware<AuthenticationMiddleware>();
    }
}
