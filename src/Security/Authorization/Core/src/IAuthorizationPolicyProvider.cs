// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// A type which can provide a <see cref="AuthorizationPolicy"/> for a particular name.
/// </summary>
public interface IAuthorizationPolicyProvider
{
    /// <summary>
    /// Gets a <see cref="AuthorizationPolicy"/> from the given <paramref name="policyName"/>
    /// </summary>
    /// <param name="policyName">The policy name to retrieve.</param>
    /// <returns>The named <see cref="AuthorizationPolicy"/>.</returns>
    Task<AuthorizationPolicy?> GetPolicyAsync(string policyName);

    /// <summary>
    /// Gets the default authorization policy.
    /// </summary>
    /// <returns>The default authorization policy.</returns>
    Task<AuthorizationPolicy> GetDefaultPolicyAsync();

    /// <summary>
    /// Gets the fallback authorization policy.
    /// </summary>
    /// <returns>The fallback authorization policy.</returns>
    Task<AuthorizationPolicy?> GetFallbackPolicyAsync();

#if NETCOREAPP
    /// <summary>
    /// Determines if policies from this provider can be cached, defaults to false.
    /// </summary>
    bool AllowsCachingPolicies => false;
#endif
}
