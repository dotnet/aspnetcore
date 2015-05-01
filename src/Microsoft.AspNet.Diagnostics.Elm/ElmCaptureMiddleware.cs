// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Diagnostics.Elm
{
    /// <summary>
    /// Enables the Elm logging service.
    /// </summary>
    public class ElmCaptureMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ElmOptions _options;
        private readonly ILogger _logger;

        public ElmCaptureMiddleware(RequestDelegate next, ILoggerFactory factory, IOptions<ElmOptions> options)
        {
            _next = next;
            _options = options.Options;
            _logger = factory.CreateLogger<ElmCaptureMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            var requestId = Guid.NewGuid();
            using (_logger.BeginScope(string.Format("request {0}", requestId)))
            {
                var p = ElmScope.Current;
                ElmScope.Current.Context.HttpInfo = GetHttpInfo(context, requestId);
                try
                {
                    await _next(context);
                }
                finally
                {
                    ElmScope.Current.Context.HttpInfo.StatusCode = context.Response.StatusCode;
                }
            }
        }

        /// <summary>
        /// Takes the info from the given HttpContext and copies it to an HttpInfo object
        /// </summary>
        /// <returns>The HttpInfo for the current elm context</returns>
        private static HttpInfo GetHttpInfo(HttpContext context, Guid requestId)
        {
            return new HttpInfo()
            {
                RequestID = requestId,
                Host = context.Request.Host,
                ContentType = context.Request.ContentType,
                Path = context.Request.Path,
                Scheme = context.Request.Scheme,
                StatusCode = context.Response.StatusCode,
                User = context.User,
                Method = context.Request.Method,
                Protocol = context.Request.Protocol,
                Headers = context.Request.Headers,
                Query = context.Request.QueryString,
                Cookies = context.Request.Cookies
            };
        }
    }
}