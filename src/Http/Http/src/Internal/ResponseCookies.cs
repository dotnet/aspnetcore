// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// A wrapper for the response Set-Cookie header.
/// </summary>
internal sealed partial class ResponseCookies : IResponseCookies
{
    private readonly IFeatureCollection _features;

    private ILogger? _logger;
    private bool _retrievedLogger;

    /// <summary>
    /// Create a new wrapper.
    /// </summary>
    internal ResponseCookies(IFeatureCollection features)
    {
        _features = features;
        Headers = _features.GetRequiredFeature<IHttpResponseFeature>().Headers;
    }

    private IHeaderDictionary Headers { get; set; }

    /// <inheritdoc />
    public void Append(string key, string value)
    {
        var setCookieHeaderValue = new SetCookieHeaderValue(key, Uri.EscapeDataString(value))
        {
            Path = "/"
        };
        var cookieValue = setCookieHeaderValue.ToString();

        Headers.SetCookie = StringValues.Concat(Headers.SetCookie, cookieValue);
    }

    /// <inheritdoc />
    public void Append(string key, string value, CookieOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var messagesToLog = GetMessagesToLog(options);
        if (messagesToLog != MessagesToLog.None && TryGetLogger(out var logger))
        {
            LogMessages(logger, messagesToLog, key);
        }

        var cookie = options.CreateCookieHeader(key, Uri.EscapeDataString(value)).ToString();
        Headers.SetCookie = StringValues.Concat(Headers.SetCookie, cookie);
    }

    /// <inheritdoc />
    public void Append(ReadOnlySpan<KeyValuePair<string, string>> keyValuePairs, CookieOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var messagesToLog = GetMessagesToLog(options);
        if (messagesToLog != MessagesToLog.None && TryGetLogger(out var logger))
        {
            foreach (var keyValuePair in keyValuePairs)
            {
                LogMessages(logger, messagesToLog, keyValuePair.Key);
            }
        }

        var cookieSuffix = options.CreateCookieHeader(string.Empty, string.Empty).ToString().AsSpan(1);
        var cookies = new string[keyValuePairs.Length];
        var position = 0;

        foreach (var keyValuePair in keyValuePairs)
        {
            cookies[position] = string.Concat(keyValuePair.Key, "=", Uri.EscapeDataString(keyValuePair.Value), cookieSuffix);
            position++;
        }

        // Can't use += as StringValues does not override operator+
        // and the implicit conversions will cause an incorrect string concat https://github.com/dotnet/runtime/issues/52507
        Headers.SetCookie = StringValues.Concat(Headers.SetCookie, cookies);
    }

    /// <inheritdoc />
    public void Delete(string key)
    {
        Delete(key, new CookieOptions() { Path = "/" });
    }

