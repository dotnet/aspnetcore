// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    private readonly Func<OnRejectedContext, CancellationToken, ValueTask>? _onRejected;
    private readonly ILogger _logger;
    private readonly PartitionedRateLimiter<HttpContext> _limiter;
    private readonly int _rejectionStatusCode;
    private readonly IDictionary<string, AspNetPolicy> _policyMap;
    private readonly AspNetKey _defaultPolicyKey = new AspNetKey<PolicyNameKey>(new PolicyNameKey { PolicyName = "__defaultPolicyKey" });

    /// <summary>
    /// Creates a new <see cref="RateLimitingMiddleware"/>.
    /// </summary>
    /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
    /// <param name="logger">The <see cref="ILogger"/> used for logging.</param>
    /// <param name="options">The options for the middleware.</param>
    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, IOptions<RateLimiterOptions> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _onRejected = options.Value.OnRejected;
        _rejectionStatusCode = options.Value.RejectionStatusCode;
        _policyMap = options.Value.PolicyMap;

        var _globalLimiter = options.Value.GlobalLimiter;

    }

    // TODO - EventSource?
    /// <summary>
    /// Invokes the logic of the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>A <see cref="Task"/> that completes when the request leaves.</returns>
    public async Task Invoke(HttpContext context)
    {
        using var lease = await TryAcquireAsync(context);
        if (lease.IsAcquired)
        {
            await _next(context);
        }
        else
        {
            RateLimiterLog.RequestRejectedLimitsExceeded(_logger);
            // OnRejected "wins" over DefaultRejectionStatusCode - we set DefaultRejectionStatusCode first,
            // then call OnRejected in case it wants to do any further modification of the status code.
            context.Response.StatusCode = _rejectionStatusCode;
            await _onRejected(new OnRejectedContext() { HttpContext = context, Lease = lease }, context.RequestAborted);
        }
    }

    private ValueTask<RateLimitLease> TryAcquireAsync(HttpContext context)
    {
        var lease = _limiter.Acquire(context);
        if (lease.IsAcquired)
        {
            return ValueTask.FromResult(lease);
        }

        return _limiter.WaitAsync(context, cancellationToken: context.RequestAborted);
    }

    // Create the endpoint-specific PartitionedRateLimiter
    private PartitionedRateLimiter<HttpContext> CreateEndpointLimiter()
    {
        // If we have a policy for this endpoint, use its partitioner. Else use a NoLimiter.
        return PartitionedRateLimiter.Create<HttpContext, AspNetKey>(context =>
        {
            AspNetPolicy? policy;
            var name = context.GetEndpoint()?.Metadata.GetMetadata<IRateLimiterMetadata>()?.Name;
            if (!(name is null))
            {
                if (_policyMap.TryGetValue(name, out policy))
                {
                    return policy.GetPartition(context);
                }
            }
            return RateLimitPartition.CreateNoLimiter<AspNetKey>(_defaultPolicyKey);
        });
    }

    private static partial class RateLimiterLog
    {
        [LoggerMessage(1, LogLevel.Debug, "Rate limits exceeded, rejecting this request.", EventName = "RequestRejectedLimitsExceeded")]
        internal static partial void RequestRejectedLimitsExceeded(ILogger logger);
    }
}
