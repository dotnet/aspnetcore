// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for the StaticFileMiddleware
/// </summary>
public static class StaticFileExtensions
{
    /// <summary>
    /// Enables static file serving for the current request path
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    /// <remarks>
    /// Files are served from the path specified in <see cref="IWebHostEnvironment.WebRootPath"/>
    /// or <see cref="IWebHostEnvironment.WebRootFileProvider"/> which defaults to the 'wwwroot' subfolder.
    /// </remarks>
    public static IApplicationBuilder UseStaticFiles(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<StaticFileMiddleware>();
    }

    /// <summary>
    /// Enables static file serving for the given request path
    /// </summary>
    /// <param name="app"></param>
    /// <param name="requestPath">The relative request path.</param>
    /// <returns></returns>
    /// <remarks>
    /// Files are served from the path specified in <see cref="IWebHostEnvironment.WebRootPath"/>
    /// or <see cref="IWebHostEnvironment.WebRootFileProvider"/> which defaults to the 'wwwroot' subfolder.
    /// </remarks>
    public static IApplicationBuilder UseStaticFiles(this IApplicationBuilder app, string requestPath)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseStaticFiles(new StaticFileOptions
        {
            RequestPath = new PathString(requestPath)
        });
    }

    /// <summary>
    /// Enables static file serving with the given options
    /// </summary>
    /// <param name="app"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseStaticFiles(this IApplicationBuilder app, StaticFileOptions options)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(options);

        return app.UseMiddleware<StaticFileMiddleware>(Options.Create(options));
    }
}
