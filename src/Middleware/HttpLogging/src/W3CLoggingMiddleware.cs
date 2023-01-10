// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using System.Numerics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.HttpLogging;

/// <summary>
/// Middleware that logs HTTP requests and HTTP responses.
/// </summary>
internal sealed class W3CLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly W3CLogger _w3cLogger;
    private readonly IOptionsMonitor<W3CLoggerOptions> _options;
    private string? _serverName;

    // Convenience for getting the index of each element in the elements array
    internal static readonly int _dateIndex = BitOperations.Log2((int)W3CLoggingFields.Date);
    internal static readonly int _timeIndex = BitOperations.Log2((int)W3CLoggingFields.Time);
    internal static readonly int _clientIpIndex = BitOperations.Log2((int)W3CLoggingFields.ClientIpAddress);
    internal static readonly int _userNameIndex = BitOperations.Log2((int)W3CLoggingFields.UserName);
    internal static readonly int _serverNameIndex = BitOperations.Log2((int)W3CLoggingFields.ServerName);
    internal static readonly int _serverIpIndex = BitOperations.Log2((int)W3CLoggingFields.ServerIpAddress);
    internal static readonly int _serverPortIndex = BitOperations.Log2((int)W3CLoggingFields.ServerPort);
    internal static readonly int _methodIndex = BitOperations.Log2((int)W3CLoggingFields.Method);
    internal static readonly int _uriStemIndex = BitOperations.Log2((int)W3CLoggingFields.UriStem);
    internal static readonly int _uriQueryIndex = BitOperations.Log2((int)W3CLoggingFields.UriQuery);
    internal static readonly int _protocolStatusIndex = BitOperations.Log2((int)W3CLoggingFields.ProtocolStatus);
    internal static readonly int _timeTakenIndex = BitOperations.Log2((int)W3CLoggingFields.TimeTaken);
    internal static readonly int _protocolVersionIndex = BitOperations.Log2((int)W3CLoggingFields.ProtocolVersion);
    internal static readonly int _hostIndex = BitOperations.Log2((int)W3CLoggingFields.Host);
    internal static readonly int _userAgentIndex = BitOperations.Log2((int)W3CLoggingFields.UserAgent);
    internal static readonly int _cookieIndex = BitOperations.Log2((int)W3CLoggingFields.Cookie);
    internal static readonly int _refererIndex = BitOperations.Log2((int)W3CLoggingFields.Referer);
    private readonly ISet<string> _additionalRequestHeaders;

    // Number of fields in W3CLoggingFields - equal to the number of _*Index variables above
    internal const int _fieldsLength = 17;

    /// <summary>
    /// Initializes <see cref="W3CLoggingMiddleware" />.
    /// </summary>
    /// <param name="next"></param>
    /// <param name="options"></param>
    /// <param name="w3cLogger"></param>
    public W3CLoggingMiddleware(RequestDelegate next, IOptionsMonitor<W3CLoggerOptions> options, W3CLogger w3cLogger)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(w3cLogger);

        _next = next;
        _options = options;
        _w3cLogger = w3cLogger;
        _additionalRequestHeaders = W3CLoggerOptions.FilterRequestHeaders(options.CurrentValue);
    }

    /// <summary>
    /// Invokes the <see cref="HttpLoggingMiddleware" />.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task Invoke(HttpContext context)
    {
        var options = _options.CurrentValue;

        var elements = new string[_fieldsLength];
        var additionalHeaderElements = new string[_additionalRequestHeaders.Count];

        // Whether any of the requested fields actually had content
        bool shouldLog = false;

        var now = DateTime.UtcNow;
        var stopWatch = ValueStopwatch.StartNew();
        if (options.LoggingFields.HasFlag(W3CLoggingFields.Date))
        {
            shouldLog |= AddToList(elements, _dateIndex, now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        if (options.LoggingFields.HasFlag(W3CLoggingFields.Time))
        {
            shouldLog |= AddToList(elements, _timeIndex, now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
        }

        if (options.LoggingFields.HasFlag(W3CLoggingFields.ServerName))
        {
            _serverName ??= Environment.MachineName;
            shouldLog |= AddToList(elements, _serverNameIndex, _serverName);
        }

        if ((W3CLoggingFields.ConnectionInfoFields & options.LoggingFields) != W3CLoggingFields.None)
        {
            var connectionInfo = context.Connection;

            if (options.LoggingFields.HasFlag(W3CLoggingFields.ClientIpAddress))
            {
                shouldLog |= AddToList(elements, _clientIpIndex, connectionInfo.RemoteIpAddress is null ? "" : connectionInfo.RemoteIpAddress.ToString());
            }

            if (options.LoggingFields.HasFlag(W3CLoggingFields.ServerIpAddress))
            {
                shouldLog |= AddToList(elements, _serverIpIndex, connectionInfo.LocalIpAddress is null ? "" : connectionInfo.LocalIpAddress.ToString());
            }

            if (options.LoggingFields.HasFlag(W3CLoggingFields.ServerPort))
            {
                shouldLog |= AddToList(elements, _serverPortIndex, connectionInfo.LocalPort.ToString(CultureInfo.InvariantCulture));
            }
        }

        var request = context.Request;

        if ((W3CLoggingFields.Request & options.LoggingFields) != W3CLoggingFields.None)
        {
            if (options.LoggingFields.HasFlag(W3CLoggingFields.ProtocolVersion))
            {
                shouldLog |= AddToList(elements, _protocolVersionIndex, request.Protocol);
            }

            if (options.LoggingFields.HasFlag(W3CLoggingFields.Method))
            {
                shouldLog |= AddToList(elements, _methodIndex, request.Method);
            }

            if (options.LoggingFields.HasFlag(W3CLoggingFields.UriStem))
            {
                shouldLog |= AddToList(elements, _uriStemIndex, (request.PathBase + request.Path).ToUriComponent());
            }

            if (options.LoggingFields.HasFlag(W3CLoggingFields.UriQuery))
            {
                shouldLog |= AddToList(elements, _uriQueryIndex, request.QueryString.Value);
            }

            if ((W3CLoggingFields.RequestHeaders & options.LoggingFields) != W3CLoggingFields.None)
            {
                if (options.LoggingFields.HasFlag(W3CLoggingFields.Host))
                {
                    if (request.Headers.TryGetValue(HeaderNames.Host, out var host))
                    {
                        shouldLog |= AddToList(elements, _hostIndex, host.ToString());
                    }
                }

                if (options.LoggingFields.HasFlag(W3CLoggingFields.Referer))
                {
                    if (request.Headers.TryGetValue(HeaderNames.Referer, out var referer))
                    {
                        shouldLog |= AddToList(elements, _refererIndex, referer.ToString());
                    }
                }

                if (options.LoggingFields.HasFlag(W3CLoggingFields.UserAgent))
                {
                    if (request.Headers.TryGetValue(HeaderNames.UserAgent, out var agent))
                    {
                        shouldLog |= AddToList(elements, _userAgentIndex, agent.ToString());
                    }
                }
            }
        }

        if (options.LoggingFields.HasFlag(W3CLoggingFields.Cookie))
        {
            if (request.Headers.TryGetValue(HeaderNames.Cookie, out var cookie))
            {
                shouldLog |= AddToList(elements, _cookieIndex, cookie.ToString());
            }
        }

        if (_additionalRequestHeaders.Count != 0)
        {
            var additionalRequestHeaders = _additionalRequestHeaders.ToList();

            for (var i = 0; i < additionalRequestHeaders.Count; i++)
            {
                if (request.Headers.TryGetValue(additionalRequestHeaders[i], out var headerValue))
                {
                    shouldLog |= AddToList(additionalHeaderElements, i, headerValue.ToString());
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
            if (shouldLog)
            {
                _w3cLogger.Log(elements, additionalHeaderElements);
            }
            throw;
        }

        if (options.LoggingFields.HasFlag(W3CLoggingFields.UserName))
        {
            shouldLog |= AddToList(elements, _userNameIndex, context.User?.Identity?.Name ?? "");
        }

        if (options.LoggingFields.HasFlag(W3CLoggingFields.ProtocolStatus))
        {
            shouldLog |= AddToList(elements, _protocolStatusIndex, StatusCodeHelper.ToStatusString(response.StatusCode));
        }

        if (options.LoggingFields.HasFlag(W3CLoggingFields.TimeTaken))
        {
            shouldLog |= AddToList(elements, _timeTakenIndex, stopWatch.GetElapsedTime().TotalMilliseconds.ToString(CultureInfo.InvariantCulture));
        }

        // Write the log
        if (shouldLog)
        {
            _w3cLogger.Log(elements, additionalHeaderElements);
        }
    }

    private static bool AddToList(string[] elements, int index, string? value)
    {
        value ??= string.Empty;
        elements[index] = ReplaceWhitespace(value.Trim());
        return !string.IsNullOrWhiteSpace(value);
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
