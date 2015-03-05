// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Logging;

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
        public StaticFileMiddleware([NotNull] RequestDelegate next, [NotNull] IHostingEnvironment hostingEnv, [NotNull] StaticFileOptions options, [NotNull] ILoggerFactory loggerFactory)
        {
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
                        if (_logger.IsEnabled(LogLevel.Verbose))
                        {
                            _logger.LogVerbose(string.Format("Copying file {0} to the response body", fileContext.SubPath));
                        }
                        return fileContext.SendAsync();

                    case StaticFileContext.PreconditionState.NotModified:
                        if (_logger.IsEnabled(LogLevel.Verbose))
                        {
                            _logger.LogVerbose(string.Format("{0} not modified", fileContext.SubPath));
                        }
                        return fileContext.SendStatusAsync(Constants.Status304NotModified);

                    case StaticFileContext.PreconditionState.PreconditionFailed:
                        return fileContext.SendStatusAsync(Constants.Status412PreconditionFailed);

                    default:
                        var exception = new NotImplementedException(fileContext.GetPreconditionState().ToString());
                        _logger.LogError("No precondition state specified", exception);
                        throw exception;
                }
            }

            return _next(context);
        }
    }
}
