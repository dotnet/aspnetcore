// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Watch.BrowserRefresh
{
    public class BrowserRefreshMiddleware
    {
        private static readonly MediaTypeHeaderValue _textHtmlMediaType = new MediaTypeHeaderValue("text/html");
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public BrowserRefreshMiddleware(RequestDelegate next, ILogger<BrowserRefreshMiddleware> logger) =>
            (_next, _logger) = (next, logger);

        public async Task InvokeAsync(HttpContext context)
        {
            // We only need to support this for requests that could be initiated by a browser.
            if (IsBrowserRequest(context))
            {
                // Use a custom StreamWrapper to rewrite output on Write/WriteAsync
                using var responseStreamWrapper = new ResponseStreamWrapper(context, _logger);
                var originalBodyFeature = context.Features.Get<IHttpResponseBodyFeature>();
                context.Features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(responseStreamWrapper));

                try
                {
                    await _next(context);
                }
                finally
                {
                    context.Features.Set(originalBodyFeature);
                }

                if (responseStreamWrapper.IsHtmlResponse && _logger.IsEnabled(LogLevel.Debug))
                {
                    if (responseStreamWrapper.ScriptInjectionPerformed)
                    {
                        Log.BrowserConfiguredForRefreshes(_logger);
                    }
                    else
                    {
                        Log.FailedToConfiguredForRefreshes(_logger);
                    }
                }
            }
            else
            {
                await _next(context);
            }
        }

        internal static bool IsBrowserRequest(HttpContext context)
        {
            var request = context.Request;
            if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsPost(request.Method))
            {
                return false;
            }

            var typedHeaders = request.GetTypedHeaders();
            if (!(typedHeaders.Accept is IList<MediaTypeHeaderValue> acceptHeaders))
            {
                return false;
            }

            for (var i = 0; i < acceptHeaders.Count; i++)
            {
                if (acceptHeaders[i].IsSubsetOf(_textHtmlMediaType))
                {
                    return true;
                }
            }

            return false;
        }

        internal static class Log
        {
            private static readonly Action<ILogger, Exception?> _setupResponseForBrowserRefresh = LoggerMessage.Define(
               LogLevel.Debug,
                new EventId(1, "SetUpResponseForBrowserRefresh"),
               "Response markup is scheduled to include browser refresh script injection.");

            private static readonly Action<ILogger, Exception?> _browserConfiguredForRefreshes = LoggerMessage.Define(
               LogLevel.Debug,
                new EventId(2, "BrowserConfiguredForRefreshes"),
               "Response markup was updated to include browser refresh script injection.");

            private static readonly Action<ILogger, Exception?> _failedToConfigureForRefreshes = LoggerMessage.Define(
               LogLevel.Debug,
                new EventId(3, "FailedToConfiguredForRefreshes"),
               "Unable to configure browser refresh script injection on the response.");

            public static void SetupResponseForBrowserRefresh(ILogger logger) => _setupResponseForBrowserRefresh(logger, null);
            public static void BrowserConfiguredForRefreshes(ILogger logger) => _browserConfiguredForRefreshes(logger, null);
            public static void FailedToConfiguredForRefreshes(ILogger logger) => _failedToConfigureForRefreshes(logger, null);
        }
    }
}
