// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.HttpsPolicy;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for the HttpsRedirection middleware.
/// </summary>
public static class HttpsPolicyBuilderExtensions
{
    /// <summary>
    /// Adds middleware for redirecting HTTP Requests to HTTPS.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> for HttpsRedirection.</returns>
    public static IApplicationBuilder UseHttpsRedirection(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var serverAddressFeature = app.ServerFeatures.Get<IServerAddressesFeature>();
        if (serverAddressFeature != null)
        {
            app.UseMiddleware<HttpsRedirectionMiddleware>(serverAddressFeature);
        }
        else
        {
            app.UseMiddleware<HttpsRedirectionMiddleware>();
        }
        return app;
    }
}
