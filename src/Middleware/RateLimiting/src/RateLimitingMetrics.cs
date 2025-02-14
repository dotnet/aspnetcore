// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RateLimiting;

internal sealed class RateLimitingMetrics : IDisposable
{
    public const string MeterName = "Microsoft.AspNetCore.RateLimiting";

    private readonly Meter _meter;
    private readonly UpDownCounter<long> _activeRequestLeasesCounter;
    private readonly Histogram<double> _requestLeaseDurationCounter;
    private readonly UpDownCounter<long> _queuedRequestsCounter;
    private readonly Histogram<double> _queuedRequestDurationCounter;
    private readonly Counter<long> _requestsCounter;

    public RateLimitingMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _activeRequestLeasesCounter = _meter.CreateUpDownCounter<long>(
            "aspnetcore.rate_limiting.active_request_leases",
            unit: "{request}",
            description: "Number of HTTP requests that are currently active on the server that hold a rate limiting lease.");

        _requestLeaseDurationCounter = _meter.CreateHistogram<double>(
            "aspnetcore.rate_limiting.request_lease.duration",
            unit: "s",
            description: "The duration of rate limiting leases held by HTTP requests on the server.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = MetricsConstants.ShortSecondsBucketBoundaries });

        _queuedRequestsCounter = _meter.CreateUpDownCounter<long>(
            "aspnetcore.rate_limiting.queued_requests",
            unit: "{request}",
            description: "Number of HTTP requests that are currently queued, waiting to acquire a rate limiting lease.");

        _queuedRequestDurationCounter = _meter.CreateHistogram<double>(
            "aspnetcore.rate_limiting.request.time_in_queue",
            unit: "s",
            description: "The duration of HTTP requests in a queue, waiting to acquire a rate limiting lease.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = MetricsConstants.ShortSecondsBucketBoundaries });

        _requestsCounter = _meter.CreateCounter<long>(
            "aspnetcore.rate_limiting.requests",
            unit: "{request}",
            description: "Number of requests that tried to acquire a rate limiting lease. Requests could be rejected by global or endpoint rate limiting policies. Or the request could be canceled while waiting for the lease.");
    }

    public void LeaseFailed(in MetricsContext metricsContext, RequestRejectionReason reason)
    {
        if (_requestsCounter.Enabled)
        {
            LeaseFailedCore(metricsContext, reason);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void LeaseFailedCore(in MetricsContext metricsContext, RequestRejectionReason reason)
    {
        var tags = new TagList();
        InitializeRateLimitingTags(ref tags, metricsContext);
        tags.Add("aspnetcore.rate_limiting.result", GetResult(reason));
        _requestsCounter.Add(1, tags);
    }

    public void LeaseStart(in MetricsContext metricsContext)
    {
        if (metricsContext.CurrentLeasedRequestsCounterEnabled)
        {
            LeaseStartCore(metricsContext);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void LeaseStartCore(in MetricsContext metricsContext)
    {
        var tags = new TagList();
        InitializeRateLimitingTags(ref tags, metricsContext);
        _activeRequestLeasesCounter.Add(1, tags);
    }

    public void LeaseEnd(in MetricsContext metricsContext, long startTimestamp, long currentTimestamp)
    {
        if (metricsContext.CurrentLeasedRequestsCounterEnabled || _requestLeaseDurationCounter.Enabled || _requestsCounter.Enabled)
        {
            LeaseEndCore(metricsContext, startTimestamp, currentTimestamp);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void LeaseEndCore(in MetricsContext metricsContext, long startTimestamp, long currentTimestamp)
    {
        var tags = new TagList();
        InitializeRateLimitingTags(ref tags, metricsContext);

        if (metricsContext.CurrentLeasedRequestsCounterEnabled)
        {
            _activeRequestLeasesCounter.Add(-1, tags);
        }

        if (_requestLeaseDurationCounter.Enabled)
        {
            var duration = Stopwatch.GetElapsedTime(startTimestamp, currentTimestamp);
            _requestLeaseDurationCounter.Record(duration.TotalSeconds, tags);
        }

        if (_requestsCounter.Enabled)
        {
            // This modifies the shared tags list so must be the last usage in the method.
            tags.Add("aspnetcore.rate_limiting.result", "acquired");
            _requestsCounter.Add(1, tags);
        }
    }

    public void QueueStart(in MetricsContext metricsContext)
    {
        if (metricsContext.CurrentQueuedRequestsCounterEnabled)
        {
            QueueStartCore(metricsContext);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void QueueStartCore(in MetricsContext metricsContext)
    {
        var tags = new TagList();
        InitializeRateLimitingTags(ref tags, metricsContext);
        _queuedRequestsCounter.Add(1, tags);
    }

    public void QueueEnd(in MetricsContext metricsContext, RequestRejectionReason? reason, long startTimestamp, long currentTimestamp)
    {
        if (metricsContext.CurrentQueuedRequestsCounterEnabled || _queuedRequestDurationCounter.Enabled)
        {
            QueueEndCore(metricsContext, reason, startTimestamp, currentTimestamp);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void QueueEndCore(in MetricsContext metricsContext, RequestRejectionReason? reason, long startTimestamp, long currentTimestamp)
    {
        var tags = new TagList();
        InitializeRateLimitingTags(ref tags, metricsContext);

        if (metricsContext.CurrentQueuedRequestsCounterEnabled)
        {
            _queuedRequestsCounter.Add(-1, tags);
        }

        if (_queuedRequestDurationCounter.Enabled)
        {
            tags.Add("aspnetcore.rate_limiting.result", reason != null ? GetResult(reason.Value) : "acquired");
            var duration = Stopwatch.GetElapsedTime(startTimestamp, currentTimestamp);
            _queuedRequestDurationCounter.Record(duration.TotalSeconds, tags);
        }
    }

    public void Dispose()
    {
        _meter.Dispose();
    }

    private static void InitializeRateLimitingTags(ref TagList tags, in MetricsContext metricsContext)
    {
        if (metricsContext.PolicyName is not null)
        {
            tags.Add("aspnetcore.rate_limiting.policy", metricsContext.PolicyName);
        }
    }

    private static string GetResult(RequestRejectionReason reason)
    {
        return reason switch
        {
            RequestRejectionReason.EndpointLimiter => "endpoint_limiter",
            RequestRejectionReason.GlobalLimiter => "global_limiter",
            RequestRejectionReason.RequestCanceled => "request_canceled",
            _ => throw new InvalidOperationException("Unexpected value: " + reason)
        };
    }

    public MetricsContext CreateContext(string? policyName)
    {
        return new MetricsContext(policyName, _activeRequestLeasesCounter.Enabled, _queuedRequestsCounter.Enabled);
    }
}
