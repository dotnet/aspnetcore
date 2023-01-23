// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.HostFiltering;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for the HostFiltering middleware.
/// </summary>
public static class HostFilteringBuilderExtensions
{
    /// <summary>
    /// Adds middleware for filtering requests by allowed host headers. Invalid requests will be rejected with a
    /// 400 status code.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
    /// <returns>The original <see cref="IApplicationBuilder"/>.</returns>
    public static IApplicationBuilder UseHostFiltering(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMiddleware<HostFilteringMiddleware>();

        return app;
    }
}
