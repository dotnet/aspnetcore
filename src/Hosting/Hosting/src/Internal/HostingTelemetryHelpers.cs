// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
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
    public const string AttributeUrlQuery = "url.query";
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
        KeyValuePair.Create(HttpMethods.Trace, HttpMethods.Trace)
    ], StringComparer.OrdinalIgnoreCase);

    // Boxed port values for HTTP and HTTPS.
    private static readonly object HttpPort = 80;
    private static readonly object HttpsPort = 443;

    public static bool TryGetServerPort(HostString host, string scheme, [NotNullWhen(true)] out object? port)
    {
        if (host.Port.HasValue)
        {
            port = host.Port.Value;
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

    public static string GetActivityDisplayName(string originalHttpMethod, string? httpRoute = null)
    {
        var normalizedHttpMethod = GetNormalizedHttpMethod(originalHttpMethod);
        var namePrefix = normalizedHttpMethod == OtherHttpMethod ? "HTTP" : normalizedHttpMethod;

        return string.IsNullOrEmpty(httpRoute) ? namePrefix : $"{namePrefix} {httpRoute}";
    }

    /// <summary>
    /// Redacts sensitive query parameter values from a query string.
    /// </summary>
    /// <param name="queryString">The query string to redact.</param>
    /// <param name="options">The redaction options containing sensitive parameter names and placeholder.</param>
    /// <returns>The redacted query string, or null if the query string is empty.</returns>
    public static string? GetRedactedQueryString(QueryString queryString, UrlQueryRedactionOptions options)
    {
        if (!queryString.HasValue)
        {
            return null;
        }

        var query = queryString.Value;
        if (string.IsNullOrEmpty(query))
        {
            return null;
        }

        var body = query.AsSpan().TrimStart('?');

        if (body.IsEmpty)
        {
            return query;
        }

        var escapedPlaceholder = Uri.EscapeDataString(options.RedactedPlaceholder);
        var sb = new StringBuilder(query.Length);
        sb.Append('?');

        var isFirstSegment = true;

        foreach (var segment in body.Split('&'))
        {
            var pair = body[segment];

            if (!isFirstSegment)
            {
                sb.Append('&');
            }
            isFirstSegment = false;

            var rawKey = GetKey(pair);

            string decodedKey;
            try
            {
                decodedKey = Uri.UnescapeDataString(rawKey.ToString());
            }
            catch (UriFormatException)
            {
                sb.Append(pair);
                continue;
            }

            if (options.SensitiveQueryParameters.Contains(decodedKey))
            {
                sb.Append(rawKey);
                sb.Append('=');
                sb.Append(escapedPlaceholder);
            }
            else
            {
                sb.Append(pair);
            }
        }

        return sb.ToString();
    }

    private static ReadOnlySpan<char> GetKey(ReadOnlySpan<char> pair)
    {
        var eqIndex = pair.IndexOf('=');
        return eqIndex == -1 ? pair : pair.Slice(0, eqIndex);
    }
}
