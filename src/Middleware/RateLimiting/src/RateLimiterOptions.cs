// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Specifies options for the rate limiting middleware.
/// </summary>
public sealed class RateLimiterOptions
{
    internal IDictionary<string, AspNetPolicy> PolicyMap { get; }
        = new Dictionary<string, AspNetPolicy>(StringComparer.Ordinal);

    internal IDictionary<string, PolicyTypeInfo> UnactivatedPolicyMap { get; }
        = new Dictionary<string, PolicyTypeInfo> (StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the <see cref="PartitionedRateLimiter{TResource}"/>
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
    /// Adds a new rate limiting policy with the given name.
    /// </summary>
    /// <param name="policyName">The name to be associated with the given <see cref="RateLimiter"/>.</param>
    /// <param name="partitioner">Method called every time an Acquire or WaitAsync call is made to figure out what rate limiter to apply to the request.</param>
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

        if (PolicyMap.ContainsKey(policyName) || UnactivatedPolicyMap.ContainsKey(policyName))
        {
            throw new ArgumentException("There already exists a policy with the name {name}");
        }

        PolicyMap.Add(policyName, new AspNetPolicy(ConvertPartitioner<TPartitionKey>(partitioner)));

        return this;
    }

    /// <summary>
    /// Adds a new rate limiting policy with the given name.
    /// </summary>
    /// <param name="policyName">The name to be associated with the given TPolicy.</param>
    public RateLimiterOptions AddPolicy<TPartitionKey, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPolicy>(string policyName) where TPolicy : IRateLimiterPolicy<TPartitionKey>
    {
        if (policyName == null)
        {
            throw new ArgumentNullException(nameof(policyName));
        }

        if (PolicyMap.ContainsKey(policyName) || UnactivatedPolicyMap.ContainsKey(policyName))
        {
            throw new ArgumentException("There already exists a policy with the name {name}");
        }

        UnactivatedPolicyMap.Add(policyName, new PolicyTypeInfo { PolicyType = typeof(TPolicy), PartitionKeyType = typeof(TPartitionKey) });

        return this;
    }

    /// <summary>
    /// Adds a new rate limiting policy with the given name.
    /// </summary>
    /// <param name="policyName">The name to be associated with the given <see cref="IRateLimiterPolicy{TPartitionKey}"/>.</param>
    /// <param name="policy">The <see cref="IRateLimiterPolicy{TPartitionKey}"/> to be applied.</param>
    public RateLimiterOptions AddPolicy<TPartitionKey>(string policyName, IRateLimiterPolicy<TPartitionKey> policy)
    {
        if (policyName == null)
        {
            throw new ArgumentNullException(nameof(policyName));
        }

        if (PolicyMap.ContainsKey(policyName) || UnactivatedPolicyMap.ContainsKey(policyName))
        {
            throw new ArgumentException("There already exists a policy with the name {name}");
        }

        if (policy == null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        PolicyMap.Add(policyName, new AspNetPolicy(ConvertPartitioner<TPartitionKey>(policy.GetPartition)));

        return this;
    }

    internal RateLimiterOptions InternalAddPolicy(string policyName, Func<HttpContext, RateLimitPartition<AspNetKey>> partitioner)
    {
        if (policyName == null)
        {
            throw new ArgumentNullException(nameof(policyName));
        }

        if (partitioner == null)
        {
            throw new ArgumentNullException(nameof(partitioner));
        }

        if (PolicyMap.ContainsKey(policyName) || UnactivatedPolicyMap.ContainsKey(policyName))
        {
            throw new ArgumentException("There already exists a policy with the name {name}");
        }

        PolicyMap.Add(policyName, new AspNetPolicy(partitioner));

        return this;
    }

    private static Func<HttpContext, RateLimitPartition<AspNetKey>> ConvertPartitioner<TPartitionKey>(Func<HttpContext, RateLimitPartition<TPartitionKey>> partitioner)
    {
        return (context =>
        {
            RateLimitPartition<TPartitionKey> partition = partitioner(context);
            return new RateLimitPartition<AspNetKey>(new AspNetKey<TPartitionKey>(partition.PartitionKey), key => partition.Factory(partition.PartitionKey));
        });
    }

    internal static Func<HttpContext, RateLimitPartition<AspNetKey>> ConvertPolicyObject<TPartitionKey>(object policy)
    {
        if (!(policy is IRateLimiterPolicy<TPartitionKey>))
        {
            throw new ArgumentException("Invalid policy passed");
        }
        return ConvertPartitioner<TPartitionKey>(((IRateLimiterPolicy<TPartitionKey>)policy).GetPartition);
    }
}
