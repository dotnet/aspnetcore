// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.StaticFiles
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
        private readonly IFileProvider _fileProvider;
        private readonly IContentTypeProvider _contentTypeProvider;

        /// <summary>
        /// Creates a new instance of the StaticFileMiddleware.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="hostingEnv">The <see cref="IHostingEnvironment"/> used by this middleware.</param>
        /// <param name="options">The configuration options.</param>
        /// <param name="loggerFactory">An <see cref="ILoggerFactory"/> instance used to create loggers.</param>
        public StaticFileMiddleware(RequestDelegate next, IHostingEnvironment hostingEnv, IOptions<StaticFileOptions> options, ILoggerFactory loggerFactory)
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

            _next = next;
            _options = options.Value;
            _contentTypeProvider = options.Value.ContentTypeProvider ?? new FileExtensionContentTypeProvider();
            _fileProvider = _options.FileProvider ?? Helpers.ResolveFileProvider(hostingEnv);
            _matchUrl = _options.RequestPath;
            _logger = loggerFactory.CreateLogger<StaticFileMiddleware>();
        }

        /// <summary>
        /// Processes a request to determine if it matches a known file, and if so, serves it.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            var fileContext = new StaticFileContext(context, _options, _matchUrl, _logger, _fileProvider, _contentTypeProvider);

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
                            await fileContext.SendStatusAsync(Constants.Status200Ok);
                            return;
                        }

                        try
                        {
                            if (fileContext.IsRangeRequest)
                            {
                                await fileContext.SendRangeAsync();
                                return;
                            }

                            await fileContext.SendAsync();
                            _logger.LogFileServed(fileContext.SubPath, fileContext.PhysicalPath);
                            return;
                        }
                        catch (FileNotFoundException)
                        {
                            context.Response.Clear();
                        }
                        break;
                    case StaticFileContext.PreconditionState.NotModified:
                        _logger.LogPathNotModified(fileContext.SubPath);
                        await fileContext.SendStatusAsync(Constants.Status304NotModified);
                        return;

                    case StaticFileContext.PreconditionState.PreconditionFailed:
                        _logger.LogPreconditionFailed(fileContext.SubPath);
                        await fileContext.SendStatusAsync(Constants.Status412PreconditionFailed);
                        return;

                    default:
                        var exception = new NotImplementedException(fileContext.GetPreconditionState().ToString());
                        Debug.Fail(exception.ToString());
                        throw exception;
                }
            }

            await _next(context);
        }
    }
}
