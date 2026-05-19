// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods that combine all of the static file middleware components:
/// Default files, directory browsing, send file, and static files
/// </summary>
public static class FileServerExtensions
{
    /// <summary>
    /// Enable all static file middleware (except directory browsing) for the current request path in the current directory.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    /// <remarks>
    /// Files are served from the path specified in <see cref="IWebHostEnvironment.WebRootPath"/>
    /// or <see cref="IWebHostEnvironment.WebRootFileProvider"/> which defaults to the 'wwwroot' subfolder.
    /// </remarks>
    public static IApplicationBuilder UseFileServer(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseFileServer(new FileServerOptions());
    }

    /// <summary>
    /// Enable all static file middleware on for the current request path in the current directory.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="enableDirectoryBrowsing">Should directory browsing be enabled?</param>
    /// <returns></returns>
    /// <remarks>
    /// Files are served from the path specified in <see cref="IWebHostEnvironment.WebRootPath"/>
    /// or <see cref="IWebHostEnvironment.WebRootFileProvider"/> which defaults to the 'wwwroot' subfolder.
    /// </remarks>
    public static IApplicationBuilder UseFileServer(this IApplicationBuilder app, bool enableDirectoryBrowsing)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseFileServer(new FileServerOptions
        {
            EnableDirectoryBrowsing = enableDirectoryBrowsing
        });
    }

    /// <summary>
    /// Enables all static file middleware (except directory browsing) for the given request path from the directory of the same name
    /// </summary>
    /// <param name="app"></param>
    /// <param name="requestPath">The relative request path.</param>
    /// <returns></returns>
    /// <remarks>
    /// Files are served from the path specified in <see cref="IWebHostEnvironment.WebRootPath"/>
    /// or <see cref="IWebHostEnvironment.WebRootFileProvider"/> which defaults to the 'wwwroot' subfolder.
    /// </remarks>
    public static IApplicationBuilder UseFileServer(this IApplicationBuilder app, string requestPath)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(requestPath);

        return app.UseFileServer(new FileServerOptions
        {
            RequestPath = new PathString(requestPath)
        });
    }

    /// <summary>
    /// Enable all static file middleware with the given options
    /// </summary>
    /// <param name="app"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseFileServer(this IApplicationBuilder app, FileServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(options);

        if (options.EnableDefaultFiles)
        {
            app.UseDefaultFiles(options.DefaultFilesOptions);
        }

        if (options.EnableDirectoryBrowsing)
        {
            app.UseDirectoryBrowser(options.DirectoryBrowserOptions);
        }

        return app.UseStaticFiles(options.StaticFileOptions);
    }
}
