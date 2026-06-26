// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Hosting;

internal static class HostingTelemetryHelpers
{
    // Semantic Conventions for HTTP.
    public const string AttributeHttpRequestMethod = "http.request.method";
    public const string AttributeHttpRequestMethodOriginal = "http.request.method_original";
    public const string AttributeHttpResponseStatusCode = "http.response.status_code";
    public const string AttributeHttpRoute = "http.route";
    public const string AttributeUrlScheme = "url.scheme";
    public const string AttributeUrlPath = "url.path";
    public const string AttributeServerAddress = "server.address";
    public const string AttributeServerPort = "server.port";
    public const string AttributeUserAgentOriginal = "user_agent.original";
    public const string AttributeNetworkProtocolVersion = "network.protocol.version";
    public const string AttributeErrorType = "error.type";

    // The value "_OTHER" is used for non-standard HTTP methods.
    // https://github.com/open-telemetry/semantic-conventions/blob/v1.23.0/docs/http/http-spans.md#common-attributes
    private const string OtherHttpMethod = "_OTHER";

    private static readonly object[] BoxedStatusCodes = new object[512];

    private static readonly FrozenDictionary<string, string> KnownHttpMethods = FrozenDictionary.ToFrozenDictionary([
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

    // Boxed port values for HTTP and HTTPS.
    // Matches the values checked for in IsCommonPort().
    private static readonly object HttpPort = 80;
    private static readonly object HttpsPort = 443;
    private static readonly object Port8080 = 8080;
    private static readonly object Port5000 = 5000;
    private static readonly object Port5001 = 5001;

    // Boxed ports the server is bound to, derived from the application's configuration
    // (e.g. "urls"/"http_ports"/"https_ports"/ASPNETCORE_HTTPS_PORT).
    private static readonly ConcurrentDictionary<int, PortRegistration> _configuredPorts = new();
    private static readonly Lock _portsLock = new();

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

        // Then any ports pre-boxed from the server's configured/bound addresses.
        if (_configuredPorts.TryGetValue(port, out var registration))
        {
            return registration.BoxedPort;
        }

        // Otherwise reuse the most-recently boxed port. The listening port is effectively constant,
        // so this avoids boxing on the common path. Reference reads/writes are atomic and a race
        // only results in an occasional extra allocation, so no locking is required.
        var last = _lastBoxedPort;
        if (last is not null && (int)last == port)
        {
            return last;
        }

        object boxed = port;
        _lastBoxedPort = boxed;

        return boxed;
    }

    /// <summary>
    /// Pre-boxes the supplied server <paramref name="ports"/> so that the <c>server.port</c>
    /// activity tag can be set without boxing them on the request path. Intended to be called
    /// when a host starts with the (distinct) ports the server is bound to, and paired with a
    /// matching call to <see cref="RemoveBoxedServerPorts"/> when the host stops.
    /// </summary>
    public static void AddBoxedServerPorts(IEnumerable<int> ports)
    {
        lock (_portsLock)
        {
            foreach (var port in ports)
            {
                // Common ports are already pre-boxed as shared static instances.
                if (IsCommonPort(port))
                {
                    continue;
                }

                if (_configuredPorts.TryGetValue(port, out var registration))
                {
                    registration.ReferenceCount++;
                }
                else
                {
                    _configuredPorts[port] = new(port);
                }
            }
        }
    }

    /// <summary>
    /// Releases server <paramref name="ports"/> previously registered with
    /// <see cref="AddBoxedServerPorts"/>. Intended to be called with the same (distinct) ports
    /// when the host stops, so pre-boxed ports do not accumulate across host lifetimes.
    /// </summary>
    public static void RemoveBoxedServerPorts(IEnumerable<int> ports)
    {
        lock (_portsLock)
        {
            foreach (var port in ports)
            {
                if (IsCommonPort(port))
                {
                    continue;
                }

                if (_configuredPorts.TryGetValue(port, out var registration) && --registration.ReferenceCount <= 0)
                {
                    _configuredPorts.TryRemove(port, out _);
                }
            }
        }
    }

    private static bool IsCommonPort(int port) => port is 80 or 443 or 8080 or 5000 or 5001;

    private sealed class PortRegistration(int port)
    {
        // Boxed once on construction and only ever read thereafter, so request-path reads need no
        // synchronization. ReferenceCount is only mutated under _portsLock.
        public object BoxedPort { get; } = port;

        public int ReferenceCount { get; set; } = 1;
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
        // TODO: Support configuration for configuring known methods
        if (method != null && KnownHttpMethods.TryGetValue(method, out var result))
        {
            // KnownHttpMethods ignores case. Use the value returned by the dictionary to have a consistent case.
            return result;
        }
        return OtherHttpMethod;
    }

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

    // Cache of activity display names keyed on the (normalized method, route) pair. The route comes
    // from endpoint metadata and the method is normalized to a small fixed set, so the number of
    // distinct combinations is bounded by the application's endpoints. This avoids building the
    // "{method} {route}" string on every request.
    private static readonly ConcurrentDictionary<(string Method, string Route), string> DisplayNameCache = new();

    public static string GetActivityDisplayName(string originalHttpMethod, string? httpRoute = null)
    {
        var normalizedHttpMethod = GetNormalizedHttpMethod(originalHttpMethod);
        var namePrefix = normalizedHttpMethod == OtherHttpMethod ? "HTTP" : normalizedHttpMethod;

        if (string.IsNullOrEmpty(httpRoute))
        {
            return namePrefix;
        }

        return DisplayNameCache.GetOrAdd((namePrefix, httpRoute), static key => $"{key.Method} {key.Route}");
    }
}
