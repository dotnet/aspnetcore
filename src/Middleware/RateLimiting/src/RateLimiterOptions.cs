// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Specifies options for the rate limiting middleware.
/// </summary>
public sealed class RateLimiterOptions
{
    private PartitionedRateLimiter<HttpContext> _limiter = new NoLimiter<HttpContext>();
    private Func<OnRejectedContext, CancellationToken, ValueTask> _onRejected = (context, token) =>
    {
        return ValueTask.CompletedTask;
    };
    private IDictionary<string, RateLimiterPolicy> PartitionMap { get; }
        = new Dictionary<string, RateLimiterPolicy>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the <see cref="PartitionedRateLimiter{TResource}"/>
    /// </summary>
    public PartitionedRateLimiter<HttpContext>? GlobalLimiter
    {
        get => _limiter;
        set => _limiter = value;
    }

    /// <summary>
    /// Gets or sets a <see cref="Func{OnRejectedContext, CancellationToken, ValueTask}"/> that handles requests rejected by this middleware.
    /// </summary>
    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected
    {
        get => _onRejected;
        set => _onRejected = value;
    }

    /// <summary>
    /// Gets or sets the default status code to set on the response when a request is rejected.
    /// Defaults to <see cref="StatusCodes.Status503ServiceUnavailable"/>.
    /// </summary>
    /// <remarks>
    /// This status code will be set before <see cref="OnRejected"/> is called, so any status code set by
    /// <see cref="OnRejected"/> will "win" over this default.
    /// </remarks>
    public int RejectionStatusCode { get; set; } = StatusCodes.Status503ServiceUnavailable;

    /// <summary>
    /// Adds a new rate limiting policy with the given name.
    /// </summary>
    /// <param name="name">The name to be associated with the given <see cref="RateLimiter"/></param>
    /// <param name="partitioner">Method called every time an Acquire or WaitAsync call is made to figure out what rate limiter to apply to the request.</param>
    /// <param name="global">Determines if this policy should be shared across endpoints. Defaults to false.</param>
    public RateLimiterOptions AddPolicy<TPartitionKey>(string policyName, Func<HttpContext, RateLimitPartition<TPartitionKey>> partitioner)
    {
        if (policyName == null)
        {
            throw new ArgumentNullException(nameof(policyName));
        }

        if (partitioner == null)
        {
            throw new ArgumentNullException(nameof(partitioner));
        }

        if (PartitionMap.ContainsKey(policyName))
        {
            throw new ArgumentException("There already exists a partition with the name {name}");
        }

        PartitionMap.Add(policyName, partitioner);

        return this;

        Func<HttpContext, RateLimitPartition<AspNetKey>> func = context =>
        {
            RateLimitPartition<TPartitionKey> partition = partitioner(context);
            return new RateLimitPartition<AspNetKey<TPartitionKey>>(new AspNetKey<TPartitionKey>(partition.PartitionKey), partition.Factory(partition.PartitionKey));
        };
    }

    public RateLimiterOptions AddPolicy<TPartitionKey, TPolicy>(string policyName) where TPolicy : IRateLimiterPolicy<TPartitionKey>
    {
        return this;
    }

    public RateLimiterOptions AddPolicy<TPartitionKey>(string policyName, IRateLimiterPolicy<TPartitionKey> policy)
    {
        return this;
    }
}
