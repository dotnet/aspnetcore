// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HttpLogging
{
    /// <summary>
    /// Middleware that logs HTTP requests and HTTP responses.
    /// </summary>
    public class HttpLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly IConfiguration _config;
        private readonly ILogger _logger;
        private HttpLoggingOptions _options;
        private const int PipeThreshold = 32 * 1024;

        /// <summary>
        /// Initializes <see cref="HttpLoggingMiddleware" />.
        /// </summary>
        /// <param name="next"></param>
        /// <param name="options"></param>
        /// <param name="config"></param>
        /// <param name="loggerFactory"></param>
        public HttpLoggingMiddleware(RequestDelegate next, IOptions<HttpLoggingOptions> options, IConfiguration config, ILoggerFactory loggerFactory)

        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
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
            if (!_options.DisableRequestLogging)
            {
                var list = new List<KeyValuePair<string, object?>>();

                var request = context.Request;
                list.Add(new KeyValuePair<string, object?>(nameof(request.Protocol), request.Protocol));
                list.Add(new KeyValuePair<string, object?>(nameof(request.Method), request.Method));
                list.Add(new KeyValuePair<string, object?>(nameof(request.ContentType), request.ContentType));
                list.Add(new KeyValuePair<string, object?>(nameof(request.ContentLength), request.ContentLength));
                list.Add(new KeyValuePair<string, object?>(nameof(request.Scheme), request.Scheme));
                list.Add(new KeyValuePair<string, object?>(nameof(request.Host), request.Host.Value));
                list.Add(new KeyValuePair<string, object?>(nameof(request.PathBase), request.PathBase.Value));
                list.Add(new KeyValuePair<string, object?>(nameof(request.Path), request.Path.Value));
                list.Add(new KeyValuePair<string, object?>(nameof(request.QueryString), request.QueryString.Value));

                // Would hope for this to be nested somehow? Scope?
                foreach (var header in FilterHeaders(request.Headers))
                {
                    list.Add(header);
                }

                // reading the request body always seems expensive.
                // TODO do we want string here? Other middleware writes to utf8jsonwriter directly.
                var body = await ReadRequestBody(request, context.RequestAborted);

                list.Add(new KeyValuePair<string, object?>(nameof(request.Body), body));

                // TODO add and remove things from log.

                var httpRequestLog = new HttpRequestLog(list);
                _logger.Log(LogLevel.Information,
                     eventId: LoggerEventIds.RequestLog,
                     state: httpRequestLog,
                     exception: null,
                     formatter: HttpRequestLog.Callback);
            }

            if (_options.DisableResponseLogging)
            {
                // Short circuit and don't replace response body.
                await _next(context).ConfigureAwait(false);
                return;
            }

            var response = context.Response;
            var originalBody = response.Body;

            // TODO pool memory streams.

            var originalBodyFeature = context.Features.Get<IHttpResponseBodyFeature>()!;
            var bufferingStream = new ResponseBufferingStream(originalBodyFeature, memoryStream, _options.ResponseBodyLogLimit);
            response.Body = bufferingStream;

            try
            {
                await _next(context).ConfigureAwait(false);
                var list = new List<KeyValuePair<string, object?>>();

                // TODO elapsed milliseconds?
                list.Add(new KeyValuePair<string, object?>(nameof(response.StatusCode), response.StatusCode));
                list.Add(new KeyValuePair<string, object?>(nameof(response.ContentType), response.ContentType));
                list.Add(new KeyValuePair<string, object?>(nameof(response.ContentLength), response.ContentLength));

                var httpRequestLog = new HttpResponseLog(list);
                foreach (var header in FilterHeaders(response.Headers))
                {
                    list.Add(header);
                }
            }
            finally
            {
                context.Features.Set(originalBodyFeature);
            }
        }

        private IEnumerable<KeyValuePair<string, object?>> FilterHeaders(IHeaderDictionary headers)
        {
            foreach (var (key, value) in headers)
            {
                //if (_options.Filtering == HttpHeaderFiltering.OnlyListed && !_options.FilteringSet.Contains(key))
                //{
                //    // Key is not among the "only listed" headers.
                //    continue;
                //}

                //if (_options.Filtering == HttpHeaderFiltering.AllButListed && _options.FilteringSet.Contains(key))
                //{
                //    // Key is among "all but listed" headers.
                //    continue;
                //}

                //if (_options.Redact && _options.Redactors.TryGetValue(key, out var redactor))
                //{
                //    yield return (key, redactor(value.ToString()));
                //    continue;
                //}

                yield return new KeyValuePair<string, object?>(key, value.ToString());
            }
        }

        private async Task<string> ReadRequestBody(HttpRequest request, CancellationToken token)
        {
            using var joinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource();
            joinedTokenSource.CancelAfter(_options.ReadTimeout);
            var limit = _options.RequestBodyLogLimit;
            if (limit <= PipeThreshold)
            {
                try
                {
                    while (true)
                    {
                        var result = await request.BodyReader.ReadAsync(joinedTokenSource.Token);
                        if (!result.IsCompleted && result.Buffer.Length <= limit)
                        {
                            // Need more data.
                            request.BodyReader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
                            continue;
                        }

                        var res = Encoding.UTF8.GetString(result.Buffer.Slice(0, result.Buffer.Length > limit ? limit : result.Buffer.Length));
                        request.BodyReader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
                        return res;
                    }
                }
                catch (OperationCanceledException)
                {
                    // Token source hides triggering token (https://github.com/dotnet/runtime/issues/22172)
                    if (!token.IsCancellationRequested && joinedTokenSource.Token.IsCancellationRequested)
                    {
                        // TODO should this be empty instead?
                        return "[Cancelled]";
                    }

                    throw;
                }
            }
            else
            {
                // TODO determine buffering limits here.
                request.EnableBuffering();

                // Read here.

                if (request.Body.CanSeek)
                {
                    _ = request.Body.Seek(0, SeekOrigin.Begin);
                }
            }

            return "";
        }
    }
}
