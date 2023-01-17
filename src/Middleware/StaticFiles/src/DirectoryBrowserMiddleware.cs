// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.StaticFiles;

/// <summary>
/// Enables directory browsing
/// </summary>
public class DirectoryBrowserMiddleware
{
    private readonly DirectoryBrowserOptions _options;
    private readonly PathString _matchUrl;
    private readonly RequestDelegate _next;
    private readonly IDirectoryFormatter _formatter;
    private readonly IFileProvider _fileProvider;

    /// <summary>
    /// Creates a new instance of the SendFileMiddleware. Using <see cref="HtmlEncoder.Default"/> instance.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="hostingEnv">The <see cref="IWebHostEnvironment"/> used by this middleware.</param>
    /// <param name="options">The configuration for this middleware.</param>
    public DirectoryBrowserMiddleware(RequestDelegate next, IWebHostEnvironment hostingEnv, IOptions<DirectoryBrowserOptions> options)
        : this(next, hostingEnv, HtmlEncoder.Default, options)
    {
    }

    /// <summary>
    /// Creates a new instance of the SendFileMiddleware.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="hostingEnv">The <see cref="IWebHostEnvironment"/> used by this middleware.</param>
    /// <param name="encoder">The <see cref="HtmlEncoder"/> used by the default <see cref="HtmlDirectoryFormatter"/>.</param>
    /// <param name="options">The configuration for this middleware.</param>
    public DirectoryBrowserMiddleware(RequestDelegate next, IWebHostEnvironment hostingEnv, HtmlEncoder encoder, IOptions<DirectoryBrowserOptions> options)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(hostingEnv);
        ArgumentNullException.ThrowIfNull(encoder);
        ArgumentNullException.ThrowIfNull(options);

        _next = next;
        _options = options.Value;
        _fileProvider = _options.FileProvider ?? Helpers.ResolveFileProvider(hostingEnv);
        _formatter = _options.Formatter ?? new HtmlDirectoryFormatter(encoder);
        _matchUrl = _options.RequestPath;
    }

    /// <summary>
    /// Examines the request to see if it matches a configured directory.  If so, a view of the directory contents is returned.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public Task Invoke(HttpContext context)
    {
        // Check if the URL matches any expected paths, skip if an endpoint with a request delegate was selected
        if (context.GetEndpoint()?.RequestDelegate is null
            && Helpers.IsGetOrHeadMethod(context.Request.Method)
            && Helpers.TryMatchPath(context, _matchUrl, forDirectory: true, subpath: out var subpath)
            && TryGetDirectoryInfo(subpath, out var contents))
        {
            // If the path matches a directory but does not end in a slash, redirect to add the slash.
            // This prevents relative links from breaking.
            if (_options.RedirectToAppendTrailingSlash && !Helpers.PathEndsInSlash(context.Request.Path))
            {
                Helpers.RedirectToPathWithSlash(context);
                return Task.CompletedTask;
            }

            return _formatter.GenerateContentAsync(context, contents);
        }

        return _next(context);
    }

    private bool TryGetDirectoryInfo(PathString subpath, out IDirectoryContents contents)
    {
        // TryMatchPath will not output an empty subpath when it returns true. This is called only in that case.
        contents = _fileProvider.GetDirectoryContents(subpath.Value!);
        return contents.Exists;
    }
}
