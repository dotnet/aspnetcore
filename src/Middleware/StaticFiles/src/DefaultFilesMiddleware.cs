// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.StaticFiles;

/// <summary>
/// This examines a directory path and determines if there is a default file present.
/// If so the file name is appended to the path and execution continues.
/// Note we don't just serve the file because it may require interpretation.
/// </summary>
public class DefaultFilesMiddleware
{
    private readonly DefaultFilesOptions _options;
    private readonly PathString _matchUrl;
    private readonly RequestDelegate _next;
    private readonly IFileProvider _fileProvider;

    /// <summary>
    /// Creates a new instance of the DefaultFilesMiddleware.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="hostingEnv">The <see cref="IWebHostEnvironment"/> used by this middleware.</param>
    /// <param name="options">The configuration options for this middleware.</param>
    public DefaultFilesMiddleware(RequestDelegate next, IWebHostEnvironment hostingEnv, IOptions<DefaultFilesOptions> options)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(hostingEnv);
        ArgumentNullException.ThrowIfNull(options);

        _next = next;
        _options = options.Value;
        _fileProvider = _options.FileProvider ?? Helpers.ResolveFileProvider(hostingEnv);
        _matchUrl = _options.RequestPath;
    }

    /// <summary>
    /// This examines the request to see if it matches a configured directory, and if there are any files with the
    /// configured default names in that directory.  If so this will append the corresponding file name to the request
    /// path for a later middleware to handle.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public Task Invoke(HttpContext context)
    {
        if (context.GetEndpoint()?.RequestDelegate is null
            && Helpers.IsGetOrHeadMethod(context.Request.Method)
            && Helpers.TryMatchPath(context, _matchUrl, forDirectory: true, subpath: out var subpath))
        {
            // TryMatchPath will not output an empty subpath when it returns true.
            var dirContents = _fileProvider.GetDirectoryContents(subpath.Value!);
            if (dirContents.Exists)
            {
                // Check if any of our default files exist.
                for (int matchIndex = 0; matchIndex < _options.DefaultFileNames.Count; matchIndex++)
                {
                    string defaultFile = _options.DefaultFileNames[matchIndex];
                    var file = _fileProvider.GetFileInfo(subpath.Value + defaultFile);
                    // TryMatchPath will make sure subpath always ends with a "/" by adding it if needed.
                    if (file.Exists)
                    {
                        // If the path matches a directory but does not end in a slash, redirect to add the slash.
                        // This prevents relative links from breaking.
                        if (_options.RedirectToAppendTrailingSlash && !Helpers.PathEndsInSlash(context.Request.Path))
                        {
                            Helpers.RedirectToPathWithSlash(context);
                            return Task.CompletedTask;
                        }
                        // Match found, re-write the url. A later middleware will actually serve the file.
                        context.Request.Path = new PathString(Helpers.GetPathValueWithSlash(context.Request.Path) + defaultFile);
                        break;
                    }
                }
            }
        }

        return _next(context);
    }
}
