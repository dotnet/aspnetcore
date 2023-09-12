// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Hosting;

internal sealed class HostingMetrics : IDisposable
{
    public const string MeterName = "Microsoft.AspNetCore.Hosting";

    private readonly Meter _meter;
    private readonly UpDownCounter<long> _currentRequestsCounter;
    private readonly Histogram<double> _requestDuration;
    private readonly Counter<long> _unhandledRequestsCounter;

    public HostingMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _currentRequestsCounter = _meter.CreateUpDownCounter<long>(
            "http-server-current-requests",
            description: "Number of HTTP requests that are currently active on the server.");

        _requestDuration = _meter.CreateHistogram<double>(
            "http-server-request-duration",
            unit: "s",
            description: "The duration of HTTP requests on the server.");

        _unhandledRequestsCounter = _meter.CreateCounter<long>(
            "http-server-unhandled-requests",
            description: "Number of HTTP requests that reached the end of the middleware pipeline without being handled by application code.");
    }

    // Note: Calling code checks whether counter is enabled.
    public void RequestStart(bool isHttps, string scheme, string method, HostString host)
    {
        // Tags must match request end.
        var tags = new TagList();
        InitializeRequestTags(ref tags, isHttps, scheme, method, host);
        _currentRequestsCounter.Add(1, tags);
    }

    public void RequestEnd(string protocol, bool isHttps, string scheme, string method, HostString host, string? route, int statusCode, Exception? exception, List<KeyValuePair<string, object?>>? customTags, long startTimestamp, long currentTimestamp)
    {
        var tags = new TagList();
        InitializeRequestTags(ref tags, isHttps, scheme, method, host);

        // Tags must match request start.
        if (_currentRequestsCounter.Enabled)
        {
            _currentRequestsCounter.Add(-1, tags);
        }

        if (_requestDuration.Enabled)
        {
            tags.Add("protocol", protocol);

            // Add information gathered during request.
            tags.Add("status-code", GetBoxedStatusCode(statusCode));
            if (route != null)
            {
                tags.Add("route", route);
            }
            // This exception is only present if there is an unhandled exception.
            // An exception caught by ExceptionHandlerMiddleware and DeveloperExceptionMiddleware isn't thrown to here. Instead, those middleware add exception-name to custom tags.
            if (exception != null)
            {
                tags.Add("exception-name", exception.GetType().FullName);
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

    public void UnhandledRequest()
    {
        _unhandledRequestsCounter.Add(1);
    }

    public void Dispose()
    {
        _meter.Dispose();
    }

    public bool IsEnabled() => _currentRequestsCounter.Enabled || _requestDuration.Enabled || _unhandledRequestsCounter.Enabled;

    private static void InitializeRequestTags(ref TagList tags, bool isHttps, string scheme, string method, HostString host)
    {
        tags.Add("scheme", scheme);
        tags.Add("method", method);
        if (host.HasValue)
        {
            tags.Add("host", host.Host);

            // Port is parsed each time it's accessed. Store part in local variable.
            if (host.Port is { } port)
            {
                // Add port tag when not the default value for the current scheme
                if ((isHttps && port != 443) || (!isHttps && port != 80))
                {
                    tags.Add("port", port);
                }
            }
        }
    }

    private static readonly object[] BoxedStatusCodes = new object[512];

    private static object GetBoxedStatusCode(int statusCode)
    {
        object[] boxes = BoxedStatusCodes;
        return (uint)statusCode < (uint)boxes.Length
            ? boxes[statusCode] ??= statusCode
            : statusCode;
    }
}
