// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;

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
        public DirectoryBrowserMiddleware([NotNull] RequestDelegate next, [NotNull] IHostingEnvironment hostingEnv, [NotNull] DirectoryBrowserOptions options)
        {
            if (options.Formatter == null)
            {
                throw new ArgumentException(Resources.Args_NoFormatter);
            }
            if (options.FileSystem == null)
            {
                options.FileSystem = new PhysicalFileSystem(Helpers.ResolveRootPath(hostingEnv.WebRoot, options.RequestPath));
            }

            _next = next;
            _options = options;
            _matchUrl = options.RequestPath;
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
                    context.Response.Headers[Constants.Location] = context.Request.PathBase + context.Request.Path + "/" + context.Request.QueryString;
                    return Constants.CompletedTask;
                }

                return _options.Formatter.GenerateContentAsync(context, contents);
            }

            return _next(context);
        }

        private bool TryGetDirectoryInfo(PathString subpath, out IDirectoryContents contents)
        {
            contents = _options.FileSystem.GetDirectoryContents(subpath.Value);
            return contents.Exists;
        }
    }
}
