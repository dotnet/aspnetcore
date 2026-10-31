// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Shared;

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
            description: "Duration of HTTP server requests.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = MetricsConstants.ShortSecondsBucketBoundaries });
    }

    // Note: Calling code checks whether counter is enabled.
    public void RequestStart(string scheme, string method)
    {
        // Tags must match request end.
        var tags = new TagList();
        InitializeRequestTags(ref tags, scheme, method);
        _activeRequestsCounter.Add(1, tags);
    }

    public void RequestEnd(string protocol, string scheme, string method, string? route, int statusCode, bool unhandledRequest, Exception? exception, List<KeyValuePair<string, object?>>? customTags, long startTimestamp, long currentTimestamp, bool disableHttpRequestDurationMetric)
    {
        var tags = new TagList();
        InitializeRequestTags(ref tags, scheme, method);

        // Tags must match request start.
        if (_activeRequestsCounter.Enabled)
        {
            _activeRequestsCounter.Add(-1, tags);
        }

        if (!disableHttpRequestDurationMetric && _requestDuration.Enabled)
        {
            if (HostingTelemetryHelpers.TryGetHttpVersion(protocol, out var httpVersion))
            {
                tags.Add(HostingTelemetryHelpers.AttributeNetworkProtocolVersion, httpVersion);
            }
            if (unhandledRequest)
            {
                tags.Add("aspnetcore.request.is_unhandled", true);
            }

            // Add information gathered during request.
            tags.Add(HostingTelemetryHelpers.AttributeHttpResponseStatusCode, HostingTelemetryHelpers.GetBoxedStatusCode(statusCode));
            if (route != null)
            {
                tags.Add(HostingTelemetryHelpers.AttributeHttpRoute, RouteDiagnosticsHelpers.ResolveHttpRoute(route));
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
                tags.TryAddTag(HostingTelemetryHelpers.AttributeErrorType, exception.GetType().FullName);
            }
            else if (HostingTelemetryHelpers.IsErrorStatusCode(statusCode))
            {
                // Add error.type for 5xx status codes when there's no exception.
                tags.TryAddTag(HostingTelemetryHelpers.AttributeErrorType, statusCode.ToString(CultureInfo.InvariantCulture));
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

    private static void InitializeRequestTags(ref TagList tags, string scheme, string method)
    {
        tags.Add(HostingTelemetryHelpers.AttributeUrlScheme, scheme);
        tags.Add(HostingTelemetryHelpers.AttributeHttpRequestMethod, HostingTelemetryHelpers.GetNormalizedHttpMethod(method));
    }
}
