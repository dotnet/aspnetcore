// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// IApplicationBuilder extensions for the WelcomePageMiddleware.
/// </summary>
public static class WelcomePageExtensions
{
    /// <summary>
    /// Adds the WelcomePageMiddleware to the pipeline with the given options.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseWelcomePage(this IApplicationBuilder app, WelcomePageOptions options)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(options);

        return app.UseMiddleware<WelcomePageMiddleware>(Options.Create(options));
    }

    /// <summary>
    /// Adds the WelcomePageMiddleware to the pipeline with the given path.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseWelcomePage(this IApplicationBuilder app, PathString path)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseWelcomePage(new WelcomePageOptions
        {
            Path = path
        });
    }

    /// <summary>
    /// Adds the WelcomePageMiddleware to the pipeline with the given path.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseWelcomePage(this IApplicationBuilder app, string path)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseWelcomePage(new WelcomePageOptions
        {
            Path = new PathString(path)
        });
    }

    /// <summary>
    /// Adds the WelcomePageMiddleware to the pipeline.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseWelcomePage(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<WelcomePageMiddleware>();
    }
}
