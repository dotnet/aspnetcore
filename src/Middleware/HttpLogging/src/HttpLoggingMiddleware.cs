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
        private HttpLoggingOptions _options;
        private const int PipeThreshold = 32 * 1024;
        private const int DefaultRequestFieldsMinusHeaders = 7;
        private const int DefaultResponseFieldsMinusHeaders = 2;

        /// <summary>
        /// Initializes <see cref="HttpLoggingMiddleware" />.
        /// </summary>
        /// <param name="next"></param>
        /// <param name="options"></param>
        /// <param name="loggerFactory"></param>
        public HttpLoggingMiddleware(RequestDelegate next, IOptions<HttpLoggingOptions> options, ILoggerFactory loggerFactory)

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

            _options = options.Value;
            _logger = loggerFactory.CreateLogger<HttpLoggingMiddleware>();
        }

        /// <summary>
        /// Invokes the <see cref="HttpLoggingMiddleware" />.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            if ((HttpLoggingFields.Request & _options.LoggingFields) != HttpLoggingFields.None)
            {
                var request = context.Request;
                var list = new List<KeyValuePair<string, object?>>(request.Headers.Count + DefaultRequestFieldsMinusHeaders);

                if (_options.LoggingFields.HasFlag(HttpLoggingFields.Protocol))
                {
                    AddToList(list, nameof(request.Protocol), request.Protocol);
                }

                if (_options.LoggingFields.HasFlag(HttpLoggingFields.Method))
                {
                    AddToList(list, nameof(request.Method), request.Method);
                }

                if (_options.LoggingFields.HasFlag(HttpLoggingFields.Scheme))
                {
                    AddToList(list, nameof(request.Scheme), request.Scheme);
                }

                if (_options.LoggingFields.HasFlag(HttpLoggingFields.Path))
                {
                    AddToList(list, nameof(request.PathBase), request.PathBase);
                    AddToList(list, nameof(request.Path), request.Path);
                }

                if (_options.LoggingFields.HasFlag(HttpLoggingFields.Query))
                {
                    AddToList(list, nameof(request.QueryString), request.QueryString.Value);
                }

                if (_options.LoggingFields.HasFlag(HttpLoggingFields.RequestHeaders))
                {
                    FilterHeaders(list, request.Headers, _options.AllowedRequestHeaders);
                }

                if (_options.LoggingFields.HasFlag(HttpLoggingFields.RequestBody) && IsSupportedMediaType(request.ContentType))
                {
                    var body = await ReadRequestBody(request, context.RequestAborted);

                    list.Add(new KeyValuePair<string, object?>(nameof(request.Body), body));
                }    

                // TODO add and remove things from log.
                var httpRequestLog = new HttpRequestLog(list);

                _logger.Log(LogLevel.Information,
                     eventId: LoggerEventIds.RequestLog,
                     state: httpRequestLog,
                     exception: null,
                     formatter: HttpRequestLog.Callback);
            }

            if ((HttpLoggingFields.Response & _options.LoggingFields) == HttpLoggingFields.None)
            {
                // Short circuit and don't replace response body.
                await _next(context).ConfigureAwait(false);
                return;
            }

            var response = context.Response;

            ResponseBufferingStream? bufferingStream = null;
            IHttpResponseBodyFeature? originalBodyFeature = null;
            if (_options.LoggingFields.HasFlag(HttpLoggingFields.ResponseBody))
            {
                originalBodyFeature = context.Features.Get<IHttpResponseBodyFeature>()!;
                // TODO pool these.
                bufferingStream = new ResponseBufferingStream(originalBodyFeature, _options.ResponseBodyLogLimit);
                response.Body = bufferingStream;
            }

            try
            {
                await _next(context).ConfigureAwait(false);
                var list = new List<KeyValuePair<string, object?>>(response.Headers.Count + DefaultResponseFieldsMinusHeaders);

                if (_options.LoggingFields.HasFlag(HttpLoggingFields.StatusCode))
                {
                    list.Add(new KeyValuePair<string, object?>(nameof(response.StatusCode), response.StatusCode));
                }

                if (_options.LoggingFields.HasFlag(HttpLoggingFields.ResponseHeaders))
                {
                    FilterHeaders(list, response.Headers, _options.AllowedResponseHeaders);
                }

                if (_options.LoggingFields.HasFlag(HttpLoggingFields.ResponseBody) && IsSupportedMediaType(response.ContentType))
                {
                    var body = bufferingStream!.GetString(_options.BodyEncoding);
                    list.Add(new KeyValuePair<string, object?>(nameof(response.Body), body));
                }

                var httpResponseLog = new HttpResponseLog(list);

                _logger.Log(LogLevel.Information,
                    eventId: LoggerEventIds.ResponseLog,
                    state: httpResponseLog,
                    exception: null,
                    formatter: HttpResponseLog.Callback);
            }
            finally
            {
                if (_options.LoggingFields.HasFlag(HttpLoggingFields.ResponseBody))
                {
                    bufferingStream?.Dispose();

                    context.Features.Set(originalBodyFeature);
                }
            }
        }

        private static void AddToList(List<KeyValuePair<string, object?>> list, string key, string? value)
        {
            list.Add(new KeyValuePair<string, object?>(key, value));
        }

        private bool IsSupportedMediaType(string contentType)
        {
            var mediaTypeList = _options.SupportedMediaTypes;
            if (mediaTypeList == null || mediaTypeList.Count == 0 || string.IsNullOrEmpty(contentType))
            {
                return false;
            }

            var mediaType = new MediaTypeHeaderValue(contentType);
            foreach (var type in mediaTypeList)
            {
                if (mediaType.IsSubsetOf(type))
                {
                    return true;
                }
            }

            return false;
        }

        private void FilterHeaders(List<KeyValuePair<string, object?>> keyValues, IHeaderDictionary headers, ISet<string> allowedHeaders)
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

        private async Task<string> ReadRequestBody(HttpRequest request, CancellationToken token)
        {
            if (_options.BodyEncoding == null)
            {
                return "X";
            }

            using var joinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            joinedTokenSource.CancelAfter(_options.RequestBodyTimeout);
            var limit = _options.RequestBodyLogLimit;

            // Use a pipe for smaller payload sizes
            if (limit <= PipeThreshold)
            {
                try
                {
                    while (true)
                    {
                        // TODO if someone uses the body after this, it will not have the rest of the data.
                        var result = await request.BodyReader.ReadAsync(joinedTokenSource.Token);
                        if (!result.IsCompleted && result.Buffer.Length <= limit)
                        {
                            // Need more data.
                            request.BodyReader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
                            continue;
                        }

                        var res = _options.BodyEncoding.GetString(result.Buffer.Slice(0, result.Buffer.Length > limit ? limit : result.Buffer.Length));

                        request.BodyReader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
                        return res;
                    }
                }
                catch (OperationCanceledException)
                {
                    // Token source hides triggering token (https://github.com/dotnet/runtime/issues/22172)
                    if (!token.IsCancellationRequested && joinedTokenSource.Token.IsCancellationRequested)
                    {
                        return "X";
                    }

                    throw;
                }
            }
            else
            {
                request.EnableBuffering();

                // Read here.
                var buffer = ArrayPool<byte>.Shared.Rent(limit);

                try
                {
                    var count = 0;
                    while (true)
                    {
                        var read = await request.Body.ReadAsync(buffer, count, limit - count);
                        count += read;

                        Debug.Assert(count <= limit);
                        if (read == 0 || count == limit)
                        {
                            break;
                        }
                    }

                    return _options.BodyEncoding.GetString(new Span<byte>(buffer).Slice(0, count));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                    // reseek back to start.
                    if (request.Body.CanSeek)
                    {
                        _ = request.Body.Seek(0, SeekOrigin.Begin);
                    }
                }
            }
        }
    }
}
