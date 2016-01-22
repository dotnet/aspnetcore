// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics.Elm
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
            _options = options.Value;
            _logger = factory.CreateLogger<ElmCaptureMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            using (RequestIdentifier.Ensure(context))
            {
                var requestId = context.Features.Get<IHttpRequestIdentifierFeature>().TraceIdentifier;
                using (_logger.BeginScope("Request: {RequestId}", requestId))
                {
                    try
                    {
                        ElmScope.Current.Context.HttpInfo = GetHttpInfo(context);
                        await _next(context);
                    }
                    finally
                    {
                        ElmScope.Current.Context.HttpInfo.StatusCode = context.Response.StatusCode;
                    }
                }
            }
        }

        /// <summary>
        /// Takes the info from the given HttpContext and copies it to an HttpInfo object
        /// </summary>
        /// <returns>The HttpInfo for the current elm context</returns>
        private static HttpInfo GetHttpInfo(HttpContext context)
        {
            return new HttpInfo()
            {
                RequestID = context.Features.Get<IHttpRequestIdentifierFeature>().TraceIdentifier,
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