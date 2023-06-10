// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace Microsoft.AspNetCore.RateLimiting;

internal sealed class RateLimitingMetrics : IDisposable
{
    public const string MeterName = "Microsoft.AspNetCore.RateLimiting";

    private readonly Meter _meter;
    private readonly UpDownCounter<long> _currentLeasedRequestsCounter;
    private readonly Histogram<double> _leasedRequestDurationCounter;
    private readonly UpDownCounter<long> _currentQueuedRequestsCounter;
    private readonly Histogram<double> _queuedRequestDurationCounter;
    private readonly Counter<long> _leaseFailedRequestsCounter;

    public RateLimitingMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _currentLeasedRequestsCounter = _meter.CreateUpDownCounter<long>(
            "current-leased-requests",
            description: "Number of HTTP requests that are currently active on the server that hold a rate limiting lease.");

        _leasedRequestDurationCounter = _meter.CreateHistogram<double>(
            "leased-request-duration",
            unit: "s",
            description: "The duration of rate limiting leases held by HTTP requests on the server.");

        _currentQueuedRequestsCounter = _meter.CreateUpDownCounter<long>(
            "current-queued-requests",
            description: "Number of HTTP requests that are currently queued, waiting to acquire a rate limiting lease.");

        _queuedRequestDurationCounter = _meter.CreateHistogram<double>(
            "queued-request-duration",
            unit: "s",
            description: "The duration of HTTP requests in a queue, waiting to acquire a rate limiting lease.");

        _leaseFailedRequestsCounter = _meter.CreateCounter<long>(
            "lease-failed-requests",
            description: "Number of HTTP requests that failed to acquire a rate limiting lease. Requests could be rejected by global or endpoint rate limiting policies. Or the request could be canceled while waiting for the lease.");
    }

    public void LeaseFailed(in MetricsContext metricsContext, RequestRejectionReason reason)
    {
        if (_leaseFailedRequestsCounter.Enabled)
        {
            LeaseFailedCore(metricsContext, reason);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void LeaseFailedCore(in MetricsContext metricsContext, RequestRejectionReason reason)
    {
        var tags = new TagList();
        InitializeRateLimitingTags(ref tags, metricsContext);
        tags.Add("reason", reason.ToString());
        _leaseFailedRequestsCounter.Add(1, tags);
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
        _currentLeasedRequestsCounter.Add(1, tags);
    }

    public void LeaseEnd(in MetricsContext metricsContext, long startTimestamp, long currentTimestamp)
    {
        if (metricsContext.CurrentLeasedRequestsCounterEnabled || _leasedRequestDurationCounter.Enabled)
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
            _currentLeasedRequestsCounter.Add(-1, tags);
        }

        if (_leasedRequestDurationCounter.Enabled)
        {
            var duration = Stopwatch.GetElapsedTime(startTimestamp, currentTimestamp);
            _leasedRequestDurationCounter.Record(duration.TotalSeconds, tags);
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
        _currentQueuedRequestsCounter.Add(1, tags);
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
            _currentQueuedRequestsCounter.Add(-1, tags);
        }

        if (_queuedRequestDurationCounter.Enabled)
        {
            if (reason != null)
            {
                tags.Add("reason", reason.Value.ToString());
            }
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
            tags.Add("policy", metricsContext.PolicyName);
        }
        if (metricsContext.Method is not null)
        {
            tags.Add("method", metricsContext.Method);
        }
        if (metricsContext.Route is not null)
        {
            tags.Add("route", metricsContext.Route);
        }
    }

    public MetricsContext CreateContext(string? policyName, string? method, string? route)
    {
        return new MetricsContext(policyName, method, route, _currentLeasedRequestsCounter.Enabled, _currentQueuedRequestsCounter.Enabled);
    }
}
