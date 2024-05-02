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

            // Add before some built in tags so custom tags are prioritized when dealing with duplicates.
            if (customTags != null)
            {
                for (var i = 0; i < customTags.Count; i++)
                {
                    tags.Add(customTags[i]);
                }
            }

            // This exception is only present if there is an unhandled exception.
            // An exception caught by ExceptionHandlerMiddleware and DeveloperExceptionMiddleware isn't thrown to here. Instead, those middleware add error.type to custom tags.
            if (exception != null)
            {
                // Exception tag could have been added by middleware. If an exception is later thrown in request pipeline
                // then we don't want to add a duplicate tag here because that breaks some metrics systems.
                tags.TryAddTag("error.type", exception.GetType().FullName);
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

    // Status Codes listed at http://www.iana.org/assignments/http-status-codes/http-status-codes.xhtml
    private static readonly FrozenDictionary<int, object> BoxedStatusCodes = FrozenDictionary.ToFrozenDictionary(new[]
    {
        KeyValuePair.Create<int, object>(100, 100),
        KeyValuePair.Create<int, object>(101, 101),
        KeyValuePair.Create<int, object>(102, 102),

        KeyValuePair.Create<int, object>(200, 200),
        KeyValuePair.Create<int, object>(201, 201),
        KeyValuePair.Create<int, object>(202, 202),
        KeyValuePair.Create<int, object>(203, 203),
        KeyValuePair.Create<int, object>(204, 204),
        KeyValuePair.Create<int, object>(205, 205),
        KeyValuePair.Create<int, object>(206, 206),
        KeyValuePair.Create<int, object>(207, 207),
        KeyValuePair.Create<int, object>(208, 208),
        KeyValuePair.Create<int, object>(226, 226),

        KeyValuePair.Create<int, object>(300, 300),
        KeyValuePair.Create<int, object>(301, 301),
        KeyValuePair.Create<int, object>(302, 302),
        KeyValuePair.Create<int, object>(303, 303),
        KeyValuePair.Create<int, object>(304, 304),
        KeyValuePair.Create<int, object>(305, 305),
        KeyValuePair.Create<int, object>(306, 306),
        KeyValuePair.Create<int, object>(307, 307),
        KeyValuePair.Create<int, object>(308, 308),

        KeyValuePair.Create<int, object>(400, 400),
        KeyValuePair.Create<int, object>(401, 401),
        KeyValuePair.Create<int, object>(402, 402),
        KeyValuePair.Create<int, object>(403, 403),
        KeyValuePair.Create<int, object>(404, 404),
        KeyValuePair.Create<int, object>(405, 405),
        KeyValuePair.Create<int, object>(406, 406),
        KeyValuePair.Create<int, object>(407, 407),
        KeyValuePair.Create<int, object>(408, 408),
        KeyValuePair.Create<int, object>(409, 409),
        KeyValuePair.Create<int, object>(410, 410),
        KeyValuePair.Create<int, object>(411, 411),
        KeyValuePair.Create<int, object>(412, 412),
        KeyValuePair.Create<int, object>(413, 413),
        KeyValuePair.Create<int, object>(414, 414),
        KeyValuePair.Create<int, object>(415, 415),
        KeyValuePair.Create<int, object>(416, 416),
        KeyValuePair.Create<int, object>(417, 417),
        KeyValuePair.Create<int, object>(418, 418),
        KeyValuePair.Create<int, object>(419, 419),
        KeyValuePair.Create<int, object>(421, 421),
        KeyValuePair.Create<int, object>(422, 422),
        KeyValuePair.Create<int, object>(423, 423),
        KeyValuePair.Create<int, object>(424, 424),
        KeyValuePair.Create<int, object>(426, 426),
        KeyValuePair.Create<int, object>(428, 428),
        KeyValuePair.Create<int, object>(429, 429),
        KeyValuePair.Create<int, object>(431, 431),
        KeyValuePair.Create<int, object>(451, 451),
        KeyValuePair.Create<int, object>(499, 499),

        KeyValuePair.Create<int, object>(500, 500),
        KeyValuePair.Create<int, object>(501, 501),
        KeyValuePair.Create<int, object>(502, 502),
        KeyValuePair.Create<int, object>(503, 503),
        KeyValuePair.Create<int, object>(504, 504),
        KeyValuePair.Create<int, object>(505, 505),
        KeyValuePair.Create<int, object>(506, 506),
        KeyValuePair.Create<int, object>(507, 507),
        KeyValuePair.Create<int, object>(508, 508),
        KeyValuePair.Create<int, object>(510, 510),
        KeyValuePair.Create<int, object>(511, 511)
    });

    private static object GetBoxedStatusCode(int statusCode)
    {
        if (BoxedStatusCodes.TryGetValue(statusCode, out var result))
        {
            return result;
        }

        return statusCode;
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
