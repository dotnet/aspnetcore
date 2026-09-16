// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

/// <summary>
/// Binds the non-endpoint portion of the Kestrel configuration section onto <see cref="KestrelServerOptions"/>.
/// </summary>
/// <remarks>
/// Only keys present in configuration are applied, so options the app never mentions in configuration keep whatever
/// value they already have. Configuration does not win over code: this runs from the <see cref="KestrelConfigurationLoader"/>
/// constructor, i.e. while <c>IConfigureOptions&lt;KestrelServerOptions&gt;</c> delegates are still being invoked, so anything
/// the app configures in code afterwards overwrites what was bound here.
/// A key explicitly set to null resets the corresponding nullable option to null.
/// </remarks>
internal static class ServerOptionsConfigurationBinder
{
    private const string LimitsKey = "Limits";
    private const string Http2Key = "Http2";
    private const string Http3Key = "Http3";
    private const string BytesPerSecondKey = "BytesPerSecond";
    private const string GracePeriodKey = "GracePeriod";

    // "Kestrel": {
    //     "AddServerHeader": false,
    //     "Limits": {
    //         "MaxRequestBodySize": 10485760,
    //         "KeepAliveTimeout": "00:02:10",
    //         "MinRequestBodyDataRate": {
    //             "BytesPerSecond": 240,
    //             "GracePeriod": "00:00:05"
    //         },
    //         "Http2": { "MaxStreamsPerConnection": 100 },
    //         "Http3": { "MaxRequestHeaderFieldSize": 32768 }
    //     }
    // }
    public static void Bind(IConfiguration configuration, KestrelServerOptions options)
    {
        var keys = GetKeys(configuration);

        BindBoolean(configuration, keys, nameof(KestrelServerOptions.AddServerHeader), value => options.AddServerHeader = value);
        BindBoolean(configuration, keys, nameof(KestrelServerOptions.AllowAlternateSchemes), value => options.AllowAlternateSchemes = value);
        BindBoolean(configuration, keys, nameof(KestrelServerOptions.AllowHostHeaderOverride), value => options.AllowHostHeaderOverride = value);
        BindBoolean(configuration, keys, nameof(KestrelServerOptions.AllowResponseHeaderCompression), value => options.AllowResponseHeaderCompression = value);
        BindBoolean(configuration, keys, nameof(KestrelServerOptions.AllowSynchronousIO), value => options.AllowSynchronousIO = value);
        BindBoolean(configuration, keys, nameof(KestrelServerOptions.DisableStringReuse), value => options.DisableStringReuse = value);

        BindLimits(configuration.GetSection(LimitsKey), options.Limits);
    }

    private static void BindLimits(IConfigurationSection configuration, KestrelServerLimits limits)
    {
        var keys = GetKeys(configuration);

        BindInt64OrNull(configuration, keys, nameof(KestrelServerLimits.MaxResponseBufferSize), value => limits.MaxResponseBufferSize = value);
        BindInt64OrNull(configuration, keys, nameof(KestrelServerLimits.MaxRequestBufferSize), value => limits.MaxRequestBufferSize = value);
        BindInt32(configuration, keys, nameof(KestrelServerLimits.MaxRequestLineSize), value => limits.MaxRequestLineSize = value);
        BindInt32(configuration, keys, nameof(KestrelServerLimits.MaxRequestHeadersTotalSize), value => limits.MaxRequestHeadersTotalSize = value);
        BindInt32(configuration, keys, nameof(KestrelServerLimits.MaxRequestHeaderCount), value => limits.MaxRequestHeaderCount = value);
        BindInt64OrNull(configuration, keys, nameof(KestrelServerLimits.MaxRequestBodySize), value => limits.MaxRequestBodySize = value);
        BindTimeSpan(configuration, keys, nameof(KestrelServerLimits.KeepAliveTimeout), value => limits.KeepAliveTimeout = value);
        BindTimeSpan(configuration, keys, nameof(KestrelServerLimits.RequestHeadersTimeout), value => limits.RequestHeadersTimeout = value);
        BindInt64OrNull(configuration, keys, nameof(KestrelServerLimits.MaxConcurrentConnections), value => limits.MaxConcurrentConnections = value);
        BindInt64OrNull(configuration, keys, nameof(KestrelServerLimits.MaxConcurrentUpgradedConnections), value => limits.MaxConcurrentUpgradedConnections = value);
        BindMinDataRate(configuration, keys, nameof(KestrelServerLimits.MinRequestBodyDataRate), limits.MinRequestBodyDataRate, value => limits.MinRequestBodyDataRate = value);
        BindMinDataRate(configuration, keys, nameof(KestrelServerLimits.MinResponseDataRate), limits.MinResponseDataRate, value => limits.MinResponseDataRate = value);

        BindHttp2Limits(configuration.GetSection(Http2Key), limits.Http2);
        BindHttp3Limits(configuration.GetSection(Http3Key), limits.Http3);
    }

