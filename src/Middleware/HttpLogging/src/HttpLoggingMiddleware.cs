// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
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
        private readonly ILogger _httpLogger;
        private readonly ILogger _w3cLogger;
        private readonly IOptionsMonitor<HttpLoggingOptions> _options;
        private readonly IOptionsMonitor<LoggerFilterOptions> _filterOptions;
        private const int DefaultRequestFieldsMinusHeaders = 7;
        private const int DefaultResponseFieldsMinusHeaders = 2;
        private const string Redacted = "[Redacted]";

        /// <summary>
        /// Initializes <see cref="HttpLoggingMiddleware" />.
        /// </summary>
        /// <param name="next"></param>
        /// <param name="options"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="filterOptions"></param>
        public HttpLoggingMiddleware(RequestDelegate next, IOptionsMonitor<HttpLoggingOptions> options, ILoggerFactory loggerFactory, IOptionsMonitor<LoggerFilterOptions> filterOptions)
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

            _filterOptions = filterOptions;
            _options = options;

            // By default, disable sending events to W3CLogger
            // TODO - it seems odd to add this rule so late. Would it be better to add it in the extension method that adds HttpLoggingMiddleware?
            // Somewhere else?
            _filterOptions.CurrentValue.Rules.Add(new LoggerFilterRule(null, "Microsoft.AspNetCore.W3CLogging", LogLevel.None, null));

            _httpLogger = loggerFactory.CreateLogger<HttpLoggingMiddleware>();
            // TODO - change this, maybe proxy type
            _w3cLogger = loggerFactory.CreateLogger("Microsoft.AspNetCore.W3CLogging");
        }

        /// <summary>
        /// Invokes the <see cref="HttpLoggingMiddleware" />.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>HttpResponseLog.cs
        public Task Invoke(HttpContext context)
        {
            var httpEnabled = _httpLogger.IsEnabled(LogLevel.Information);
            var w3cEnabled = _w3cLogger.IsEnabled(LogLevel.Information);
            if (!httpEnabled && !w3cEnabled)
            {
                // Logger isn't enabled.
                return _next(context);
            }

            return InvokeInternal(context, httpEnabled, w3cEnabled);
        }

        private async Task InvokeInternal(HttpContext context, bool httpEnabled, bool w3cEnabled)
        {
            var options = _options.CurrentValue;

            var w3cList = new List<KeyValuePair<string, object?>>();

            if (w3cEnabled)
            {
                if (options.LoggingFields.HasFlag(HttpLoggingFields.DateTime))
                {
                    AddToList(w3cList, nameof(DateTime), DateTime.Now.ToString(CultureInfo.InvariantCulture));
                }

                if (options.LoggingFields.HasFlag(HttpLoggingFields.UserName))
                {
                    AddToList(w3cList, nameof(HttpContext.User), context.User is null ? "" : (context.User.Identity is null ? "" : (context.User.Identity.Name is null ? "" : context.User.Identity.Name)));
                }

                if ((HttpLoggingFields.ConnectionInfoFields & options.LoggingFields) != HttpLoggingFields.None)
                {
                    var connectionInfo = context.Connection;

                    if (options.LoggingFields.HasFlag(HttpLoggingFields.ClientIpAddress))
                    {
                        AddToList(w3cList, nameof(ConnectionInfo.RemoteIpAddress), connectionInfo.RemoteIpAddress is null ? "" : connectionInfo.RemoteIpAddress.ToString());
                    }

                    if (options.LoggingFields.HasFlag(HttpLoggingFields.ServerIpAddress))
                    {
                        AddToList(w3cList, nameof(ConnectionInfo.LocalIpAddress), connectionInfo.LocalIpAddress is null ? "" : connectionInfo.LocalIpAddress.ToString());
                    }

                    if (options.LoggingFields.HasFlag(HttpLoggingFields.ServerPort))
                    {
                        AddToList(w3cList, nameof(ConnectionInfo.LocalPort), connectionInfo.LocalPort.ToString(CultureInfo.InvariantCulture));
                    }
                }
            }

            RequestBufferingStream ? requestBufferingStream = null;
            Stream? originalBody = null;

            if ((HttpLoggingFields.Request & options.LoggingFields) != HttpLoggingFields.None)
            {
                var request = context.Request;
                var list = new List<KeyValuePair<string, object?>>(
                    request.Headers.Count + DefaultRequestFieldsMinusHeaders);

                if (options.LoggingFields.HasFlag(HttpLoggingFields.RequestProtocol))
                {
                    if (httpEnabled)
                    {
                        AddToList(list, nameof(request.Protocol), request.Protocol);
                    }
                    if (w3cEnabled)
                    {
                        AddToList(w3cList, nameof(request.Protocol), request.Protocol);
                    }
                }

                if (options.LoggingFields.HasFlag(HttpLoggingFields.RequestMethod))
                {
                    if (httpEnabled)
                    {
                        AddToList(list, nameof(request.Method), request.Method);
                    }
                    if (w3cEnabled)
                    {
                        AddToList(w3cList, nameof(request.Method), request.Method);
                    }
                }

                if (httpEnabled && options.LoggingFields.HasFlag(HttpLoggingFields.RequestScheme))
                {
                    AddToList(list, nameof(request.Scheme), request.Scheme);
                }

                if (httpEnabled && options.LoggingFields.HasFlag(HttpLoggingFields.RequestPath))
                {
                    AddToList(list, nameof(request.PathBase), request.PathBase);
                    AddToList(list, nameof(request.Path), request.Path);
                }

                if (httpEnabled && options.LoggingFields.HasFlag(HttpLoggingFields.RequestQueryString))
                {
                    AddToList(list, nameof(request.QueryString), request.QueryString.Value);
                }

                if (w3cEnabled && options.LoggingFields.HasFlag(HttpLoggingFields.RequestQuery))
                {
                    // TODO - query is written as a list of {Key}:{Value} pairs delimited by semicolon -
                    // is there a better/standardized format?
                    var query = request.Query;
                    StringBuilder sb = new StringBuilder();
                    foreach (string key in query.Keys)
                    {
                        sb.Append(key);
                        sb.Append(':');
                        sb.Append(query[key]);
                        sb.Append(';');
                    }
                    AddToList(w3cList, nameof(request.Query), sb.ToString());
                }

                if (options.LoggingFields.HasFlag(HttpLoggingFields.RequestHeaders))
                {
                    if (httpEnabled)
                    {
                        FilterHeaders(list, request.Headers, options._internalHttpRequestHeaders);
                    }
                    if (w3cEnabled)
                    {
                        WriteHeaders(w3cList, request.Headers, options._internalW3CRequestHeaders);
                    }
                }

                if (w3cEnabled && options.LoggingFields.HasFlag(HttpLoggingFields.RequestCookie))
                {
                    // TODO - cookies are written as a list of {Key}:{Value} pairs delimited by semicolon -
                    // is there a better/standardized format?
                    var cookies = request.Cookies;
                    StringBuilder sb = new StringBuilder();
                    foreach (string key in cookies.Keys)
                    {
                        sb.Append(key);
                        sb.Append(':');
                        sb.Append(cookies[key]);
                        sb.Append(';');
                    }
                    AddToList(w3cList, nameof(request.Cookies), sb.ToString());
                }

                if (httpEnabled && options.LoggingFields.HasFlag(HttpLoggingFields.RequestBody))
                {
                    if (MediaTypeHelpers.TryGetEncodingForMediaType(request.ContentType,
                        options.MediaTypeOptions.MediaTypeStates,
                        out var encoding))
                    {
                        originalBody = request.Body;
                        requestBufferingStream = new RequestBufferingStream(
                            request.Body,
                            options.RequestBodyLogLimit,
                            _httpLogger,
                            encoding);
                        request.Body = requestBufferingStream;
                    }
                    else
                    {
                        _httpLogger.UnrecognizedMediaType();
                    }
                }

                if (httpEnabled)
                {
                    var httpRequestLog = new HttpRequestLog(list);

                    _httpLogger.RequestLog(httpRequestLog);
                }
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
                        _httpLogger,
                        context,
                        options.MediaTypeOptions.MediaTypeStates,
                        options);
                    response.Body = responseBufferingStream;
                    context.Features.Set<IHttpResponseBodyFeature>(responseBufferingStream);
                }

                await _next(context);

                if (httpEnabled && requestBufferingStream?.HasLogged == false)
                {
                    // If the middleware pipeline didn't read until 0 was returned from readasync,
                    // make sure we log the request body.
                    requestBufferingStream.LogRequestBody();
                }

                if (w3cEnabled && options.LoggingFields.HasFlag(HttpLoggingFields.ResponseStatusCode))
                {
                    w3cList.Add(new KeyValuePair<string, object?>(nameof(response.StatusCode),
                        response.StatusCode.ToString(CultureInfo.InvariantCulture)));
                }

                if (w3cEnabled && options.LoggingFields.HasFlag(HttpLoggingFields.ResponseHeaders))
                {
                    WriteHeaders(w3cList, response.Headers, options._internalW3CResponseHeaders);
                }

                if (httpEnabled && (responseBufferingStream == null || responseBufferingStream.FirstWrite == false))
                {
                    // No body, write headers here.
                    LogResponseHeaders(response, options, _httpLogger);
                }

                if (httpEnabled && responseBufferingStream != null)
                {
                    var responseBody = responseBufferingStream.GetString(responseBufferingStream.Encoding);
                    if (!string.IsNullOrEmpty(responseBody))
                    {
                        _httpLogger.ResponseBody(responseBody);
                    }
                }
                if (w3cEnabled && w3cList.Count > 0)
                {
                    var httpW3CLog = new HttpW3CLog(w3cList);
                    _w3cLogger.W3CLog(httpW3CLog);
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

        public static void LogResponseHeaders(HttpResponse response, HttpLoggingOptions options, ILogger logger)
        {
            var list = new List<KeyValuePair<string, object?>>(
                response.Headers.Count + DefaultResponseFieldsMinusHeaders);

            if (options.LoggingFields.HasFlag(HttpLoggingFields.ResponseStatusCode))
            {
                list.Add(new KeyValuePair<string, object?>(nameof(response.StatusCode), response.StatusCode));
            }

            if (options.LoggingFields.HasFlag(HttpLoggingFields.ResponseHeaders))
            {
                FilterHeaders(list, response.Headers, options._internalHttpResponseHeaders);
            }

            var httpResponseLog = new HttpResponseLog(list);

            logger.ResponseLog(httpResponseLog);
        }

        internal static void FilterHeaders(List<KeyValuePair<string, object?>> keyValues,
            IHeaderDictionary headers,
            HashSet<string> allowedHeaders)
        {
            foreach (var (key, value) in headers)
            {
                if (!allowedHeaders.Contains(key))
                {
                    // Key is not among the "only listed" headers.
                    keyValues.Add(new KeyValuePair<string, object?>(key, Redacted));
                    continue;
                }
                keyValues.Add(new KeyValuePair<string, object?>(key, value.ToString()));
            }
        }

        internal static void WriteHeaders(List<KeyValuePair<string, object?>> keyValues,
            IHeaderDictionary headers,
            HashSet<string> allowedHeaders)
        {
            foreach (var (key, value) in headers)
            {
                if (allowedHeaders.Contains(key))
                {
                    keyValues.Add(new KeyValuePair<string, object?>(key, value.ToString()));
                }
            }
        }
    }
}
