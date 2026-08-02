// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;

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
        return app.UseMiddleware<AuthenticationMiddleware>();
    }
}
