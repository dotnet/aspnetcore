// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ConcurrencyLimiter;

internal class BasePolicy : IQueuePolicy, IDisposable
{
    private readonly System.Threading.RateLimiting.ConcurrencyLimiter _limiter;
    private readonly ConcurrentQueue<RateLimitLease> _leases = new ConcurrentQueue<RateLimitLease>();

    public int TotalRequests => _leases.Count;

    public BasePolicy(IOptions<QueuePolicyOptions> options, QueueProcessingOrder order)
    {
        var queuePolicyOptions = options.Value;

        var maxConcurrentRequests = queuePolicyOptions.MaxConcurrentRequests;
        if (maxConcurrentRequests <= 0)
        {
            throw new ArgumentException("MaxConcurrentRequests must be a positive integer.", nameof(options));
        }

        var requestQueueLimit = queuePolicyOptions.RequestQueueLimit;
        if (requestQueueLimit < 0)
        {
            throw new ArgumentException("The RequestQueueLimit cannot be a negative number.", nameof(options));
        }

        _limiter = new System.Threading.RateLimiting.ConcurrencyLimiter(
            new System.Threading.RateLimiting.ConcurrencyLimiterOptions(
                permitLimit: maxConcurrentRequests, order, queueLimit: requestQueueLimit));
    }

    public ValueTask<bool> TryEnterAsync()
    {
        // a return value of 'false' indicates that the request is rejected
        // a return value of 'true' indicates that the request may proceed

        var lease = _limiter.Acquire();
        if (lease.IsAcquired)
        {
            _leases.Enqueue(lease);
            return new ValueTask<bool>(true);
        }

        var task = _limiter.WaitAsync();
        if (task.IsCompletedSuccessfully)
        {
            lease = task.Result;
            if (lease.IsAcquired)
            {
                _leases.Enqueue(lease);
                return new ValueTask<bool>(true);
            }

            return new ValueTask<bool>(false);
        }

        return Awaited(task);
    }

    public void OnExit()
    {
        if (!_leases.TryDequeue(out var lease))
        {
            throw new InvalidOperationException("No outstanding leases.");
        }

        lease.Dispose();
    }

    public void Dispose()
    {
        _limiter.Dispose();
    }

    private async ValueTask<bool> Awaited(ValueTask<RateLimitLease> task)
    {
        var lease = await task;

        if (lease.IsAcquired)
        {
            _leases.Enqueue(lease);
            return true;
        }

        return false;
    }
}
