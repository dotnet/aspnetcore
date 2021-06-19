// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Numerics;
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
    internal class W3CLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly W3CLogger _w3cLogger;
        private readonly IOptionsMonitor<W3CLoggerOptions> _options;
        private string? _serverName;
        private bool _hasLogged;

        internal static Dictionary<W3CLoggingFields, int> _fieldIndices = new Dictionary<W3CLoggingFields, int>()
        {
            { W3CLoggingFields.Date, BitOperations.Log2((int)W3CLoggingFields.Date) },
            { W3CLoggingFields.Time, BitOperations.Log2((int)W3CLoggingFields.Time) },
            { W3CLoggingFields.ClientIpAddress, BitOperations.Log2((int)W3CLoggingFields.ClientIpAddress) },
            { W3CLoggingFields.UserName, BitOperations.Log2((int)W3CLoggingFields.UserName) },
            { W3CLoggingFields.ServerName, BitOperations.Log2((int)W3CLoggingFields.ServerName) },
            { W3CLoggingFields.ServerIpAddress, BitOperations.Log2((int)W3CLoggingFields.ServerIpAddress) },
            { W3CLoggingFields.ServerPort, BitOperations.Log2((int)W3CLoggingFields.ServerPort) },
            { W3CLoggingFields.Method, BitOperations.Log2((int)W3CLoggingFields.Method) },
            { W3CLoggingFields.UriStem, BitOperations.Log2((int)W3CLoggingFields.UriStem) },
            { W3CLoggingFields.UriQuery, BitOperations.Log2((int)W3CLoggingFields.UriQuery) },
            { W3CLoggingFields.ProtocolStatus, BitOperations.Log2((int)W3CLoggingFields.ProtocolStatus) },
            { W3CLoggingFields.TimeTaken, BitOperations.Log2((int)W3CLoggingFields.TimeTaken) },
            { W3CLoggingFields.ProtocolVersion, BitOperations.Log2((int)W3CLoggingFields.ProtocolVersion) },
            { W3CLoggingFields.Host, BitOperations.Log2((int)W3CLoggingFields.Host) },
            { W3CLoggingFields.UserAgent, BitOperations.Log2((int)W3CLoggingFields.UserAgent) },
            { W3CLoggingFields.Cookie, BitOperations.Log2((int)W3CLoggingFields.Cookie) },
            { W3CLoggingFields.Referer, BitOperations.Log2((int)W3CLoggingFields.Referer) }
        };

        internal static readonly int _fieldsLength = _fieldIndices.Count;

        /// <summary>
        /// Initializes <see cref="W3CLoggingMiddleware" />.
        /// </summary>
        /// <param name="next"></param>
        /// <param name="options"></param>
        /// <param name="w3cLogger"></param>
        public W3CLoggingMiddleware(RequestDelegate next, IOptionsMonitor<W3CLoggerOptions> options, W3CLogger w3cLogger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (w3cLogger == null)
            {
                throw new ArgumentNullException(nameof(w3cLogger));
            }

            _options = options;
            _w3cLogger = w3cLogger;
        }

        /// <summary>
        /// Invokes the <see cref="HttpLoggingMiddleware" />.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>HttpResponseLog.cs
        public async Task Invoke(HttpContext context)
        {
            var options = _options.CurrentValue;

            var elements = new string[_fieldsLength];

            var now = DateTime.Now;
            if (options.LoggingFields.HasFlag(W3CLoggingFields.Date))
            {
                AddToList(elements, W3CLoggingFields.Date, now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }

            if (options.LoggingFields.HasFlag(W3CLoggingFields.Time))
            {
                AddToList(elements, W3CLoggingFields.Time, now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
            }

            if ((W3CLoggingFields.ConnectionInfoFields & options.LoggingFields) != W3CLoggingFields.None)
            {
                var connectionInfo = context.Connection;

                if (options.LoggingFields.HasFlag(W3CLoggingFields.ClientIpAddress))
                {
                    AddToList(elements, W3CLoggingFields.ClientIpAddress, connectionInfo.RemoteIpAddress is null ? "" : connectionInfo.RemoteIpAddress.ToString());
                }

                if (options.LoggingFields.HasFlag(W3CLoggingFields.ServerIpAddress))
                {
                    AddToList(elements, W3CLoggingFields.ServerIpAddress, connectionInfo.LocalIpAddress is null ? "" : connectionInfo.LocalIpAddress.ToString());
                }

                if (options.LoggingFields.HasFlag(W3CLoggingFields.ServerPort))
                {
                    AddToList(elements, W3CLoggingFields.ServerPort, connectionInfo.LocalPort.ToString(CultureInfo.InvariantCulture));
                }
            }

            if ((W3CLoggingFields.Request & options.LoggingFields) != W3CLoggingFields.None)
            {
                var request = context.Request;

                if (options.LoggingFields.HasFlag(W3CLoggingFields.ProtocolVersion))
                {
                    AddToList(elements, W3CLoggingFields.ProtocolVersion, request.Protocol);
                }

                if (options.LoggingFields.HasFlag(W3CLoggingFields.Method))
                {
                    AddToList(elements, W3CLoggingFields.Method, request.Method);
                }

                if (options.LoggingFields.HasFlag(W3CLoggingFields.UriStem))
                {
                    AddToList(elements, W3CLoggingFields.UriStem, request.Path.Value);
                }

                if (options.LoggingFields.HasFlag(W3CLoggingFields.UriQuery))
                {
                    AddToList(elements, W3CLoggingFields.UriQuery, request.QueryString.Value);
                }

                if ((W3CLoggingFields.RequestHeaders & options.LoggingFields) != W3CLoggingFields.None)
                {
                    var headers = request.Headers;

                    if (options.LoggingFields.HasFlag(W3CLoggingFields.Host))
                    {
                        if (headers.TryGetValue(HeaderNames.Host, out var host))
                        {
                            AddToList(elements, W3CLoggingFields.Host, host.ToString());
                        }
                    }

                    if (options.LoggingFields.HasFlag(W3CLoggingFields.Referer))
                    {
                        if (headers.TryGetValue(HeaderNames.Referer, out var referer))
                        {
                            AddToList(elements, W3CLoggingFields.Referer, referer.ToString());
                        }
                    }

                    if (options.LoggingFields.HasFlag(W3CLoggingFields.UserAgent))
                    {
                        if (headers.TryGetValue(HeaderNames.UserAgent, out var agent))
                        {
                            AddToList(elements, W3CLoggingFields.UserAgent, agent.ToString());
                        }
                    }

                    if (options.LoggingFields.HasFlag(W3CLoggingFields.Cookie))
                    {
                        if (headers.TryGetValue(HeaderNames.Cookie, out var cookie))
                        {
                            AddToList(elements, W3CLoggingFields.Cookie, cookie.ToString());
                        }
                    }
                }
            }

            var response = context.Response;

            try
            {
                await _next(context);
            }
            catch
            {
                // Write the log
                if (_hasLogged)
                {
                    _w3cLogger.Log(elements);
                }
                throw;
            }

            if (options.LoggingFields.HasFlag(W3CLoggingFields.UserName))
            {
                AddToList(elements, W3CLoggingFields.UserName, context?.User?.Identity?.Name ?? "");
            }

            if (options.LoggingFields.HasFlag(W3CLoggingFields.ProtocolStatus))
            {
                AddToList(elements, W3CLoggingFields.ProtocolStatus, response.StatusCode.ToString(CultureInfo.InvariantCulture));
            }

            if ((W3CLoggingFields.ResponseHeaders & options.LoggingFields) != W3CLoggingFields.None)
            {
                var headers = response.Headers;

                if (options.LoggingFields.HasFlag(W3CLoggingFields.ServerName))
                {
                    if (headers.TryGetValue(HeaderNames.Server, out var server))
                    {
                        _serverName ??= Environment.MachineName;
                        AddToList(elements, W3CLoggingFields.ServerName, _serverName);
                    }
                }
            }

            // Write the log
            if (_hasLogged)
            {
                _w3cLogger.Log(elements);
            }
        }

        private void AddToList(string[] elements, W3CLoggingFields key, string? value)
        {
            _hasLogged = true;
            value ??= string.Empty;
            elements[_fieldIndices[key]] = ReplaceWhitespace(value.Trim());
        }

        // We replace whitespace with the '+' character
        private static string ReplaceWhitespace(string entry)
        {
            var len = entry.Length;
            if (len == 0)
            {
                return entry;
            }
            var src = Array.Empty<char>();
            for (var i = 0; i < len; i++)
            {
                var ch = entry[i];
                if (ch <= '\u0020')
                {
                    if (src.Length == 0)
                    {
                        src = entry.ToCharArray();
                    }
                    src[i] = '+';
                }
            }
            // Return original string if we didn't need to modify it
            if (src.Length == 0)
            {
                return entry;
            }
            return new string(src, 0, len);
        }
    }
}
