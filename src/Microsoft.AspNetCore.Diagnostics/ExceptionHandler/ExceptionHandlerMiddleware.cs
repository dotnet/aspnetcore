// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Diagnostics
{
    public class ExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ExceptionHandlerOptions _options;
        private readonly ILogger _logger;
        private readonly Func<object, Task> _clearCacheHeadersDelegate;
        private readonly DiagnosticSource _diagnosticSource;

        public ExceptionHandlerMiddleware(
            RequestDelegate next,
            ILoggerFactory loggerFactory,
            IOptions<ExceptionHandlerOptions> options,
            DiagnosticSource diagnosticSource)
        {
            _next = next;
            _options = options.Value;
            _logger = loggerFactory.CreateLogger<ExceptionHandlerMiddleware>();
            _clearCacheHeadersDelegate = ClearCacheHeaders;
            _diagnosticSource = diagnosticSource;
            if (_options.ExceptionHandler == null)
            {
                if (_options.ExceptionHandlingPath == null)
                {
                    throw new InvalidOperationException(Resources.FormatExceptionHandlerOptions_NotConfiguredCorrectly());
                }
                else
                {
                    _options.ExceptionHandler = _next;
                }
            }
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.UnhandledException(ex);
                // We can't do anything if the response has already started, just abort.
                if (context.Response.HasStarted)
                {
                    _logger.ResponseStartedErrorHandler();
                    throw;
                }

                PathString originalPath = context.Request.Path;
                if (_options.ExceptionHandlingPath.HasValue)
                {
                    context.Request.Path = _options.ExceptionHandlingPath;
                }
                try
                {
                    context.Response.Clear();
                    var exceptionHandlerFeature = new ExceptionHandlerFeature()
                    {
                        Error = ex,
                        Path = originalPath.Value,
                    };
                    context.Features.Set<IExceptionHandlerFeature>(exceptionHandlerFeature);
                    context.Features.Set<IExceptionHandlerPathFeature>(exceptionHandlerFeature);
                    context.Response.StatusCode = 500;
                    context.Response.OnStarting(_clearCacheHeadersDelegate, context.Response);

                    await _options.ExceptionHandler(context);

                    if (_diagnosticSource.IsEnabled("Microsoft.AspNetCore.Diagnostics.HandledException"))
                    {
                        _diagnosticSource.Write("Microsoft.AspNetCore.Diagnostics.HandledException", new { httpContext = context, exception = ex });
                    }

                    // TODO: Optional re-throw? We'll re-throw the original exception by default if the error handler throws.
                    return;
                }
                catch (Exception ex2)
                {
                    // Suppress secondary exceptions, re-throw the original.
                    _logger.ErrorHandlerException(ex2);
                }
                finally
                {
                    context.Request.Path = originalPath;
                }
                throw; // Re-throw the original if we couldn't handle it
            }
        }

        private Task ClearCacheHeaders(object state)
        {
            var response = (HttpResponse)state;
            response.Headers[HeaderNames.CacheControl] = "no-cache";
            response.Headers[HeaderNames.Pragma] = "no-cache";
            response.Headers[HeaderNames.Expires] = "-1";
            response.Headers.Remove(HeaderNames.ETag);
            return Task.CompletedTask;
        }
    }
}