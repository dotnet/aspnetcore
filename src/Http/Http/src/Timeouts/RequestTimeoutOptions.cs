// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Timeouts;

/// <summary>
/// Specifies options for the request timeouts middleware.
/// </summary>
public sealed class RequestTimeoutOptions
{
    /// <summary>
    /// Applied to any request without a policy set via endpoint metadata. No value by default.
    /// </summary>
    public RequestTimeoutPolicy? DefaultPolicy { get; set; }

    /// <summary>
    /// Dictionary of policies that would be applied per endpoint.
    /// Policy names are case-insensitive.
    /// </summary>
    public IDictionary<string, RequestTimeoutPolicy> Policies { get; } = new Dictionary<string, RequestTimeoutPolicy>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Adds a new policy.
    /// </summary>
    /// <param name="policyName">The name of the policy (case-insensitive).</param>
    /// <param name="timeout">The timeout to apply for this policy.</param>
    public RequestTimeoutOptions AddPolicy(string policyName, TimeSpan timeout)
    {
        return AddPolicy(policyName, new RequestTimeoutPolicy
        {
            Timeout = timeout
        });
    }

    /// <summary>
    /// Adds a new policy.
    /// </summary>
    /// <param name="policyName">The name of the policy (case-insensitive).</param>
    /// <param name="policy">The <see cref="RequestTimeoutPolicy"/> policy to be added.</param>
    public RequestTimeoutOptions AddPolicy(string policyName, RequestTimeoutPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentException.ThrowIfNullOrEmpty(policyName);

        Policies[policyName] = policy;
        return this;
    }
}
