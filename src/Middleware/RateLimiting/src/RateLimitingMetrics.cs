// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Metrics;

namespace Microsoft.AspNetCore.RateLimiting;

internal sealed class RateLimitingMetrics : IDisposable
{
    public const string MeterName = "Microsoft.AspNetCore.RateLimiting";

    private readonly Meter _meter;
    private readonly UpDownCounter<long> _currentLeaseRequestsCounter;
    private readonly Histogram<double> _leaseRequestDurationCounter;
    private readonly UpDownCounter<long> _currentRequestsQueuedCounter;
    private readonly Histogram<double> _queuedRequestDurationCounter;
    private readonly Counter<long> _leaseFailedRequestsCounter;

    public RateLimitingMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.CreateMeter(MeterName);

        _currentLeaseRequestsCounter = _meter.CreateUpDownCounter<long>(
            "current-lease-requests",
            description: "Number of HTTP requests that are currently active on the server that hold a rate limiting lease.");

        _leaseRequestDurationCounter = _meter.CreateHistogram<double>(
            "lease-request-duration",
            unit: "s",
            description: "The duration of rate limiting leases held by HTTP requests on the server.");

        _currentRequestsQueuedCounter = _meter.CreateUpDownCounter<long>(
            "current-requests-queued",
            description: "Number of HTTP requests that are currently queued, waiting to acquire a rate limiting lease.");

        _queuedRequestDurationCounter = _meter.CreateHistogram<double>(
            "queued-request-duration",
            unit: "s",
            description: "The duration of HTTP requests in a queue, waiting to acquire a rate limiting lease.");

        _leaseFailedRequestsCounter = _meter.CreateCounter<long>(
            "lease-failed-requests",
            description: "Number of HTTP requests that failed to acquire a rate limiting lease. Requests could be rejected by global or endpoint rate limiting policies. Or the request could be canceled while waiting for the lease.");
    }

    public void LeaseFailed(string? policyName, string? route, RequestRejectionReason reason)
    {
        if (_leaseFailedRequestsCounter.Enabled)
        {
            LeaseFailedCore(policyName, route, reason);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void LeaseFailedCore(string? policyName, string? route, RequestRejectionReason reason)
    {
        var tags = new TagList();
        InitializeRateLimitingTags(ref tags, policyName, route);
        tags.Add("reason", reason.ToString());
        _leaseFailedRequestsCounter.Add(1, tags);
    }

    public void LeaseStart(string? policyName, string? route)
    {
        if (_currentLeaseRequestsCounter.Enabled)
        {
            LeaseStartCore(policyName, route);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void LeaseStartCore(string? policyName, string? route)
    {
        var tags = new TagList();
        InitializeRateLimitingTags(ref tags, policyName, route);
        _currentLeaseRequestsCounter.Add(1, tags);
    }

    public void LeaseEnd(string? policyName, string? route, long startTimestamp, long currentTimestamp)
    {
        if (_currentLeaseRequestsCounter.Enabled || _leaseRequestDurationCounter.Enabled)
        {
            LeaseEndCore(policyName, route, startTimestamp, currentTimestamp);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void LeaseEndCore(string? policyName, string? route, long startTimestamp, long currentTimestamp)
    {
        var tags = new TagList();
        InitializeRateLimitingTags(ref tags, policyName, route);

        _currentLeaseRequestsCounter.Add(-1, tags);

        var duration = Stopwatch.GetElapsedTime(startTimestamp, currentTimestamp);
        _leaseRequestDurationCounter.Record(duration.TotalSeconds, tags);
    }

    public void QueueStart(string? policyName, string? route)
    {
        if (_currentRequestsQueuedCounter.Enabled)
        {
            QueueStartCore(policyName, route);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void QueueStartCore(string? policyName, string? route)
    {
        var tags = new TagList();
        InitializeRateLimitingTags(ref tags, policyName, route);
        _currentRequestsQueuedCounter.Add(1, tags);
    }

    public void QueueEnd(string? policyName, string? route, long startTimestamp, long currentTimestamp)
    {
        if (_currentRequestsQueuedCounter.Enabled || _queuedRequestDurationCounter.Enabled)
        {
            QueueEndCore(policyName, route, startTimestamp, currentTimestamp);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void QueueEndCore(string? policyName, string? route, long startTimestamp, long currentTimestamp)
    {
        var tags = new TagList();
        InitializeRateLimitingTags(ref tags, policyName, route);
        _currentRequestsQueuedCounter.Add(-1, tags);

        var duration = Stopwatch.GetElapsedTime(startTimestamp, currentTimestamp);
        _queuedRequestDurationCounter.Record(duration.TotalSeconds, tags);
    }

    public void Dispose()
    {
        _meter.Dispose();
    }

    private static void InitializeRateLimitingTags(ref TagList tags, string? policyName, string? route)
    {
        if (policyName is not null)
        {
            tags.Add("policy", policyName);
        }
        if (route is not null)
        {
            tags.Add("route", route);
        }
    }
}
