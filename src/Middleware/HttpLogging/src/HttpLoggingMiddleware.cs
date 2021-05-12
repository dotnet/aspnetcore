// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
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
        private readonly IOptionsMonitor<HttpLoggingOptions> _options;
        private const int DefaultRequestFieldsMinusHeaders = 7;
        private const int DefaultResponseFieldsMinusHeaders = 2;
        private const string Redacted = "[Redacted]";

        /// <summary>
        /// Initializes <see cref="HttpLoggingMiddleware" />.
        /// </summary>
        /// <param name="next"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public HttpLoggingMiddleware(RequestDelegate next, IOptionsMonitor<HttpLoggingOptions> options, ILogger<HttpLoggingMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _options = options;
            _logger = logger;
        }

        /// <summary>
        /// Invokes the <see cref="HttpLoggingMiddleware" />.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>HttpResponseLog.cs
        public Task Invoke(HttpContext context)
        {
            if (!_logger.IsEnabled(LogLevel.Information))
            {
                // Logger isn't enabled.
                return _next(context);
            }

            return InvokeInternal(context);
        }

        private async Task InvokeInternal(HttpContext context)
        {
            var options = _options.CurrentValue;
            RequestBufferingStream? requestBufferingStream = null;
            Stream? originalBody = null;

            if ((HttpLoggingFields.Request & options.LoggingFields) != HttpLoggingFields.None)
            {
                var request = context.Request;
                var list = new List<KeyValuePair<string, string?>>(
                    request.Headers.Count + DefaultRequestFieldsMinusHeaders);

                if (options.ModifyRequestLog != null)
                {
                    await HandleModifableRequestLog(context, options, request, list);
                }
                else
                {
                    HandleFixedRequestLog(options, request, list);
                }

                if (options.LoggingFields.HasFlag(HttpLoggingFields.RequestBody))
                {
                    if (MediaTypeHelpers.TryGetEncodingForMediaType(request.ContentType,
                        options.MediaTypeOptions.MediaTypeStates,
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
                    else
                    {
                        _logger.UnrecognizedMediaType();
                    }
                }

                var httpRequestLog = new HttpRequestLog(list);

                _logger.RequestLog(httpRequestLog);
            }

            ResponseBufferingStream? responseBufferingStream = null;
            IHttpResponseBodyFeature? originalBodyFeature = null;

            try
            {
                var response = context.Response;

                if (options.LoggingFields.HasFlag(HttpLoggingFields.ResponseBody))
                {
                    originalBodyFeature = context.Features.Get<IHttpResponseBodyFeature>()!;

                    // TODO pool these.
                    responseBufferingStream = new ResponseBufferingStream(originalBodyFeature,
                        options.ResponseBodyLogLimit,
                        _logger,
                        context,
                        options.MediaTypeOptions.MediaTypeStates,
                        options);
                    response.Body = responseBufferingStream;
                    context.Features.Set<IHttpResponseBodyFeature>(responseBufferingStream);
                }

                await _next(context);

                if (requestBufferingStream?.HasLogged == false)
                {
                    // If the middleware pipeline didn't read until 0 was returned from readasync,
                    // make sure we log the request body.
                    requestBufferingStream.LogRequestBody();
                }

                if (responseBufferingStream == null || responseBufferingStream.FirstWrite == false)
                {
                    // No body, write headers here.
                    await LogResponseHeaders(context, options, _logger);
                }

                if (responseBufferingStream != null)
                {
                    var responseBody = responseBufferingStream.GetString(responseBufferingStream.Encoding);
                    if (!string.IsNullOrEmpty(responseBody))
                    {
                        _logger.ResponseBody(responseBody);
                    }
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

        private static void HandleFixedRequestLog(HttpLoggingOptions options, HttpRequest request, List<KeyValuePair<string, string?>> list)
        {
            if (options.LoggingFields.HasFlag(HttpLoggingFields.RequestProtocol))
            {
                AddToList(list, nameof(request.Protocol), request.Protocol);
            }

            if (options.LoggingFields.HasFlag(HttpLoggingFields.RequestMethod))
            {
                AddToList(list, nameof(request.Method), request.Method);
            }

            if (options.LoggingFields.HasFlag(HttpLoggingFields.RequestScheme))
            {
                AddToList(list, nameof(request.Scheme), request.Scheme);
            }

            if (options.LoggingFields.HasFlag(HttpLoggingFields.RequestPath))
            {
                AddToList(list, nameof(request.PathBase), request.PathBase);
                AddToList(list, nameof(request.Path), request.Path);
            }

            if (options.LoggingFields.HasFlag(HttpLoggingFields.RequestQuery))
            {
                AddToList(list, nameof(request.QueryString), request.QueryString.Value);
            }

            if (options.LoggingFields.HasFlag(HttpLoggingFields.RequestHeaders))
            {
                FilterHeaders(list, request.Headers, options._internalRequestHeaders);
            }
        }

        private static async Task HandleModifableRequestLog(HttpContext context, HttpLoggingOptions options, HttpRequest request, List<KeyValuePair<string, string?>> list)
        {
            var headerDictionary = new HeaderDictionary();
            var loggingContext = new HttpRequestLoggingContext(context, options, headerDictionary);

            if (options.LoggingFields.HasFlag(HttpLoggingFields.RequestProtocol))
            {
                loggingContext.Protocol = request.Protocol;
            }

            if (options.LoggingFields.HasFlag(HttpLoggingFields.RequestMethod))
            {
                loggingContext.Method = request.Method;
            }

            if (options.LoggingFields.HasFlag(HttpLoggingFields.RequestScheme))
            {
                loggingContext.Scheme = request.Scheme;
            }

            if (options.LoggingFields.HasFlag(HttpLoggingFields.RequestPath))
            {
                loggingContext.PathBase = request.PathBase;
                loggingContext.Path = request.Path;
            }

            if (options.LoggingFields.HasFlag(HttpLoggingFields.RequestQuery))
            {
                loggingContext.Query = request.QueryString.Value;
            }

            if (options.LoggingFields.HasFlag(HttpLoggingFields.RequestHeaders))
            {
                FilterHeaders(headerDictionary, request.Headers, options._internalRequestHeaders);
            }

            await options.ModifyRequestLog!(loggingContext);

            // Checking for null to make sure the key isn't logged if it isn't enabled.
            if (loggingContext.Protocol != null)
            {
                AddToList(list, nameof(request.Protocol), loggingContext.Protocol);
            }

            if (loggingContext.Method != null)
            {
                AddToList(list, nameof(request.Method), loggingContext.Method);
            }

            if (loggingContext.Scheme != null)
            {
                AddToList(list, nameof(request.Scheme), loggingContext.Scheme);
            }

            if (loggingContext.PathBase != null)
            {
                AddToList(list, nameof(request.PathBase), loggingContext.PathBase);
            }

            if (loggingContext.Path != null)
            {
                AddToList(list, nameof(request.Path), loggingContext.Path);
            }

            if (loggingContext.Query != null)
            {
                AddToList(list, nameof(request.QueryString), loggingContext.Query);
            }

            AddHeaders(list, headerDictionary);

            foreach (var item in loggingContext.Extra)
            {
                AddToList(list, item.Item1, item.Item2);
            }
        }

        private static void AddToList(List<KeyValuePair<string, string?>> list, string key, string? value)
        {
            list.Add(new KeyValuePair<string, string?>(key, value));
        }

        public static async ValueTask LogResponseHeaders(HttpContext context, HttpLoggingOptions options, ILogger logger)
        {
            var response = context.Response;
            var list = new List<KeyValuePair<string, string?>>(
                response.Headers.Count + DefaultResponseFieldsMinusHeaders);

            if (options.ModifyResponseLog != null)
            {
                var headerDictionary = new HeaderDictionary();
                var responseLoggingContext = new HttpResponseLoggingContext(context, options, headerDictionary);

                if (options.LoggingFields.HasFlag(HttpLoggingFields.ResponseStatusCode))
                {
                    responseLoggingContext.StatusCode = response.StatusCode.ToString(CultureInfo.InvariantCulture);
                }

                if (options.LoggingFields.HasFlag(HttpLoggingFields.RequestHeaders))
                {
                    FilterHeaders(headerDictionary, response.Headers, options._internalRequestHeaders);
                }

                await options.ModifyResponseLog(responseLoggingContext);

                if (responseLoggingContext.StatusCode != null)
                {
                    list.Add(new KeyValuePair<string, string?>(nameof(response.StatusCode), responseLoggingContext.StatusCode));
                }

                AddHeaders(list, responseLoggingContext.Headers);
                foreach (var item in responseLoggingContext.Extra)
                {
                    AddToList(list, item.Item1, item.Item2);
                }
            }
            else
            {
                if (options.LoggingFields.HasFlag(HttpLoggingFields.ResponseStatusCode))
                {
                    list.Add(new KeyValuePair<string, string?>(nameof(response.StatusCode),
                        response.StatusCode.ToString(CultureInfo.InvariantCulture)));
                }

                if (options.LoggingFields.HasFlag(HttpLoggingFields.ResponseHeaders))
                {
                    FilterHeaders(list, response.Headers, options._internalResponseHeaders);
                }
            }

            var httpResponseLog = new HttpResponseLog(list);

            logger.ResponseLog(httpResponseLog);
        }

        internal static void FilterHeaders(List<KeyValuePair<string, string?>> keyValues,
            IHeaderDictionary headers,
            HashSet<string> allowedHeaders)
        {
            foreach (var (key, value) in headers)
            {
                if (!allowedHeaders.Contains(key))
                {
                    // Key is not among the "only listed" headers.
                    keyValues.Add(new KeyValuePair<string, string?>(key, Redacted));
                    continue;
                }
                keyValues.Add(new KeyValuePair<string, string?>(key, value.ToString()));
            }
        }

        internal static void FilterHeaders(HeaderDictionary output,
            IHeaderDictionary input,
            HashSet<string> allowedHeaders)
        {
            foreach (var (key, value) in input)
            {
                if (!allowedHeaders.Contains(key))
                {
                    // Key is not among the "only listed" headers.
                    output.Add(new KeyValuePair<string, StringValues>(key, Redacted));
                    continue;
                }
                output.Add(new KeyValuePair<string, StringValues>(key, value.ToString()));
            }
        }

        internal static void AddHeaders(List<KeyValuePair<string, string?>> output,
          IHeaderDictionary input)
        {
            foreach (var (key, value) in input)
            {
                output.Add(new KeyValuePair<string, string?>(key, value.ToString()));
            }
        }
    }
}