    private static void BindHttp2Limits(IConfigurationSection configuration, Http2Limits http2)
    {
        var keys = GetKeys(configuration);

        BindInt32(configuration, keys, nameof(Http2Limits.MaxStreamsPerConnection), value => http2.MaxStreamsPerConnection = value);
        BindInt32(configuration, keys, nameof(Http2Limits.HeaderTableSize), value => http2.HeaderTableSize = value);
        BindInt32(configuration, keys, nameof(Http2Limits.MaxFrameSize), value => http2.MaxFrameSize = value);
        BindInt32(configuration, keys, nameof(Http2Limits.MaxRequestHeaderFieldSize), value => http2.MaxRequestHeaderFieldSize = value);
        BindInt32(configuration, keys, nameof(Http2Limits.InitialConnectionWindowSize), value => http2.InitialConnectionWindowSize = value);
        BindInt32(configuration, keys, nameof(Http2Limits.InitialStreamWindowSize), value => http2.InitialStreamWindowSize = value);
        BindTimeSpan(configuration, keys, nameof(Http2Limits.KeepAlivePingDelay), value => http2.KeepAlivePingDelay = value);
        BindTimeSpan(configuration, keys, nameof(Http2Limits.KeepAlivePingTimeout), value => http2.KeepAlivePingTimeout = value);
    }

    private static void BindHttp3Limits(IConfigurationSection configuration, Http3Limits http3)
    {
        var keys = GetKeys(configuration);

        BindInt32(configuration, keys, nameof(Http3Limits.MaxRequestHeaderFieldSize), value => http3.MaxRequestHeaderFieldSize = value);
    }

    private static void BindMinDataRate(IConfigurationSection configuration, HashSet<string> keys, string key, MinDataRate? currentValue, Action<MinDataRate?> setter)
    {
        if (!keys.Contains(key))
        {
            return;
        }

        var section = configuration.GetSection(key);

        // A scalar can never describe a rate, so it's a configuration error rather than an empty section.
        if (!string.IsNullOrEmpty(section.Value))
        {
            throw new InvalidOperationException(CoreStrings.FormatInvalidConfigurationValue(section.Path, section.Value));
        }

        var rateKeys = GetKeys(section);

        if (rateKeys.Count == 0)
        {
            setter(null);
            return;
        }

        // Seeded with the current value so a section can specify just one half; MinDataRate is immutable and its
        // constructor needs both, so a half left unset with no current value to fall back on is an error.
        var bytesPerSecond = currentValue?.BytesPerSecond;
        var gracePeriod = currentValue?.GracePeriod;

        BindDouble(section, rateKeys, BytesPerSecondKey, value => bytesPerSecond = value);
        BindTimeSpan(section, rateKeys, GracePeriodKey, value => gracePeriod = value);

        if (bytesPerSecond is null)
        {
            throw new InvalidOperationException(CoreStrings.FormatMissingConfigurationValue(GetPath(section, BytesPerSecondKey)));
        }

        if (gracePeriod is null)
        {
            throw new InvalidOperationException(CoreStrings.FormatMissingConfigurationValue(GetPath(section, GracePeriodKey)));
        }

        setter(new MinDataRate(bytesPerSecond.Value, gracePeriod.Value));
    }

    private static void BindBoolean(IConfiguration configuration, HashSet<string> keys, string key, Action<bool> setter)
    {
        if (TryGetValue(configuration, keys, key, out var value) && value is not null)
        {
            if (!bool.TryParse(value, out var parsed))
            {
                throw new InvalidOperationException(CoreStrings.FormatInvalidConfigurationValue(GetPath(configuration, key), value));
            }

            setter(parsed);
        }
    }

    private static void BindInt32(IConfiguration configuration, HashSet<string> keys, string key, Action<int> setter)
    {
        if (TryGetValue(configuration, keys, key, out var value) && value is not null)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                throw new InvalidOperationException(CoreStrings.FormatInvalidConfigurationValue(GetPath(configuration, key), value));
            }

            setter(parsed);
        }
    }

    private static void BindInt64OrNull(IConfiguration configuration, HashSet<string> keys, string key, Action<long?> setter)
    {
        if (!TryGetValue(configuration, keys, key, out var value))
        {
            return;
        }

        if (value is null)
        {
            setter(null);
            return;
        }

        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new InvalidOperationException(CoreStrings.FormatInvalidConfigurationValue(GetPath(configuration, key), value));
        }

        setter(parsed);
    }

    private static void BindDouble(IConfiguration configuration, HashSet<string> keys, string key, Action<double> setter)
    {
        if (TryGetValue(configuration, keys, key, out var value) && value is not null)
        {
            if (!double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed))
            {
                throw new InvalidOperationException(CoreStrings.FormatInvalidConfigurationValue(GetPath(configuration, key), value));
            }

            setter(parsed);
        }
    }

    private static void BindTimeSpan(IConfiguration configuration, HashSet<string> keys, string key, Action<TimeSpan> setter)
    {
        if (TryGetValue(configuration, keys, key, out var value) && value is not null)
        {
            if (!TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var parsed))
            {
                throw new InvalidOperationException(CoreStrings.FormatInvalidConfigurationValue(GetPath(configuration, key), value));
            }

            setter(parsed);
        }
    }

    // An empty value is indistinguishable from an explicit null for most configuration providers, so both are reported as null.
    private static bool TryGetValue(IConfiguration configuration, HashSet<string> keys, string key, out string? value)
    {
        if (!keys.Contains(key))
        {
            value = null;
            return false;
        }

        value = configuration[key];
        if (string.IsNullOrEmpty(value))
        {
            value = null;
        }

        return true;
    }

    // Unlike IConfigurationSection.Exists(), this also reports keys that are explicitly set to null.
    private static HashSet<string> GetKeys(IConfiguration configuration)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var child in configuration.GetChildren())
        {
            keys.Add(child.Key);
        }

        return keys;
    }

    private static string GetPath(IConfiguration configuration, string key) =>
        configuration is IConfigurationSection section ? ConfigurationPath.Combine(section.Path, key) : key;
}
