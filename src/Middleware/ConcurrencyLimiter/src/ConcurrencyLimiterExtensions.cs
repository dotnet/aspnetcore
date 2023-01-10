// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.ConcurrencyLimiter;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for adding the <see cref="ConcurrencyLimiterMiddleware"/> to an application.
/// </summary>
public static class ConcurrencyLimiterExtensions
{
    /// <summary>
    /// Adds the <see cref="ConcurrencyLimiterMiddleware"/> to limit the number of concurrently-executing requests.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
    public static IApplicationBuilder UseConcurrencyLimiter(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<ConcurrencyLimiterMiddleware>();
    }
}
