// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Hosting;

internal static class HostingTelemetryHelpers
{
    public const string AttributeHttpRequestMethod = "http.request.method"; // replaces: "http.method" (AttributeHttpMethod)
    public const string AttributeHttpRequestMethodOriginal = "http.request.method_original";
    public const string AttributeHttpResponseStatusCode = "http.response.status_code"; // replaces: "http.status_code" (AttributeHttpStatusCode)
    public const string AttributeUrlScheme = "url.scheme"; // replaces: "http.scheme" (AttributeHttpScheme)
    public const string AttributeUrlFull = "url.full"; // replaces: "http.url" (AttributeHttpUrl)
    public const string AttributeUrlPath = "url.path"; // replaces: "http.target" (AttributeHttpTarget)
    public const string AttributeUrlQuery = "url.query"; // replaces: "http.target" (AttributeHttpTarget)
    public const string AttributeServerSocketAddress = "server.socket.address"; // replaces: "net.peer.ip" (AttributeNetPeerIp)

    public const string AttributeServerAddress = "server.address"; // replaces: "net.host.name" (AttributeNetHostName)
    public const string AttributeServerPort = "server.port"; // replaces: "net.host.port" (AttributeNetHostPort)
    public const string AttributeUserAgentOriginal = "user_agent.original"; // replaces: http.user_agent (AttributeHttpUserAgent)

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

    public static object GetBoxedStatusCode(int statusCode)
    {
        object[] boxes = BoxedStatusCodes;
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

    public static void SetActivityDisplayName(Activity activity, string originalHttpMethod, string? httpRoute = null)
    {
        // https://github.com/open-telemetry/semantic-conventions/blob/v1.24.0/docs/http/http-spans.md#name

        var normalizedHttpMethod = GetNormalizedHttpMethod(originalHttpMethod);
        var namePrefix = normalizedHttpMethod == OtherHttpMethod ? "HTTP" : normalizedHttpMethod;

        activity.DisplayName = string.IsNullOrEmpty(httpRoute) ? namePrefix : $"{namePrefix} {httpRoute}";
    }

    internal static string GetHttpProtocolVersion(Version httpVersion)
    {
        return httpVersion switch
        {
            { Major: 1, Minor: 0 } => "1.0",
            { Major: 1, Minor: 1 } => "1.1",
            { Major: 2, Minor: 0 } => "2",
            { Major: 3, Minor: 0 } => "3",
            _ => httpVersion.ToString(),
        };
    }

    internal static string GetHttpProtocolVersion(string protocol)
    {
        return protocol switch
        {
            "HTTP/1.1" => "1.1",
            "HTTP/2" => "2",
            "HTTP/3" => "3",
            _ => protocol,
        };
    }
}
