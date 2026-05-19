// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Session;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for adding the <see cref="SessionMiddleware"/> to an application.
/// </summary>
public static class SessionMiddlewareExtensions
{
    /// <summary>
    /// Adds the <see cref="SessionMiddleware"/> to automatically enable session state for the application.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
    public static IApplicationBuilder UseSession(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<SessionMiddleware>();
    }

    /// <summary>
    /// Adds the <see cref="SessionMiddleware"/> to automatically enable session state for the application.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    /// <param name="options">The <see cref="SessionOptions"/>.</param>
    /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
    public static IApplicationBuilder UseSession(this IApplicationBuilder app, SessionOptions options)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(options);

        return app.UseMiddleware<SessionMiddleware>(Options.Create(options));
    }
}
