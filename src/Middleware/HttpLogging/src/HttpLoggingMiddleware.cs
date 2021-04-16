// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.HttpLogging
{
    /// <summary>
    /// Middleware that logs HTTP requests and HTTP responses.
    /// </summary>
    internal sealed class HttpLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private IOptionsMonitor<HttpLoggingOptions> _options;
        private const int DefaultRequestFieldsMinusHeaders = 7;
        private const int DefaultResponseFieldsMinusHeaders = 2;

        /// <summary>
        /// Initializes <see cref="HttpLoggingMiddleware" />.
        /// </summary>
        /// <param name="next"></param>
        /// <param name="options"></param>
        /// <param name="loggerFactory"></param>
        public HttpLoggingMiddleware(RequestDelegate next, IOptionsMonitor<HttpLoggingOptions> options, ILoggerFactory loggerFactory)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _options = options;
            _logger = loggerFactory.CreateLogger<HttpLoggingMiddleware>();
        }

        /// <summary>
        /// Invokes the <see cref="HttpLoggingMiddleware" />.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>HttpResponseLog.cs
        public async Task Invoke(HttpContext context)
        {
            var options = _options.CurrentValue;
            RequestBufferingStream? requestBufferingStream = null;
            Stream? originalBody = null;
            if ((HttpLoggingFields.Request & options.LoggingFields) != HttpLoggingFields.None)
            {
                var request = context.Request;
                var list = new List<KeyValuePair<string, object?>>(
                    request.Headers.Count + DefaultRequestFieldsMinusHeaders);

                if (options.LoggingFields.HasFlag(HttpLoggingFields.Protocol))
                {
                    AddToList(list, nameof(request.Protocol), request.Protocol);
                }

                if (options.LoggingFields.HasFlag(HttpLoggingFields.Method))
                {
                    AddToList(list, nameof(request.Method), request.Method);
                }

                if (options.LoggingFields.HasFlag(HttpLoggingFields.Scheme))
                {
                    AddToList(list, nameof(request.Scheme), request.Scheme);
                }

                if (options.LoggingFields.HasFlag(HttpLoggingFields.Path))
                {
                    AddToList(list, nameof(request.PathBase), request.PathBase);
                    AddToList(list, nameof(request.Path), request.Path);
                }

                if (options.LoggingFields.HasFlag(HttpLoggingFields.Query))
                {
                    AddToList(list, nameof(request.QueryString), request.QueryString.Value);
                }

                if (options.LoggingFields.HasFlag(HttpLoggingFields.RequestHeaders))
                {
                    FilterHeaders(list, request.Headers, options.AllowedRequestHeaders);
                }

                if (options.LoggingFields.HasFlag(HttpLoggingFields.RequestBody)
                    && MediaTypeHelpers.TryGetEncodingForMediaType(request.ContentType,
                        options.SupportedMediaTypes,
                        out var encoding))
                {
                    originalBody = request.Body;
                    requestBufferingStream = new RequestBufferingStream(
                        request.Body,
                        options.RequestBodyLogLimit,
                        _logger,
                        encoding);
                    request.Body = requestBufferingStream;
                }

                // TODO add and remove things from log.
                var httpRequestLog = new HttpRequestLog(list);

                _logger.Log(LogLevel.Information,
                     eventId: LoggerEventIds.RequestLog,
                     state: httpRequestLog,
                     exception: null,
                     formatter: HttpRequestLog.Callback);
            }

            ResponseBufferingStream? responseBufferingStream = null;
            IHttpResponseBodyFeature? originalBodyFeature = null;

            try
            {
                if ((HttpLoggingFields.Response & options.LoggingFields) == HttpLoggingFields.None)
                {
                    // Short circuit and don't replace response body.
                    await _next(context).ConfigureAwait(false);
                    return;
                }

                var response = context.Response;

                if (options.LoggingFields.HasFlag(HttpLoggingFields.ResponseBody))
                {
                    originalBodyFeature = context.Features.Get<IHttpResponseBodyFeature>()!;
                    // TODO pool these.
                    responseBufferingStream = new ResponseBufferingStream(originalBodyFeature,
                        options.ResponseBodyLogLimit,
                        _logger,
                        context,
                        options.SupportedMediaTypes);
                    response.Body = responseBufferingStream;
                }

                await _next(context).ConfigureAwait(false);
                var list = new List<KeyValuePair<string, object?>>(
                    response.Headers.Count + DefaultResponseFieldsMinusHeaders);

                if (options.LoggingFields.HasFlag(HttpLoggingFields.StatusCode))
                {
                    list.Add(new KeyValuePair<string, object?>(nameof(response.StatusCode), response.StatusCode));
                }

                if (options.LoggingFields.HasFlag(HttpLoggingFields.ResponseHeaders))
                {
                    FilterHeaders(list, response.Headers, options.AllowedResponseHeaders);
                }

                var httpResponseLog = new HttpResponseLog(list);

                _logger.Log(LogLevel.Information,
                    eventId: LoggerEventIds.ResponseLog,
                    state: httpResponseLog,
                    exception: null,
                    formatter: HttpResponseLog.Callback);

                if (responseBufferingStream != null)
                {
                    responseBufferingStream.LogString(responseBufferingStream.Encoding);
                }
            }
            finally
            {
                responseBufferingStream?.Dispose();

                if (originalBodyFeature != null)
                {
                    context.Features.Set(originalBodyFeature);
                }

                requestBufferingStream?.Dispose();

                if (originalBody != null)
                {
                    context.Request.Body = originalBody;
                }
            }
        }

        private static void AddToList(List<KeyValuePair<string, object?>> list, string key, string? value)
        {
            list.Add(new KeyValuePair<string, object?>(key, value));
        }

        private void FilterHeaders(List<KeyValuePair<string, object?>> keyValues,
            IHeaderDictionary headers,
            ISet<string> allowedHeaders)
        {
            foreach (var (key, value) in headers)
            {
                if (!allowedHeaders.Contains(key))
                {
                    // Key is not among the "only listed" headers.
                    keyValues.Add(new KeyValuePair<string, object?>(key, "X"));
                    continue;
                }
                keyValues.Add(new KeyValuePair<string, object?>(key, value.ToString()));
            }
        }
    }
}
