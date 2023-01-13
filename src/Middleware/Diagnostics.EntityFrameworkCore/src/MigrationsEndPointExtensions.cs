// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// <see cref="IApplicationBuilder"/> extension methods for the <see cref="MigrationsEndPointMiddleware"/>.
/// </summary>
public static class MigrationsEndPointExtensions
{
    /// <summary>
    /// Processes requests to execute migrations operations. The middleware will listen for requests made to <see cref="MigrationsEndPointOptions.DefaultPath"/>.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> to register the middleware with.</param>
    /// <returns>The same <see cref="IApplicationBuilder"/> instance so that multiple calls can be chained.</returns>
    public static IApplicationBuilder UseMigrationsEndPoint(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMigrationsEndPoint(new MigrationsEndPointOptions());
    }

    /// <summary>
    /// Processes requests to execute migrations operations. The middleware will listen for requests to the path configured in <paramref name="options"/>.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> to register the middleware with.</param>
    /// <param name="options">An action to set the options for the middleware.</param>
    /// <returns>The same <see cref="IApplicationBuilder"/> instance so that multiple calls can be chained.</returns>
    public static IApplicationBuilder UseMigrationsEndPoint(this IApplicationBuilder app, MigrationsEndPointOptions options)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(options);

        return app.UseMiddleware<MigrationsEndPointMiddleware>(Options.Create(options));
    }
}
