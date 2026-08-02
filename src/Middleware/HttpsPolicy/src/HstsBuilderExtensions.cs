// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.HttpsPolicy;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for the HSTS middleware.
/// </summary>
public static class HstsBuilderExtensions
{
    /// <summary>
    /// Adds middleware for using HSTS, which adds the Strict-Transport-Security header.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
    public static IApplicationBuilder UseHsts(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<HstsMiddleware>();
    }
}
