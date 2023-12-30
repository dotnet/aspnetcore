// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// <see cref="IApplicationBuilder" /> extension methods to add and configure <see cref="WebSocketMiddleware" />.
/// </summary>
public static class WebSocketMiddlewareExtensions
{
    /// <summary>
    /// Adds the <see cref="WebSocketMiddleware" /> to the request pipeline.
    /// </summary>
    /// <param name="app">
    /// The <see cref="IApplicationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The <see cref="IApplicationBuilder" />.
    /// </returns>
    public static IApplicationBuilder UseWebSockets(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<WebSocketMiddleware>();
    }

    /// <summary>
    /// Adds the <see cref="WebSocketMiddleware" /> to the request pipeline.
    /// </summary>
    /// <param name="app">
    /// The <see cref="IApplicationBuilder" /> to configure.
    /// </param>
    /// <param name="options">
    /// The <see cref="WebSocketOptions" /> to be used for the <see cref="WebSocketMiddleware" />.
    /// </param>
    /// <returns>
    /// The <see cref="IApplicationBuilder" />.
    /// </returns>
    public static IApplicationBuilder UseWebSockets(this IApplicationBuilder app, WebSocketOptions options)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(options);

        return app.UseMiddleware<WebSocketMiddleware>(Options.Create(options));
    }
}
