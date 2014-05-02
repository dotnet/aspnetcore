// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.FileSystems;

namespace Microsoft.AspNet.StaticFiles
{
    /// <summary>
    /// Enables serving static files for a given request path
    /// </summary>
    public class StaticFileMiddleware
    {
        private readonly StaticFileOptions _options;
        private readonly PathString _matchUrl;
        private readonly RequestDelegate _next;

        /// <summary>
        /// Creates a new instance of the StaticFileMiddleware.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="options">The configuration options.</param>
        public StaticFileMiddleware(RequestDelegate next, StaticFileOptions options)
        {
            if (next == null)
            {
                throw new ArgumentNullException("next");
            }
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }
            if (options.ContentTypeProvider == null)
            {
                throw new ArgumentException(Resources.Args_NoContentTypeProvider);
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
        /// Processes a request to determine if it matches a known file, and if so, serves it.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task Invoke(HttpContext context)
        {
            var fileContext = new StaticFileContext(context, _options, _matchUrl);
            if (fileContext.ValidateMethod()
                && fileContext.ValidatePath()
                && fileContext.LookupContentType()
                && fileContext.LookupFileInfo())
            {
                fileContext.ComprehendRequestHeaders();

                switch (fileContext.GetPreconditionState())
                {
                    case StaticFileContext.PreconditionState.Unspecified:
                    case StaticFileContext.PreconditionState.ShouldProcess:
                        if (fileContext.IsHeadMethod)
                        {
                            return fileContext.SendStatusAsync(Constants.Status200Ok);
                        }
                        if (fileContext.IsRangeRequest)
                        {
                            return fileContext.SendRangeAsync();
                        }
                        return fileContext.SendAsync();

                    case StaticFileContext.PreconditionState.NotModified:
                        return fileContext.SendStatusAsync(Constants.Status304NotModified);

                    case StaticFileContext.PreconditionState.PreconditionFailed:
                        return fileContext.SendStatusAsync(Constants.Status412PreconditionFailed);

                    default:
                        throw new NotImplementedException(fileContext.GetPreconditionState().ToString());
                }
            }

            return _next(context);
        }
    }
}