    /// <inheritdoc />
    public void Delete(string key, CookieOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var encodedKeyPlusEquals = key + "=";
        var domainHasValue = !string.IsNullOrEmpty(options.Domain);
        var pathHasValue = !string.IsNullOrEmpty(options.Path);

        Func<string, string, CookieOptions, bool> rejectPredicate;
        if (domainHasValue && pathHasValue)
        {
            rejectPredicate = (value, encKeyPlusEquals, opts) =>
                value.StartsWith(encKeyPlusEquals, StringComparison.OrdinalIgnoreCase) &&
                    value.Contains($"domain={opts.Domain}", StringComparison.OrdinalIgnoreCase) &&
                    value.Contains($"path={opts.Path}", StringComparison.OrdinalIgnoreCase);
        }
        else if (domainHasValue)
        {
            rejectPredicate = (value, encKeyPlusEquals, opts) =>
                value.StartsWith(encKeyPlusEquals, StringComparison.OrdinalIgnoreCase) &&
                    value.Contains($"domain={opts.Domain}", StringComparison.OrdinalIgnoreCase);
        }
        else if (pathHasValue)
        {
            rejectPredicate = (value, encKeyPlusEquals, opts) =>
                value.StartsWith(encKeyPlusEquals, StringComparison.OrdinalIgnoreCase) &&
                    value.Contains($"path={opts.Path}", StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            rejectPredicate = (value, encKeyPlusEquals, opts) => value.StartsWith(encKeyPlusEquals, StringComparison.OrdinalIgnoreCase);
        }

        var existingValues = Headers.SetCookie;
        if (!StringValues.IsNullOrEmpty(existingValues))
        {
            var values = existingValues.ToArray();
            var newValues = new List<string>();

            for (var i = 0; i < values.Length; i++)
            {
                var value = values[i] ?? string.Empty;
                if (!rejectPredicate(value, encodedKeyPlusEquals, options))
                {
                    newValues.Add(value);
                }
            }

            Headers.SetCookie = new StringValues(newValues.ToArray());
        }

        Append(key, string.Empty, new CookieOptions(options)
        {
            Expires = DateTimeOffset.UnixEpoch,
            MaxAge = null, // Some browsers require this (https://github.com/dotnet/aspnetcore/issues/52159)
        });
    }

    private bool TryGetLogger([NotNullWhen(true)] out ILogger? logger)
    {
        if (!_retrievedLogger)
        {
            _retrievedLogger = true;
            var services = _features.Get<IServiceProvidersFeature>()?.RequestServices;
            _logger = services?.GetService<ILogger<ResponseCookies>>();
        }

        logger = _logger;
        return logger is not null;
    }

    private static MessagesToLog GetMessagesToLog(CookieOptions options)
    {
        var toLog = MessagesToLog.None;

        if (!options.Secure && options.SameSite == SameSiteMode.None)
        {
            toLog |= MessagesToLog.SameSiteNotSecure;
        }

        if (options.Partitioned)
        {
            if (!options.Secure)
            {
                toLog |= MessagesToLog.PartitionedNotSecure;
            }

            if (options.SameSite != SameSiteMode.None)
            {
                toLog |= MessagesToLog.PartitionedNotSameSiteNone;
            }

            // Chromium checks this
            if (options.Path != "/")
            {
                toLog |= MessagesToLog.PartitionedNotPathRoot;
            }
        }

        return toLog;
    }

    private static void LogMessages(ILogger logger, MessagesToLog messages, string cookieName)
    {
        if ((messages & MessagesToLog.SameSiteNotSecure) != 0)
        {
            Log.SameSiteCookieNotSecure(logger, cookieName);
        }

        if ((messages & MessagesToLog.PartitionedNotSecure) != 0)
        {
            Log.PartitionedCookieNotSecure(logger, cookieName);
        }

        if ((messages & MessagesToLog.PartitionedNotSameSiteNone) != 0)
        {
            Log.PartitionedCookieNotSameSiteNone(logger, cookieName);
        }

        if ((messages & MessagesToLog.PartitionedNotPathRoot) != 0)
        {
            Log.PartitionedCookieNotPathRoot(logger, cookieName);
        }
    }

    [Flags]
    private enum MessagesToLog
    {
        None,
        SameSiteNotSecure = 1 << 0,
        PartitionedNotSecure = 1 << 1,
        PartitionedNotSameSiteNone = 1 << 2,
        PartitionedNotPathRoot = 1 << 3,
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Warning, "The cookie '{name}' has set 'SameSite=None' and must also set 'Secure'. This cookie will likely be rejected by the client.", EventName = "SameSiteNotSecure")]
        public static partial void SameSiteCookieNotSecure(ILogger logger, string name);

        [LoggerMessage(2, LogLevel.Warning, "The cookie '{name}' has set 'Partitioned' and must also set 'Secure'. This cookie will likely be rejected by the client.", EventName = "PartitionedNotSecure")]
        public static partial void PartitionedCookieNotSecure(ILogger logger, string name);

        [LoggerMessage(3, LogLevel.Debug, "The cookie '{name}' has set 'Partitioned' and should also set 'SameSite=None'. This cookie will likely be rejected by the client.", EventName = "PartitionedNotSameSiteNone")]
        public static partial void PartitionedCookieNotSameSiteNone(ILogger logger, string name);

        [LoggerMessage(4, LogLevel.Debug, "The cookie '{name}' has set 'Partitioned' and should also set 'Path=/'. This cookie may be rejected by the client.", EventName = "PartitionedNotPathRoot")]
        public static partial void PartitionedCookieNotPathRoot(ILogger logger, string name);
    }
}
