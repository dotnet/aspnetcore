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
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.HttpLogging
{
    internal class W3CLogger : IAsyncDisposable
    {
        private readonly W3CLoggerProcessor _messageQueue;
        private readonly IOptionsMonitor<W3CLoggerOptions> _options;
        // Subtract 8 to account for flags that represent groups (e.g. "RequestHeaders", "None")
        private readonly int _fieldsLength = Enum.GetValues(typeof(W3CLoggingFields)).Length - 6;

        public W3CLogger(IOptionsMonitor<W3CLoggerOptions> options)
        {
            _options = options;
            _messageQueue = InitializeMessageQueue(_options);
        }

        // Virtual for testing
        internal virtual W3CLoggerProcessor InitializeMessageQueue(IOptionsMonitor<W3CLoggerOptions> options)
        {
            return new W3CLoggerProcessor(options);
        }

        internal void OnOptionsChange()
        {
            _messageQueue.OnOptionsChange();
        }

        public async ValueTask DisposeAsync() => await _messageQueue.DisposeAsync();

        public void Log(IReadOnlyCollection<KeyValuePair<string, string?>> state)
        {
            _messageQueue.EnqueueMessage(Format(state));
        }

        private string Format(IEnumerable<KeyValuePair<string, string?>> state)
        {
            var elements = new string[_fieldsLength];
            foreach(var kvp in state)
            {
                var val = kvp.Value ?? string.Empty;
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
                        elements[BitOperations.Log2((int)W3CLoggingFields.UserAgent)] = ReplaceWhitespace(val.Trim());
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
                        elements[BitOperations.Log2((int)W3CLoggingFields.Cookie)] = ReplaceWhitespace(val.Trim());
                        break;
                    default:
                        break;
                }
            }
            // 200 is around the length of an average cookie-less entry
            var sb = new ValueStringBuilder(200);
            var firstElement = true;
            for (var i = 0; i < elements.Length; i++)
            {
                if (_options.CurrentValue.LoggingFields.HasFlag((W3CLoggingFields)(1 << i)))
                {
                    if (!firstElement)
                    {
                        sb.Append(' ');
                    }
                    else
                    {
                        firstElement = false;
                    }
                    // If the element was not logged, or was the empty string, we log it as a dash
                    if (string.IsNullOrEmpty(elements[i]))
                    {
                        sb.Append('-');
                    }
                    else
                    {
                        sb.Append(elements[i]);
                    }
                }
            }
            return sb.ToString();
        }

        // Modified from https://www.codeproject.com/Articles/1014073/Fastest-method-to-remove-all-whitespace-from-Strin
        private static string ReplaceWhitespace(string entry)
        {
            var len = entry.Length;
            var src = entry.ToCharArray();
            for (var i = 0; i < len; i++)
            {
                var ch = src[i];
                switch (ch)
                {
                    case '\u0020':
                    case '\u00A0':
                    case '\u1680':
                    case '\u2000':
                    case '\u2001':
                    case '\u2002':
                    case '\u2003':
                    case '\u2004':
                    case '\u2005':
                    case '\u2006':
                    case '\u2007':
                    case '\u2008':
                    case '\u2009':
                    case '\u200A':
                    case '\u202F':
                    case '\u205F':
                    case '\u3000':
                    case '\u2028':
                    case '\u2029':
                    case '\u0009':
                    case '\u000A':
                    case '\u000B':
                    case '\u000C':
                    case '\u000D':
                    case '\u0085':
                        src[i] = '+';
                        break;
                    default:
                        // Character doesn't need to change.
                        break;
                }
            }
            return new string(src, 0, len);
        }
    }
}
