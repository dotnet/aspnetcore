// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// Follows OpenTelemetry Semantic Conventions for HTTP
/// <a href="https://github.com/open-telemetry/semantic-conventions/blob/v1.44.0/docs/http/http-spans.md">v1.44.0</a>
/// </summary>
internal static class HostingTelemetryHelpers
{
    public const string AttributeHttpRequestMethod = "http.request.method";
    public const string AttributeHttpRequestMethodOriginal = "http.request.method_original";
    public const string AttributeHttpResponseStatusCode = "http.response.status_code";
    public const string AttributeHttpRoute = "http.route";
    public const string AttributeUrlScheme = "url.scheme";
    public const string AttributeUrlPath = "url.path";
    public const string AttributeUrlQuery = "url.query";
    public const string AttributeClientAddress = "client.address";
    public const string AttributeServerAddress = "server.address";
    public const string AttributeServerPort = "server.port";
    public const string AttributeNetworkPeerAddress = "network.peer.address";
    public const string AttributeNetworkPeerPort = "network.peer.port";
    public const string AttributeUserAgentOriginal = "user_agent.original";
    public const string AttributeNetworkProtocolVersion = "network.protocol.version";
    public const string AttributeErrorType = "error.type";

    // The value "_OTHER" is used for non-standard HTTP methods.
    private const string OtherHttpMethod = "_OTHER";
    private const string KnownHttpMethodsEnvironmentVariable = "OTEL_INSTRUMENTATION_HTTP_KNOWN_METHODS";
    private const string RedactedQueryParameterValue = "REDACTED";

    private static readonly object[] BoxedStatusCodes = new object[512];

    private static readonly FrozenDictionary<string, string> KnownHttpMethods =
        CreateKnownHttpMethods(Environment.GetEnvironmentVariable(KnownHttpMethodsEnvironmentVariable));

    // Boxed values for the ports most likely to be seen in the Host header.
    private static readonly object HttpPort = 80;
    private static readonly object HttpsPort = 443;
    private static readonly object Port8080 = 8080;
    private static readonly object Port5000 = 5000;
    private static readonly object Port5001 = 5001;

    // Single-value cache for any other (e.g. dynamically-assigned) port. The server's listening
    // port is effectively constant for the lifetime of the process, so this avoids boxing on the
    // common path. The Host header is client-controlled, so the cache is intentionally limited to
    // a single entry; a miss simply boxes the value again.
    private static object? _lastBoxedPort;

    public static bool TryGetServerPort(HostString host, string scheme, [NotNullWhen(true)] out object? port)
    {
        if (host.Port is { } portValue)
        {
            port = GetBoxedPort(portValue);
            return true;
        }

        // If the port is not specified, use the default port for the scheme.
        if (string.Equals(scheme, "http", StringComparison.OrdinalIgnoreCase))
        {
            port = HttpPort;
            return true;
        }
        else if (string.Equals(scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            port = HttpsPort;
            return true;
        }

        // Unknown scheme, no default port.
        port = null;
        return false;
    }

    private static object GetBoxedPort(int port)
    {
        // Reuse pre-boxed instances for the most common ports to avoid allocating.
        var common = port switch
        {
            80 => HttpPort,
            443 => HttpsPort,
            8080 => Port8080,
            5000 => Port5000,
            5001 => Port5001,
            _ => null,
        };

        if (common is not null)
        {
            return common;
        }

        // Otherwise reuse the most-recently boxed port. Reference reads/writes are atomic and a
        // race only results in an occasional extra allocation, so no locking is required.
        var last = _lastBoxedPort;
        if (last is not null && (int)last == port)
        {
            return last;
        }

        object boxed = port;
        _lastBoxedPort = boxed;

        return boxed;
    }

    public static object GetBoxedStatusCode(int statusCode)
    {
        var boxes = BoxedStatusCodes;
        return (uint)statusCode < (uint)boxes.Length
            ? boxes[statusCode] ??= statusCode
            : statusCode;
    }

    public static string GetNormalizedHttpMethod(string method)
    {
        if (method is not null && KnownHttpMethods.TryGetValue(method, out var result))
        {
            // KnownHttpMethods ignores case. Use the value returned by the dictionary to have a consistent case.
            return result;
        }
        return OtherHttpMethod;
    }

    // Internal for testing.
    internal static FrozenDictionary<string, string> CreateKnownHttpMethods(string? configuredKnownMethods)
    {
        if (!string.IsNullOrEmpty(configuredKnownMethods))
        {
            var knownHttpMethods = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var method in configuredKnownMethods.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                knownHttpMethods[method] = method;
            }

            if (knownHttpMethods.Count > 0)
            {
                return knownHttpMethods.ToFrozenDictionary(StringComparer.Ordinal);
            }
        }

        return FrozenDictionary.ToFrozenDictionary([
            KeyValuePair.Create(HttpMethods.Connect, HttpMethods.Connect),
            KeyValuePair.Create(HttpMethods.Delete, HttpMethods.Delete),
            KeyValuePair.Create(HttpMethods.Get, HttpMethods.Get),
            KeyValuePair.Create(HttpMethods.Head, HttpMethods.Head),
            KeyValuePair.Create(HttpMethods.Options, HttpMethods.Options),
            KeyValuePair.Create(HttpMethods.Patch, HttpMethods.Patch),
            KeyValuePair.Create(HttpMethods.Post, HttpMethods.Post),
            KeyValuePair.Create(HttpMethods.Put, HttpMethods.Put),
            KeyValuePair.Create(HttpMethods.Query, HttpMethods.Query),
            KeyValuePair.Create(HttpMethods.Trace, HttpMethods.Trace)
        ], StringComparer.OrdinalIgnoreCase);
    }

