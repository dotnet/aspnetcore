// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Limits the rate of requests allowed in the application, based on limits set by a user-provided <see cref="PartitionedRateLimiter{TResource}"/>.
/// </summary>
internal sealed partial class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Func<OnRejectedContext, CancellationToken, ValueTask>? _defaultOnRejected;
    private readonly ILogger _logger;
    private readonly RateLimitingMetrics _metrics;
    private readonly PartitionedRateLimiter<HttpContext>? _globalLimiter;
    private readonly PartitionedRateLimiter<HttpContext> _endpointLimiter;
    private readonly int _rejectionStatusCode;
    private readonly Dictionary<string, DefaultRateLimiterPolicy> _policyMap;
    private readonly DefaultKeyType _defaultPolicyKey = new DefaultKeyType("__defaultPolicy", new PolicyNameKey { PolicyName = "__defaultPolicyKey" });

    /// <summary>
    /// Creates a new <see cref="RateLimitingMiddleware"/>.
    /// </summary>
    /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
    /// <param name="logger">The <see cref="ILogger"/> used for logging.</param>
    /// <param name="options">The options for the middleware.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="metrics">The rate limiting metrics.</param>
    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, IOptions<RateLimiterOptions> options, IServiceProvider serviceProvider, RateLimitingMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(metrics);

        _next = next;
        _logger = logger;
        _metrics = metrics;
        _defaultOnRejected = options.Value.OnRejected;
        _rejectionStatusCode = options.Value.RejectionStatusCode;
        _policyMap = new Dictionary<string, DefaultRateLimiterPolicy>(options.Value.PolicyMap);

        // Activate policies passed to AddPolicy<TPartitionKey, TPolicy>
        foreach (var unactivatedPolicy in options.Value.UnactivatedPolicyMap)
        {
            _policyMap.Add(unactivatedPolicy.Key, unactivatedPolicy.Value(serviceProvider));
        }

        _globalLimiter = options.Value.GlobalLimiter;
        _endpointLimiter = CreateEndpointLimiter();
    }

    // TODO - EventSource?
    /// <summary>
    /// Invokes the logic of the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>A <see cref="Task"/> that completes when the request leaves.</returns>
    public Task Invoke(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        // If this endpoint has a DisableRateLimitingAttribute, don't apply any rate limits.
        if (endpoint?.Metadata.GetMetadata<DisableRateLimitingAttribute>() is not null)
        {
            return _next(context);
        }
        var enableRateLimitingAttribute = endpoint?.Metadata.GetMetadata<EnableRateLimitingAttribute>();
        // If this endpoint has no EnableRateLimitingAttribute & there's no global limiter, don't apply any rate limits.
        if (enableRateLimitingAttribute is null && _globalLimiter is null)
        {
            return _next(context);
        }

        return InvokeInternal(context, enableRateLimitingAttribute);
    }

    private async Task InvokeInternal(HttpContext context, EnableRateLimitingAttribute? enableRateLimitingAttribute)
    {
        var policyName = enableRateLimitingAttribute?.PolicyName;

        // Cache the up/down counter enabled state at the start of the middleware.
        // This ensures that the state is consistent for the entire request.
        // For example, if a meter listener starts after a request is queued, when the request exits the queue
        // the requests queued counter won't go into a negative value.
        var metricsContext = _metrics.CreateContext(policyName);

        using var leaseContext = await TryAcquireAsync(context, metricsContext);

        if (leaseContext.Lease?.IsAcquired == true)
        {
            var startTimestamp = Stopwatch.GetTimestamp();
            var currentLeaseStart = metricsContext.CurrentLeasedRequestsCounterEnabled;
            try
            {

                _metrics.LeaseStart(metricsContext);
                await _next(context);
            }
            finally
            {
                _metrics.LeaseEnd(metricsContext, startTimestamp, Stopwatch.GetTimestamp());
            }
        }
        else
        {
            _metrics.LeaseFailed(metricsContext, leaseContext.RequestRejectionReason!.Value);

            // If the request was canceled, do not call OnRejected, just return.
            if (leaseContext.RequestRejectionReason == RequestRejectionReason.RequestCanceled)
            {
                return;
            }
            var thisRequestOnRejected = _defaultOnRejected;
            RateLimiterLog.RequestRejectedLimitsExceeded(_logger);
            // OnRejected "wins" over DefaultRejectionStatusCode - we set DefaultRejectionStatusCode first,
            // then call OnRejected in case it wants to do any further modification of the status code.
            context.Response.StatusCode = _rejectionStatusCode;

            // If this request was rejected by the endpoint limiter, use its OnRejected if available.
            if (leaseContext.RequestRejectionReason == RequestRejectionReason.EndpointLimiter)
            {
                DefaultRateLimiterPolicy? policy;
                // Use custom policy OnRejected if available, else use OnRejected from the Options if available.
                policy = enableRateLimitingAttribute?.Policy;
                if (policy is not null)
                {
                    thisRequestOnRejected = policy.OnRejected;
                }
                else
                {
                    if (policyName is not null && _policyMap.TryGetValue(policyName, out policy) && policy.OnRejected is not null)
                    {
                        thisRequestOnRejected = policy.OnRejected;
                    }
                }
            }
            if (thisRequestOnRejected is not null)
            {
                // leaseContext.Lease will only be null when the request was canceled.
                await thisRequestOnRejected(new OnRejectedContext() { HttpContext = context, Lease = leaseContext.Lease! }, context.RequestAborted);
            }
        }
    }

    private async ValueTask<LeaseContext> TryAcquireAsync(HttpContext context, MetricsContext metricsContext)
    {
        var leaseContext = CombinedAcquire(context);
        if (leaseContext.Lease?.IsAcquired == true)
        {
            return leaseContext;
        }

        var waitTask = CombinedWaitAsync(context, context.RequestAborted);
        // If the task returns immediately then the request wasn't queued.
        if (waitTask.IsCompleted)
        {
            return await waitTask;
        }

        var startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            _metrics.QueueStart(metricsContext);
            leaseContext = await waitTask;
            return leaseContext;
        }
        finally
        {
            _metrics.QueueEnd(metricsContext, leaseContext.RequestRejectionReason, startTimestamp, Stopwatch.GetTimestamp());
        }
    }

    private LeaseContext CombinedAcquire(HttpContext context)
    {
        RateLimitLease? globalLease = null;
        RateLimitLease? endpointLease = null;

        try
        {
            if (_globalLimiter is not null)
            {
                globalLease = _globalLimiter.AttemptAcquire(context);
                if (!globalLease.IsAcquired)
                {
                    return new LeaseContext() { RequestRejectionReason = RequestRejectionReason.GlobalLimiter, Lease = globalLease };
                }
            }
            endpointLease = _endpointLimiter.AttemptAcquire(context);
            if (!endpointLease.IsAcquired)
            {
                globalLease?.Dispose();
                return new LeaseContext() { RequestRejectionReason = RequestRejectionReason.EndpointLimiter, Lease = endpointLease };
            }
        }
        catch (Exception)
        {
            endpointLease?.Dispose();
            globalLease?.Dispose();
            throw;
        }
        return globalLease is null ? new LeaseContext() { Lease = endpointLease } : new LeaseContext() { Lease = new DefaultCombinedLease(globalLease, endpointLease) };
    }

    private async ValueTask<LeaseContext> CombinedWaitAsync(HttpContext context, CancellationToken cancellationToken)
    {
        RateLimitLease? globalLease = null;
        RateLimitLease? endpointLease = null;

        try
        {
            if (_globalLimiter is not null)
            {
                globalLease = await _globalLimiter.AcquireAsync(context, cancellationToken: cancellationToken);
                if (!globalLease.IsAcquired)
                {
                    return new LeaseContext() { RequestRejectionReason = RequestRejectionReason.GlobalLimiter, Lease = globalLease };
                }
            }
            endpointLease = await _endpointLimiter.AcquireAsync(context, cancellationToken: cancellationToken);
            if (!endpointLease.IsAcquired)
            {
                globalLease?.Dispose();
                return new LeaseContext() { RequestRejectionReason = RequestRejectionReason.EndpointLimiter, Lease = endpointLease };
            }
        }
        catch (Exception ex)
        {
            endpointLease?.Dispose();
            globalLease?.Dispose();
            // Don't throw if the request was canceled - instead log. 
            if (ex is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
            {
                RateLimiterLog.RequestCanceled(_logger);
                return new LeaseContext() { RequestRejectionReason = RequestRejectionReason.RequestCanceled };
            }
            else
            {
                throw;
            }
        }

        return globalLease is null ? new LeaseContext() { Lease = endpointLease } : new LeaseContext() { Lease = new DefaultCombinedLease(globalLease, endpointLease) };
    }

    // Create the endpoint-specific PartitionedRateLimiter
    private PartitionedRateLimiter<HttpContext> CreateEndpointLimiter()
    {
        // If we have a policy for this endpoint, use its partitioner. Else use a NoLimiter.
        return PartitionedRateLimiter.Create<HttpContext, DefaultKeyType>(context =>
        {
            DefaultRateLimiterPolicy? policy;
            var enableRateLimitingAttribute = context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>();
            if (enableRateLimitingAttribute is null)
            {
                return RateLimitPartition.GetNoLimiter<DefaultKeyType>(_defaultPolicyKey);
            }
            policy = enableRateLimitingAttribute.Policy;
            if (policy is not null)
            {
                return policy.GetPartition(context);
            }
            var name = enableRateLimitingAttribute.PolicyName;
            if (name is not null)
            {
                if (_policyMap.TryGetValue(name, out policy))
                {
                    return policy.GetPartition(context);
                }
                else
                {
                    throw new InvalidOperationException($"This endpoint requires a rate limiting policy with name {name}, but no such policy exists.");
                }
            }
            // Should be impossible for both name & policy to be null, but throw in that scenario just in case.
            else
            {
                throw new InvalidOperationException("This endpoint requested a rate limiting policy with a null name.");
            }
        }, new DefaultKeyTypeEqualityComparer());
    }

    private static partial class RateLimiterLog
    {
        [LoggerMessage(1, LogLevel.Debug, "Rate limits exceeded, rejecting this request.", EventName = "RequestRejectedLimitsExceeded")]
        internal static partial void RequestRejectedLimitsExceeded(ILogger logger);

        [LoggerMessage(2, LogLevel.Debug, "This endpoint requires a rate limiting policy with name {PolicyName}, but no such policy exists.", EventName = "WarnMissingPolicy")]
        internal static partial void WarnMissingPolicy(ILogger logger, string policyName);

        [LoggerMessage(3, LogLevel.Debug, "The request was canceled.", EventName = "RequestCanceled")]
        internal static partial void RequestCanceled(ILogger logger);
    }
}
