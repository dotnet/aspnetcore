// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting.Policies;

namespace Microsoft.AspNetCore.RateLimiting;

public class RateLimitingOptions
{
    private string _defaultPolicyName = "__DefaultRateLimitingPolicy";
    internal IDictionary<string, RateLimitingPolicy> PolicyMap { get; }
        = new Dictionary<string, RateLimitingPolicy>(StringComparer.Ordinal);

    public RateLimitingOptions AddLimiter(string name, RateLimiter limiter)
    {
        PolicyMap[name] = new RateLimitingPolicy(limiter);
        return this;
    }

    /*
    public void AddLimiter<T>(string name, RateLimiter<T> limiter) where T : HttpContext
    {
        PolicyMap[name] = new RateLimitingPolicy(limiter);
    }
    */

    /// <summary>
    /// Gets the policy based on the <paramref name="name"/>
    /// </summary>
    /// <param name="name">The name of the policy to lookup.</param>
    /// <returns>The <see cref="CorsPolicy"/> if the policy was added.<c>null</c> otherwise.</returns>
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
