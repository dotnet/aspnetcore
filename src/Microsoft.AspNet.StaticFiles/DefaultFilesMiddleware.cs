// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.Owin.FileSystems;

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
        public DefaultFilesMiddleware(RequestDelegate next, DefaultFilesOptions options)
        {
            if (next == null)
            {
                throw new ArgumentNullException("next");
            }
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }
            if (options.FileSystem == null)
            {
                options.FileSystem = new PhysicalFileSystem("." + options.RequestPath.Value);
            }

            _next = next;
            _options = options;
            _matchUrl = options.RequestPath;
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
            IEnumerable<IFileInfo> dirContents;
            PathString subpath;
            if (Helpers.IsGetOrHeadMethod(context.Request.Method)
                && Helpers.TryMatchPath(context, _matchUrl, forDirectory: true, subpath: out subpath)
                && _options.FileSystem.TryGetDirectoryContents(subpath.Value, out dirContents))
            {
                // Check if any of our default files exist.
                for (int matchIndex = 0; matchIndex < _options.DefaultFileNames.Count; matchIndex++)
                {
                    string defaultFile = _options.DefaultFileNames[matchIndex];
                    IFileInfo file;
                    // TryMatchPath will make sure subpath always ends with a "/" by adding it if needed.
                    if (_options.FileSystem.TryGetFileInfo(subpath + defaultFile, out file))
                    {
                        // If the path matches a directory but does not end in a slash, redirect to add the slash.
                        // This prevents relative links from breaking.
                        if (!Helpers.PathEndsInSlash(context.Request.Path))
                        {
                            context.Response.StatusCode = 301;
                            context.Response.Headers[Constants.Location] = context.Request.PathBase + context.Request.Path + "/";
                            return Constants.CompletedTask;
                        }

                        // Match found, re-write the url. A later middleware will actually serve the file.
                        context.Request.Path = new PathString(context.Request.Path.Value + defaultFile);
                        break;
                    }
                }
            }

            return _next(context);
        }
    }
}