    public static string GetRedactedQueryString(string queryString)
    {
        Debug.Assert(queryString.Length > 0 && queryString[0] == '?');

        var query = queryString.AsSpan(1);
        StringBuilder? builder = null;
        var copyFrom = 0;
        var segmentStart = 0;

        while (segmentStart < query.Length)
        {
            var segmentLength = query[segmentStart..].IndexOf('&');
            if (segmentLength < 0)
            {
                segmentLength = query.Length - segmentStart;
            }

            var segment = query.Slice(segmentStart, segmentLength);
            var equalsIndex = segment.IndexOf('=');

            if (equalsIndex >= 0 && IsSensitiveQueryParameter(segment[..equalsIndex]))
            {
                builder ??= new StringBuilder(query.Length);
                var valueStart = segmentStart + equalsIndex + 1;
                builder.Append(query[copyFrom..valueStart]);
                builder.Append(RedactedQueryParameterValue);
                copyFrom = segmentStart + segmentLength;
            }

            segmentStart += segmentLength + 1;
        }

        if (builder is null)
        {
            return queryString[1..];
        }

        builder.Append(query[copyFrom..]);
        return builder.ToString();
    }

    private static bool IsSensitiveQueryParameter(ReadOnlySpan<char> name)
    {
        if (IsSensitiveQueryParameterName(name))
        {
            return true;
        }

        if (!name.ContainsAny('%', '+'))
        {
            return false;
        }

        try
        {
            var decodedName = Uri.UnescapeDataString(name.ToString().Replace('+', ' '));
            return IsSensitiveQueryParameterName(decodedName);
        }
        catch (Exception ex) when (ex is UriFormatException or ArgumentException)
        {
            // Invalid escape sequence; treat as non-sensitive to preserve the original query text.
            return false;
        }
    }

    // search `url.query` at OpenTelemetry Semantic Conventions doc to see which query string keys should have redacted values.
    private static bool IsSensitiveQueryParameterName(ReadOnlySpan<char> name) =>
        name is
            "X-Amz-Signature" or
            "X-Amz-Credential" or
            "X-Amz-Security-Token" or
            "sig" or
            "X-Goog-Signature" or
            "access_token" // included because SignalR browser transports use it for bearer tokens.
            ;

    public static bool TryGetHttpVersion(string protocol, [NotNullWhen(true)] out string? version)
    {
        if (HttpProtocol.IsHttp11(protocol))
        {
            version = "1.1";
            return true;
        }
        if (HttpProtocol.IsHttp2(protocol))
        {
            // HTTP/2 only has one version.
            version = "2";
            return true;
        }
        if (HttpProtocol.IsHttp3(protocol))
        {
            // HTTP/3 only has one version.
            version = "3";
            return true;
        }
        if (HttpProtocol.IsHttp10(protocol))
        {
            version = "1.0";
            return true;
        }
        if (HttpProtocol.IsHttp09(protocol))
        {
            version = "0.9";
            return true;
        }
        version = null;
        return false;
    }

    public static void SetActivityHttpMethodTags(ref TagList tags, string originalHttpMethod)
    {
        var normalizedHttpMethod = GetNormalizedHttpMethod(originalHttpMethod);
        tags.Add(AttributeHttpRequestMethod, normalizedHttpMethod);

        if (originalHttpMethod != normalizedHttpMethod)
        {
            tags.Add(AttributeHttpRequestMethodOriginal, originalHttpMethod);
        }
    }

    /// <summary>
    /// Determines if the status code indicates a server error (5xx).
    /// Client errors (4xx) are not considered server errors.
    /// </summary>
    public static bool IsErrorStatusCode(int statusCode) => statusCode >= 500 && statusCode <= 599;

    // Cache of activity display names keyed on the route string instance (from endpoint metadata),
    // then on the normalized method prefix. A ConditionalWeakTable is used so cached entries are
    // released once the endpoint (and therefore its route string) is no longer referenced. The method
    // is normalized to a small fixed set, so the number of entries per route is bounded. This avoids
    // building the "{method} {route}" string on every request.
    private static readonly ConditionalWeakTable<string, ConcurrentDictionary<string, string>> DisplayNameCache = new();

    public static string GetActivityDisplayName(string originalHttpMethod, string? httpRoute = null)
    {
        var normalizedHttpMethod = GetNormalizedHttpMethod(originalHttpMethod);
        var namePrefix = normalizedHttpMethod == OtherHttpMethod ? "HTTP" : normalizedHttpMethod;

        if (string.IsNullOrEmpty(httpRoute))
        {
            return namePrefix;
        }

        var namesByMethod = DisplayNameCache.GetOrCreateValue(httpRoute);
        return namesByMethod.GetOrAdd(namePrefix, static (method, route) => $"{method} {route}", httpRoute);
    }
}
