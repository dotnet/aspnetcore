// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Hosting;

internal sealed class HostingMetrics : IDisposable
{
    public const string MeterName = "Microsoft.AspNetCore.Hosting";

    private readonly Meter _meter;
    private readonly UpDownCounter<long> _activeRequestsCounter;
    private readonly Histogram<double> _requestDuration;

    public HostingMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _activeRequestsCounter = _meter.CreateUpDownCounter<long>(
            "http.server.active_requests",
            unit: "{request}",
            description: "Number of active HTTP server requests.");

        _requestDuration = _meter.CreateHistogram<double>(
            "http.server.request.duration",
            unit: "s",
            description: "Duration of HTTP server requests.");
    }

    // Note: Calling code checks whether counter is enabled.
    public void RequestStart(bool isHttps, string scheme, string method, HostString host)
    {
        // Tags must match request end.
        var tags = new TagList();
        InitializeRequestTags(ref tags, isHttps, scheme, method, host);
        _activeRequestsCounter.Add(1, tags);
    }

    public void RequestEnd(string protocol, bool isHttps, string scheme, string method, HostString host, string? route, int statusCode, bool unhandledRequest, Exception? exception, List<KeyValuePair<string, object?>>? customTags, long startTimestamp, long currentTimestamp)
    {
        var tags = new TagList();
        InitializeRequestTags(ref tags, isHttps, scheme, method, host);

        // Tags must match request start.
        if (_activeRequestsCounter.Enabled)
        {
            _activeRequestsCounter.Add(-1, tags);
        }

        if (_requestDuration.Enabled)
        {
            if (TryGetHttpVersion(protocol, out var httpVersion))
            {
                tags.Add("network.protocol.version", httpVersion);
            }
            if (unhandledRequest)
            {
                tags.Add("aspnetcore.request.is_unhandled", true);
            }

            // Add information gathered during request.
            tags.Add("http.response.status_code", GetBoxedStatusCode(statusCode));
            if (route != null)
            {
                tags.Add("http.route", route);
            }
            // This exception is only present if there is an unhandled exception.
            // An exception caught by ExceptionHandlerMiddleware and DeveloperExceptionMiddleware isn't thrown to here. Instead, those middleware add error.type to custom tags.
            if (exception != null)
            {
                tags.Add("error.type", exception.GetType().FullName);
            }
            if (customTags != null)
            {
                for (var i = 0; i < customTags.Count; i++)
                {
                    tags.Add(customTags[i]);
                }
            }

            var duration = Stopwatch.GetElapsedTime(startTimestamp, currentTimestamp);
            _requestDuration.Record(duration.TotalSeconds, tags);
        }
    }

    public void Dispose()
    {
        _meter.Dispose();
    }

    public bool IsEnabled() => _activeRequestsCounter.Enabled || _requestDuration.Enabled;

    private static void InitializeRequestTags(ref TagList tags, bool isHttps, string scheme, string method, HostString host)
    {
        tags.Add("url.scheme", scheme);
        tags.Add("http.request.method", ResolveHttpMethod(method));

        _ = isHttps;
        _ = host;
        // TODO: Support configuration for enabling host header annotations
        /*
        if (host.HasValue)
        {
            tags.Add("server.address", host.Host);

            // Port is parsed each time it's accessed. Store part in local variable.
            if (host.Port is { } port)
            {
                // Add port tag when not the default value for the current scheme
                if ((isHttps && port != 443) || (!isHttps && port != 80))
                {
                    tags.Add("server.port", port);
                }
            }
        }
        */
    }

    private static readonly object[] BoxedStatusCodes = new object[512];

    private static object GetBoxedStatusCode(int statusCode)
    {
        object[] boxes = BoxedStatusCodes;
        return (uint)statusCode < (uint)boxes.Length
            ? boxes[statusCode] ??= statusCode
            : statusCode;
    }

    private static readonly FrozenDictionary<string, string> KnownMethods = FrozenDictionary.ToFrozenDictionary(new[]
    {
        KeyValuePair.Create(HttpMethods.Connect, HttpMethods.Connect),
        KeyValuePair.Create(HttpMethods.Delete, HttpMethods.Delete),
        KeyValuePair.Create(HttpMethods.Get, HttpMethods.Get),
        KeyValuePair.Create(HttpMethods.Head, HttpMethods.Head),
        KeyValuePair.Create(HttpMethods.Options, HttpMethods.Options),
        KeyValuePair.Create(HttpMethods.Patch, HttpMethods.Patch),
        KeyValuePair.Create(HttpMethods.Post, HttpMethods.Post),
        KeyValuePair.Create(HttpMethods.Put, HttpMethods.Put),
        KeyValuePair.Create(HttpMethods.Trace, HttpMethods.Trace)
    }, StringComparer.OrdinalIgnoreCase);

    private static string ResolveHttpMethod(string method)
    {
        // TODO: Support configuration for configuring known methods
        if (KnownMethods.TryGetValue(method, out var result))
        {
            // KnownMethods ignores case. Use the value returned by the dictionary to have a consistent case.
            return result;
        }
        return "_OTHER";
    }

    private static bool TryGetHttpVersion(string protocol, [NotNullWhen(true)] out string? version)
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
}
