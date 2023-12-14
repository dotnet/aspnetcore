// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for the DirectoryBrowserMiddleware
/// </summary>
public static class DirectoryBrowserExtensions
{
    /// <summary>
    /// Enable directory browsing on the current path
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    /// <remarks>
    /// Files are served from the path specified in <see cref="IWebHostEnvironment.WebRootPath"/>
    /// or <see cref="IWebHostEnvironment.WebRootFileProvider"/> which defaults to the 'wwwroot' subfolder.
    /// </remarks>
    public static IApplicationBuilder UseDirectoryBrowser(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<DirectoryBrowserMiddleware>();
    }

    /// <summary>
    /// Enables directory browsing for the given request path
    /// </summary>
    /// <param name="app"></param>
    /// <param name="requestPath">The relative request path.</param>
    /// <returns></returns>
    /// <remarks>
    /// Files are served from the path specified in <see cref="IWebHostEnvironment.WebRootPath"/>
    /// or <see cref="IWebHostEnvironment.WebRootFileProvider"/> which defaults to the 'wwwroot' subfolder.
    /// </remarks>
    public static IApplicationBuilder UseDirectoryBrowser(this IApplicationBuilder app, string requestPath)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseDirectoryBrowser(new DirectoryBrowserOptions
        {
            RequestPath = new PathString(requestPath)
        });
    }

    /// <summary>
    /// Enable directory browsing with the given options
    /// </summary>
    /// <param name="app"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseDirectoryBrowser(this IApplicationBuilder app, DirectoryBrowserOptions options)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(options);

        return app.UseMiddleware<DirectoryBrowserMiddleware>(Options.Create(options));
    }
}
