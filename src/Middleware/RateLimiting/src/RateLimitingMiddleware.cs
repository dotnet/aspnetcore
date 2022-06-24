// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Limits the rate of requests allowed in the application, based on limits set by a user-provided <see cref="PartitionedRateLimiter{TResource}"/>.
/// </summary>
internal sealed partial class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Func<OnRejectedContext, CancellationToken, ValueTask>? _onRejected;
    private readonly ILogger _logger;
    private readonly PartitionedRateLimiter<HttpContext>? _globalLimiter;
    private readonly PartitionedRateLimiter<HttpContext> _endpointLimiter;
    private readonly int _rejectionStatusCode;
    private readonly IDictionary<string, DefaultRateLimiterPolicy> _policyMap;
    private readonly DefaultKeyType _defaultPolicyKey = new DefaultKeyType<PolicyNameKey>(new PolicyNameKey { PolicyName = "__defaultPolicyKey" });
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Creates a new <see cref="RateLimitingMiddleware"/>.
    /// </summary>
    /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
    /// <param name="logger">The <see cref="ILogger"/> used for logging.</param>
    /// <param name="options">The options for the middleware.</param>
    /// <param name="serviceProvider">The service provider for this middleware.</param>
    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, IOptions<RateLimiterOptions> options, IServiceProvider serviceProvider)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        _onRejected = options.Value.OnRejected;
        _rejectionStatusCode = options.Value.RejectionStatusCode;
        _policyMap = options.Value.PolicyMap;

        // Use reflection to activate policies passed to AddPolicy<TPartitionKey, TPolicy>
        var convertPolicyObject = typeof(RateLimiterOptions).GetMethod("ConvertPolicyObject");

        foreach (var policyTypeInfo in options.Value.UnactivatedPolicyMap)
        {
            var genericConvertPolicyObject = convertPolicyObject!.MakeGenericMethod(policyTypeInfo.Value.PartitionKeyType);
            var instance = ActivatorUtilities.CreateInstance(_serviceProvider, policyTypeInfo.Value.PolicyType);
            var obj = genericConvertPolicyObject.Invoke(new RateLimiterOptions(), new object[] { instance });
            var partitioner = (Func<HttpContext, RateLimitPartition<DefaultKeyType>>)obj!;
            _policyMap.Add(policyTypeInfo.Key, new DefaultRateLimiterPolicy(partitioner, ((IRateLimiterPolicy<object>)instance).OnRejected));
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
    public async Task Invoke(HttpContext context)
    {
        using var leaseContext = await TryAcquireAsync(context);
        if (leaseContext.Lease.IsAcquired)
        {
            await _next(context);
        }
        else
        {
            RateLimiterLog.RequestRejectedLimitsExceeded(_logger);
            // OnRejected "wins" over DefaultRejectionStatusCode - we set DefaultRejectionStatusCode first,
            // then call OnRejected in case it wants to do any further modification of the status code.
            context.Response.StatusCode = _rejectionStatusCode;

            // If the global limiter rejected this request, use OnRejected from the options if it exists.
            // Else the endpoint limiter rejected this request - use OnRejected from the endpoint policy if it exists,
            // else OnRejected from the options if it exists.
            if (leaseContext.Rejector.Equals(Rejector.Global))
            {
                if (_onRejected is not null)
                {
                    await _onRejected(new OnRejectedContext() { HttpContext = context, Lease = leaseContext.Lease }, context.RequestAborted);
                }
            }
            else
            {
                DefaultRateLimiterPolicy? policy;
                var name = context.GetEndpoint()?.Metadata.GetMetadata<IRateLimiterMetadata>()?.Name;
                // Use custom policy OnRejected if available, else use OnRejected from the Options if available.
                if (name is not null && _policyMap.TryGetValue(name, out policy) && policy.OnRejected is not null)
                {
                    await policy.OnRejected(new OnRejectedContext() { HttpContext = context, Lease = leaseContext.Lease }, context.RequestAborted);
                }
                else if (_onRejected is not null)
                {
                    await _onRejected(new OnRejectedContext() { HttpContext = context, Lease = leaseContext.Lease }, context.RequestAborted);
                }
            }
        }
    }

    private ValueTask<LeaseContext> TryAcquireAsync(HttpContext context)
    {
        var leaseContext = CombinedAcquire(context);
        if (leaseContext.Lease.IsAcquired)
        {
            return ValueTask.FromResult(leaseContext);
        }

        return CombinedWaitASync(context, context.RequestAborted);
    }

    private LeaseContext CombinedAcquire(HttpContext context)
    {
        RateLimitLease? globalLease = null;
        RateLimitLease? endpointLease = null;

        try
        {
            if (_globalLimiter is not null)
            {
                globalLease = _globalLimiter.Acquire(context);
                if (!globalLease.IsAcquired)
                {
                    return new LeaseContext() { Rejector = Rejector.Global, Lease = globalLease };
                }
            }
            endpointLease = _endpointLimiter.Acquire(context);
            if (!endpointLease.IsAcquired)
            {
                globalLease?.Dispose();
                return new LeaseContext() { Rejector = Rejector.Endpoint, Lease = endpointLease };
            }
        }
        catch (Exception)
        {
            endpointLease?.Dispose();
            globalLease?.Dispose();
            throw;
        }

        return new LeaseContext() { Lease = new DefaultCombinedLease(globalLease, endpointLease)};
    }

    private async ValueTask<LeaseContext> CombinedWaitASync(HttpContext context, CancellationToken cancellationToken)
    {
        RateLimitLease? globalLease = null;
        RateLimitLease? endpointLease = null;

        try
        {
            if (_globalLimiter is not null)
            {
                globalLease = await _globalLimiter.WaitAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (!globalLease.IsAcquired)
                {
                    return new LeaseContext() { Rejector = Rejector.Global, Lease = globalLease };
                }
            }
            endpointLease = await _endpointLimiter.WaitAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!endpointLease.IsAcquired)
            {
                globalLease?.Dispose();
                return new LeaseContext() { Rejector = Rejector.Endpoint, Lease = endpointLease };
            }
        }
        catch (Exception)
        {
            endpointLease?.Dispose();
            globalLease?.Dispose();
            throw;
        }

        return new LeaseContext() { Lease = new DefaultCombinedLease(globalLease, endpointLease) };
    }

    // Create the endpoint-specific PartitionedRateLimiter
    private PartitionedRateLimiter<HttpContext> CreateEndpointLimiter()
    {
        // If we have a policy for this endpoint, use its partitioner. Else use a NoLimiter.
        return PartitionedRateLimiter.Create<HttpContext, DefaultKeyType>(context =>
        {
            DefaultRateLimiterPolicy? policy;
            var name = context.GetEndpoint()?.Metadata.GetMetadata<IRateLimiterMetadata>()?.Name;
            if (name is not null)
            {
                if (_policyMap.TryGetValue(name, out policy))
                {
                    return policy.GetPartition(context);
                }
            }
            return RateLimitPartition.CreateNoLimiter<DefaultKeyType>(_defaultPolicyKey);
        }, new DefaultKeyTypeEqualityComparer());
    }

    private static partial class RateLimiterLog
    {
        [LoggerMessage(1, LogLevel.Debug, "Rate limits exceeded, rejecting this request.", EventName = "RequestRejectedLimitsExceeded")]
        internal static partial void RequestRejectedLimitsExceeded(ILogger logger);
    }
}

internal enum Rejector
{
    Global,
    Endpoint
}
