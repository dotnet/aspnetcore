// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Specifies options for the rate limiting middleware.
/// </summary>
public sealed class RateLimiterOptions
{
    internal IDictionary<string, DefaultRateLimiterPolicy> PolicyMap { get; }
        = new Dictionary<string, DefaultRateLimiterPolicy>(StringComparer.Ordinal);

    internal IDictionary<string, Func<IServiceProvider, DefaultRateLimiterPolicy>> UnactivatedPolicyMap { get; }
        = new Dictionary<string, Func<IServiceProvider, DefaultRateLimiterPolicy>>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the global <see cref="PartitionedRateLimiter{HttpContext}"/> that will be applied on all requests.
    /// The global limiter will be executed first, followed by the endpoint-specific limiter, if one exists.
    /// </summary>
    public PartitionedRateLimiter<HttpContext>? GlobalLimiter { get; set; }

    /// <summary>
    /// Gets or sets a <see cref="Func{OnRejectedContext, CancellationToken, ValueTask}"/> that handles requests rejected by this middleware.
    /// </summary>
    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected { get; set; }

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
    /// Adds a new rate limiting policy with the given policyName.
    /// </summary>
    /// <param name="policyName">The name to be associated with the given <see cref="RateLimiter"/>.</param>
    /// <param name="partitioner">Method called every time an Acquire or WaitAsync call is made to figure out what rate limiter to apply to the request.</param>
    public RateLimiterOptions AddPolicy<TPartitionKey>(string policyName, Func<HttpContext, RateLimitPartition<TPartitionKey>> partitioner)
    {
        ArgumentNullException.ThrowIfNull(policyName);
        ArgumentNullException.ThrowIfNull(partitioner);

        if (PolicyMap.ContainsKey(policyName) || UnactivatedPolicyMap.ContainsKey(policyName))
        {
            throw new ArgumentException($"There already exists a policy with the name {policyName}");
        }

        PolicyMap.Add(policyName, new DefaultRateLimiterPolicy(ConvertPartitioner<TPartitionKey>(partitioner), null));

        return this;
    }

    /// <summary>
    /// Adds a new rate limiting policy with the given policyName.
    /// </summary>
    /// <param name="policyName">The name to be associated with the given TPolicy.</param>
    public RateLimiterOptions AddPolicy<TPartitionKey, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPolicy>(string policyName) where TPolicy : IRateLimiterPolicy<TPartitionKey>
    {
        ArgumentNullException.ThrowIfNull(policyName);

        if (PolicyMap.ContainsKey(policyName) || UnactivatedPolicyMap.ContainsKey(policyName))
        {
            throw new ArgumentException($"There already exists a policy with the name {policyName}");
        }

        Func <IServiceProvider, DefaultRateLimiterPolicy> policyFunc = serviceProvider =>
        {
            var instance = (IRateLimiterPolicy<TPartitionKey>)ActivatorUtilities.CreateInstance<TPolicy>(serviceProvider);
            return new DefaultRateLimiterPolicy(ConvertPartitioner<TPartitionKey>(instance.GetPartition), instance.OnRejected);
        };

        UnactivatedPolicyMap.Add(policyName, policyFunc);

        return this;
    }

    /// <summary>
    /// Adds a new rate limiting policy with the given policyName.
    /// </summary>
    /// <param name="policyName">The name to be associated with the given <see cref="IRateLimiterPolicy{TPartitionKey}"/>.</param>
    /// <param name="policy">The <see cref="IRateLimiterPolicy{TPartitionKey}"/> to be applied.</param>
    public RateLimiterOptions AddPolicy<TPartitionKey>(string policyName, IRateLimiterPolicy<TPartitionKey> policy)
    {
        ArgumentNullException.ThrowIfNull(policyName);

        if (PolicyMap.ContainsKey(policyName) || UnactivatedPolicyMap.ContainsKey(policyName))
        {
            throw new ArgumentException($"There already exists a policy with the name {policyName}");
        }

        ArgumentNullException.ThrowIfNull(policy);

        PolicyMap.Add(policyName, new DefaultRateLimiterPolicy(ConvertPartitioner<TPartitionKey>(policy.GetPartition), policy.OnRejected));

        return this;
    }

    internal RateLimiterOptions InternalAddPolicy(string policyName, Func<HttpContext, RateLimitPartition<DefaultKeyType>> partitioner)
    {
        ArgumentNullException.ThrowIfNull(policyName);
        ArgumentNullException.ThrowIfNull(partitioner);

        if (PolicyMap.ContainsKey(policyName) || UnactivatedPolicyMap.ContainsKey(policyName))
        {
            throw new ArgumentException($"There already exists a policy with the name {policyName}");
        }

        PolicyMap.Add(policyName, new DefaultRateLimiterPolicy(partitioner, null));

        return this;
    }

    // Converts a Partition<TKey> to a Partition<DefaultKeyType<TKey>> to prevent accidental collisions with the keys we create in the the RateLimiterOptionsExtensions.
    private static Func<HttpContext, RateLimitPartition<DefaultKeyType>> ConvertPartitioner<TPartitionKey>(Func<HttpContext, RateLimitPartition<TPartitionKey>> partitioner)
    {
        return (context =>
        {
            RateLimitPartition<TPartitionKey> partition = partitioner(context);
            return new RateLimitPartition<DefaultKeyType>(new DefaultKeyType<TPartitionKey>(partition.PartitionKey), key => partition.Factory(partition.PartitionKey));
        });
    }
}
