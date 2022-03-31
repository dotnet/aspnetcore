// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting.Policies;

namespace Microsoft.AspNetCore.RateLimiting;

public class RateLimitingOptions
{
    private readonly string _defaultLimitingPolicyName = "__DefaultRateLimitingPolicy";
    private IDictionary<string, RateLimitingPolicy> PolicyMap { get; }
        = new Dictionary<string, RateLimitingPolicy>(StringComparer.Ordinal);
    private PartitionedRateLimiter<HttpContext>? _limiter;

    /// <summary>
    /// Gets the <see cref="PartitionedRateLimiter{TResource}"/>
    /// </summary>
    public PartitionedRateLimiter<HttpContext>? Limiter
    {
        get => _limiter;
    }

    /// <summary>
    /// Adds a new rate limiter and sets it as the default.
    /// </summary>
    /// <param name="limiter">The <see cref="RateLimiter"/> to be added.</param>
    public RateLimitingOptions AddDefaultLimiter(RateLimiter limiter)
    {
        // Provide a better duplicate-name error message for the default policy
        if (PolicyMap.ContainsKey(_defaultLimitingPolicyName))
        {
            throw new ArgumentException("Default policy is already set.");
        }
        AddLimiter(_defaultLimitingPolicyName, limiter);
        return this;
    }

    /// <summary>
    /// Adds a new rate limiter with the given name.
    /// </summary>
    /// <param name="name">The name to be associated with the given <see cref="RateLimiter"/></param>
    /// <param name="limiter">The <see cref="RateLimiter"/> to be added.</param>
    public RateLimitingOptions AddLimiter(string name, RateLimiter limiter)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (limiter == null)
        {
            throw new ArgumentNullException(nameof(limiter));
        }

        if (PolicyMap.ContainsKey(name))
        {
            throw new ArgumentException("There already exists a policy with the name {name}");
        }
        
        PolicyMap[name] = new RateLimitingPolicy(limiter);
        return this;
    }

    /// <summary>
    /// Adds a new rate limiter.
    /// </summary>
    /// <param name="limiter">The <see cref="PartitionedRateLimiter{TResource}"/> to be added.</param>
    public void AddLimiter<HttpContext>(PartitionedRateLimiter<Http.HttpContext> limiter)
    {
        if (limiter == null)
        {
            throw new ArgumentNullException(nameof(limiter));
        }
        _limiter = limiter;
    }

    /// <summary>
    /// A <see cref="RequestDelegate"/> that handles requests rejected by this middleware.
    /// If it doesn't modify the response, an empty 503 response will be written.
    /// </summary>
    public RequestDelegate OnRejected { get; set; } = context =>
    {
        return Task.CompletedTask;
    };

    /// <summary>
    /// Gets the policy based on the <paramref name="name"/>
    /// </summary>
    /// <param name="name">The name of the policy to lookup.</param>
    /// <returns>The <see cref="RateLimitingPolicy"/> if the policy was added.<c>null</c> otherwise.</returns>
    internal RateLimitingPolicy? GetPolicy(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (PolicyMap.TryGetValue(name, out var result))
        {
            return result;
        }

        return null;
    }
}
