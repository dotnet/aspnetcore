// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods to add cookie policy capabilities to an HTTP application pipeline.
/// </summary>
public static class CookiePolicyAppBuilderExtensions
{
    /// <summary>
    /// Adds the <see cref="CookiePolicyMiddleware"/> handler to the specified <see cref="IApplicationBuilder"/>, which enables cookie policy capabilities.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> to add the handler to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IApplicationBuilder UseCookiePolicy(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<CookiePolicyMiddleware>();
    }

    /// <summary>
    /// Adds the <see cref="CookiePolicyMiddleware"/> handler to the specified <see cref="IApplicationBuilder"/>, which enables cookie policy capabilities.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> to add the handler to.</param>
    /// <param name="options">A <see cref="CookiePolicyOptions"/> that specifies options for the handler.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IApplicationBuilder UseCookiePolicy(this IApplicationBuilder app, CookiePolicyOptions options)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(options);

        return app.UseMiddleware<CookiePolicyMiddleware>(Options.Create(options));
    }
}
