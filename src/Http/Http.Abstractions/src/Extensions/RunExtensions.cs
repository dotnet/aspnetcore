// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for adding terminal middleware.
/// </summary>
public static class RunExtensions
{
    /// <summary>
    /// Adds a terminal middleware delegate to the application's request pipeline.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> instance.</param>
    /// <param name="handler">A delegate that handles the request.</param>
    public static void Run(this IApplicationBuilder app, RequestDelegate handler)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(handler);

        app.Use(_ => handler);
    }
}
