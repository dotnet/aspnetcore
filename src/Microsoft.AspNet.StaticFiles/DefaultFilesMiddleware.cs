// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.StaticFiles
{
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

        /// <summary>
        /// Creates a new instance of the DefaultFilesMiddleware.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="options">The configuration options for this middleware.</param>
        public DefaultFilesMiddleware(RequestDelegate next, IHostingEnvironment hostingEnv, IOptions<DefaultFilesOptions> options)
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
            
            _next = next;
            _options = options.Value;
            _options.ResolveFileProvider(hostingEnv);
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
            PathString subpath;
            if (Helpers.IsGetOrHeadMethod(context.Request.Method)
                && Helpers.TryMatchPath(context, _matchUrl, forDirectory: true, subpath: out subpath))
            {
                var dirContents = _options.FileProvider.GetDirectoryContents(subpath.Value);
                if (dirContents.Exists)
                {
                    // Check if any of our default files exist.
                    for (int matchIndex = 0; matchIndex < _options.DefaultFileNames.Count; matchIndex++)
                    {
                        string defaultFile = _options.DefaultFileNames[matchIndex];
                        var file = _options.FileProvider.GetFileInfo(subpath + defaultFile);
                        // TryMatchPath will make sure subpath always ends with a "/" by adding it if needed.
                        if (file.Exists)
                        {
                            // If the path matches a directory but does not end in a slash, redirect to add the slash.
                            // This prevents relative links from breaking.
                            if (!Helpers.PathEndsInSlash(context.Request.Path))
                            {
                                context.Response.StatusCode = 301;
                                context.Response.Headers[HeaderNames.Location] = context.Request.PathBase + context.Request.Path + "/" + context.Request.QueryString;
                                return Constants.CompletedTask;
                            }

                            // Match found, re-write the url. A later middleware will actually serve the file.
                            context.Request.Path = new PathString(context.Request.Path.Value + defaultFile);
                            break;
                        }
                    }
                }
            }

            return _next(context);
        }
    }
}
