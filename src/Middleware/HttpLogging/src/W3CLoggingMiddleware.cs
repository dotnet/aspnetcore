// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.HttpLogging
{
    /// <summary>
    /// Middleware that logs HTTP requests and HTTP responses.
    /// </summary>
    internal sealed class W3CLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly W3CLogger _w3cLogger;
        private readonly IOptionsMonitor<W3CLoggerOptions> _options;

        /// <summary>
        /// Initializes <see cref="W3CLoggingMiddleware" />.
        /// </summary>
        /// <param name="next"></param>
        /// <param name="options"></param>
        /// <param name="environment"></param>
        public W3CLoggingMiddleware(RequestDelegate next, IOptionsMonitor<W3CLoggerOptions> options, IHostEnvironment environment)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            _options = options;
            if (String.IsNullOrEmpty(_options.CurrentValue.LogDirectory))
            {
                _options.CurrentValue.LogDirectory = Path.Join(environment.ContentRootPath, "logs");
            }
            _w3cLogger = new W3CLogger(_options);
        }

        /// <summary>
        /// Invokes the <see cref="HttpLoggingMiddleware" />.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>HttpResponseLog.cs
        public async Task Invoke(HttpContext context)
        {
            var options = _options.CurrentValue;

            var w3cList = new List<KeyValuePair<string, object?>>();

            if (options.LoggingFields.HasFlag(W3CLoggingFields.Date) || options.LoggingFields.HasFlag(W3CLoggingFields.Time))
            {
                AddToList(w3cList, nameof(DateTime), DateTime.Now.ToString(CultureInfo.InvariantCulture));
            }

            if (options.LoggingFields.HasFlag(W3CLoggingFields.UserName))
            {
                AddToList(w3cList, nameof(HttpContext.User), context.User is null ? "" : (context.User.Identity is null ? "" : (context.User.Identity.Name is null ? "" : context.User.Identity.Name)));
            }

            if ((W3CLoggingFields.ConnectionInfoFields & options.LoggingFields) != W3CLoggingFields.None)
            {
                var connectionInfo = context.Connection;

                if (options.LoggingFields.HasFlag(W3CLoggingFields.ClientIpAddress))
                {
                    AddToList(w3cList, nameof(ConnectionInfo.RemoteIpAddress), connectionInfo.RemoteIpAddress is null ? "" : connectionInfo.RemoteIpAddress.ToString());
                }

                if (options.LoggingFields.HasFlag(W3CLoggingFields.ServerIpAddress))
                {
                    AddToList(w3cList, nameof(ConnectionInfo.LocalIpAddress), connectionInfo.LocalIpAddress is null ? "" : connectionInfo.LocalIpAddress.ToString());
                }

                if (options.LoggingFields.HasFlag(W3CLoggingFields.ServerPort))
                {
                    AddToList(w3cList, nameof(ConnectionInfo.LocalPort), connectionInfo.LocalPort.ToString(CultureInfo.InvariantCulture));
                }
            }

            if ((W3CLoggingFields.Request & options.LoggingFields) != W3CLoggingFields.None)
            {
                var request = context.Request;

                if (options.LoggingFields.HasFlag(W3CLoggingFields.ProtocolVersion))
                {
                    AddToList(w3cList, nameof(request.Protocol), request.Protocol);
                }

                if (options.LoggingFields.HasFlag(W3CLoggingFields.Method))
                {
                    AddToList(w3cList, nameof(request.Method), request.Method);
                }

                if (options.LoggingFields.HasFlag(W3CLoggingFields.UriStem))
                {
                    AddToList(w3cList, nameof(request.Path), request.Path.Value);
                }

                if (options.LoggingFields.HasFlag(W3CLoggingFields.UriQuery))
                {
                    AddToList(w3cList, nameof(request.QueryString), request.QueryString.Value);
                }

                if ((W3CLoggingFields.RequestHeaders & options.LoggingFields) != W3CLoggingFields.None)
                {
                    var headers = request.Headers;

                    if (options.LoggingFields.HasFlag(W3CLoggingFields.Host))
                    {
                        if (headers.TryGetValue(HeaderNames.Host, out var host))
                        {
                            AddToList(w3cList, HeaderNames.Host, host.ToString());
                        }
                    }

                    if (options.LoggingFields.HasFlag(W3CLoggingFields.Referer))
                    {
                        if (headers.TryGetValue(HeaderNames.Referer, out var referer))
                        {
                            AddToList(w3cList, HeaderNames.Referer, referer.ToString());
                        }
                    }

                    if (options.LoggingFields.HasFlag(W3CLoggingFields.UserAgent))
                    {
                        if (headers.TryGetValue(HeaderNames.UserAgent, out var agent))
                        {
                            AddToList(w3cList, HeaderNames.UserAgent, agent.ToString());
                        }
                    }

                    if (options.LoggingFields.HasFlag(W3CLoggingFields.Cookie))
                    {
                        if (headers.TryGetValue(HeaderNames.Cookie, out var cookie))
                        {
                            AddToList(w3cList, HeaderNames.Cookie, cookie.ToString());
                        }
                    }
                }
            }

            var response = context.Response;

            await _next(context);

            if (options.LoggingFields.HasFlag(W3CLoggingFields.ProtocolStatus))
            {
                w3cList.Add(new KeyValuePair<string, object?>(nameof(response.StatusCode),
                    response.StatusCode.ToString(CultureInfo.InvariantCulture)));
            }

            if ((W3CLoggingFields.ResponseHeaders & options.LoggingFields) != W3CLoggingFields.None)
            {
                var headers = response.Headers;

                if (options.LoggingFields.HasFlag(W3CLoggingFields.ServerName))
                {
                    if (headers.TryGetValue(HeaderNames.Server, out var server))
                    {
                        AddToList(w3cList, HeaderNames.Server, server.ToString());
                    }
                }
            }

            // Write the log
            if (w3cList.Count > 0)
            {
                _w3cLogger.Log(w3cList);
            }
        }

        private static void AddToList(List<KeyValuePair<string, object?>> list, string key, string? value)
        {
            list.Add(new KeyValuePair<string, object?>(key, value));
        }
    }
}
