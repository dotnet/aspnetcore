// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Numerics;
using System.Text;
using Microsoft.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Diagnostics;

namespace Microsoft.AspNetCore.HttpLogging
{
    internal sealed class W3CLogger : IDisposable
    {
        private readonly W3CLoggerProcessor _messageQueue;
        private readonly IOptionsMonitor<W3CLoggerOptions> _options;
        private readonly W3CLoggingFields _loggingFields;

        internal W3CLogger(IOptionsMonitor<W3CLoggerOptions> options)
        {
            _options = options;

            _messageQueue = new W3CLoggerProcessor(_options);
            _loggingFields = _options.CurrentValue.LoggingFields;
            LogFileFullName = _messageQueue.FullName;
        }

        public string? LogFileFullName { get; }

        public void Dispose() => _messageQueue?.Dispose();

        public void Log(IReadOnlyCollection<KeyValuePair<string, object?>> state)
        {
            _messageQueue.EnqueueMessage(Format(state));
        }

        private string Format(IEnumerable<KeyValuePair<string, object?>> state)
        {
            // Subtract 1 to account for the "All" flag
            string[] elements = new string[Enum.GetValues(typeof(W3CLoggingFields)).Length - 1];
            foreach(KeyValuePair<string, object?> kvp in state)
            {
                var val = kvp.Value is null ? "" : kvp.Value.ToString();
                if (val is null)
                {
                    val = "";
                }
                switch (kvp.Key)
                {
                    case nameof(HttpRequest.Method):
                        elements[BitOperations.Log2((int)W3CLoggingFields.Method)] = val.Trim();
                        break;
                    case nameof(HttpRequest.Path):
                        elements[BitOperations.Log2((int)W3CLoggingFields.UriStem)] = val.Trim();
                        break;
                    case nameof(HttpRequest.QueryString):
                        elements[BitOperations.Log2((int)W3CLoggingFields.UriQuery)] = val.Trim();
                        break;
                    case nameof(HttpResponse.StatusCode):
                        elements[BitOperations.Log2((int)W3CLoggingFields.ProtocolStatus)] = val.Trim();
                        break;
                    case nameof(HttpRequest.Protocol):
                        elements[BitOperations.Log2((int)W3CLoggingFields.ProtocolVersion)] = val.Trim();
                        break;
                    case nameof(HeaderNames.Host):
                        elements[BitOperations.Log2((int)W3CLoggingFields.Host)] = val.Trim();
                        break;
                    case "User-Agent":
                        // User-Agent can have whitespace - we replace whitespace characters with the '+' character
                        elements[BitOperations.Log2((int)W3CLoggingFields.UserAgent)] = Regex.Replace(val.Trim(), @"\s", "+");
                        break;
                    case nameof(HeaderNames.Referer):
                        elements[BitOperations.Log2((int)W3CLoggingFields.Referer)] = val.Trim();
                        break;
                    case nameof(DateTime):
                        DateTime dt = DateTime.Parse(val, CultureInfo.InvariantCulture);
                        elements[BitOperations.Log2((int)W3CLoggingFields.Date)] = dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        elements[BitOperations.Log2((int)W3CLoggingFields.Time)] = dt.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
                        // We estimate time elapsed by diffing the current time with the time at which the middleware processed the request/response.
                        // This will represent the time in whole & fractional milliseconds.
                        var elapsed = DateTime.Now.Subtract(dt);
                        elements[BitOperations.Log2((int)W3CLoggingFields.TimeTaken)] = elapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
                        break;
                    case nameof(HeaderNames.Server):
                        elements[BitOperations.Log2((int)W3CLoggingFields.ServerName)] = val.Trim();
                        break;
                    case nameof(ConnectionInfo.RemoteIpAddress):
                        elements[BitOperations.Log2((int)W3CLoggingFields.ClientIpAddress)] = val.Trim();
                        break;
                    case nameof(ConnectionInfo.LocalIpAddress):
                        elements[BitOperations.Log2((int)W3CLoggingFields.ServerIpAddress)] = val.Trim();
                        break;
                    case nameof(ConnectionInfo.LocalPort):
                        elements[BitOperations.Log2((int)W3CLoggingFields.ServerPort)] = val.Trim();
                        break;
                    case nameof(HttpContext.User):
                        elements[BitOperations.Log2((int)W3CLoggingFields.UserName)] = val.Trim();
                        break;
                    case nameof(HeaderNames.Cookie):
                        // Cookie can have whitespace - we replace whitespace characters with the '+' character
                        elements[BitOperations.Log2((int)W3CLoggingFields.Cookie)] = Regex.Replace(val.Trim(), @"\s", "+");
                        break;
                    default:
                        break;
                }
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < elements.Length; i++)
            {
                if (_loggingFields.HasFlag((W3CLoggingFields)(1 << i)))
                {
                    // If the element was not logged, or was the empty string, we log it as a dash
                    if (String.IsNullOrEmpty(elements[i]))
                    {
                        sb.Append("- ");
                    }
                    else
                    {
                        sb.Append(elements[i] + " ");
                    }
                }
            }
            return sb.ToString().Trim();
        }
    }
}
