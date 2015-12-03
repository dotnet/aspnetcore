// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Logging;

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
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new instance of the StaticFileMiddleware.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="options">The configuration options.</param>
        /// <param name="loggerFactory">An <see cref="ILoggerFactory"/> instance used to create loggers.</param>
        public StaticFileMiddleware(RequestDelegate next, IHostingEnvironment hostingEnv, StaticFileOptions options, ILoggerFactory loggerFactory)
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

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (options.ContentTypeProvider == null)
            {
                throw new ArgumentException(Resources.Args_NoContentTypeProvider);
            }
            options.ResolveFileProvider(hostingEnv);

            _next = next;
            _options = options;
            _matchUrl = options.RequestPath;
            _logger = loggerFactory.CreateLogger<StaticFileMiddleware>();
        }

        /// <summary>
        /// Processes a request to determine if it matches a known file, and if so, serves it.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task Invoke(HttpContext context)
        {
            var fileContext = new StaticFileContext(context, _options, _matchUrl, _logger);
          
            if (!fileContext.ValidateMethod())
            {
                _logger.LogRequestMethodNotSupported(context.Request.Method);
            }
            else if (!fileContext.ValidatePath())
            {
                _logger.LogPathMismatch(fileContext.SubPath);
            }
            else if (!fileContext.LookupContentType())
            {
                _logger.LogFileTypeNotSupported(fileContext.SubPath);
            }
            else if (!fileContext.LookupFileInfo())
            {
                _logger.LogFileNotFound(fileContext.SubPath);
            }
            else
            { 
                // If we get here, we can try to serve the file
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
                        
                        _logger.LogFileServed(fileContext.SubPath, fileContext.PhysicalPath);
                        return fileContext.SendAsync();

                    case StaticFileContext.PreconditionState.NotModified:
                        _logger.LogPathNotModified(fileContext.SubPath);
                        return fileContext.SendStatusAsync(Constants.Status304NotModified);

                    case StaticFileContext.PreconditionState.PreconditionFailed:
                        _logger.LogPreconditionFailed(fileContext.SubPath);
                        return fileContext.SendStatusAsync(Constants.Status412PreconditionFailed);

                    default:
                        var exception = new NotImplementedException(fileContext.GetPreconditionState().ToString());
                        Debug.Fail(exception.ToString());
                        throw exception;
                }
            }

            return _next(context);
        }
    }
}
