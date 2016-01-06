// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.StaticFiles
{
    /// <summary>
    /// Enables directory browsing
    /// </summary>
    public class DirectoryBrowserMiddleware
    {
        private readonly DirectoryBrowserOptions _options;
        private readonly PathString _matchUrl;
        private readonly RequestDelegate _next;

        /// <summary>
        /// Creates a new instance of the SendFileMiddleware.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="options">The configuration for this middleware.</param>
        public DirectoryBrowserMiddleware(RequestDelegate next, IHostingEnvironment hostingEnv, IOptions<DirectoryBrowserOptions> options)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (hostingEnv == null)
            {
                throw new ArgumentNullException(nameof(hostingEnv));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.Value.Formatter == null)
            {
                throw new ArgumentException(Resources.Args_NoFormatter);
            }

            _next = next;
            _options = options.Value;
            _options.ResolveFileProvider(hostingEnv);
            _matchUrl = _options.RequestPath;
        }

        /// <summary>
        /// Examines the request to see if it matches a configured directory.  If so, a view of the directory contents is returned.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task Invoke(HttpContext context)
        {
            // Check if the URL matches any expected paths
            PathString subpath;
            IDirectoryContents contents;
            if (Helpers.IsGetOrHeadMethod(context.Request.Method)
                && Helpers.TryMatchPath(context, _matchUrl, forDirectory: true, subpath: out subpath)
                && TryGetDirectoryInfo(subpath, out contents))
            {
                // If the path matches a directory but does not end in a slash, redirect to add the slash.
                // This prevents relative links from breaking.
                if (!Helpers.PathEndsInSlash(context.Request.Path))
                {
                    context.Response.StatusCode = 301;
                    context.Response.Headers[HeaderNames.Location] = context.Request.PathBase + context.Request.Path + "/" + context.Request.QueryString;
                    return Constants.CompletedTask;
                }

                return _options.Formatter.GenerateContentAsync(context, contents);
            }

            return _next(context);
        }

        private bool TryGetDirectoryInfo(PathString subpath, out IDirectoryContents contents)
        {
            contents = _options.FileProvider.GetDirectoryContents(subpath.Value);
            return contents.Exists;
        }
    }
}