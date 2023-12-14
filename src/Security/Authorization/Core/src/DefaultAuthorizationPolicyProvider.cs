// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// The default implementation of a policy provider,
/// which provides a <see cref="AuthorizationPolicy"/> for a particular name.
/// </summary>
public class DefaultAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly AuthorizationOptions _options;
    private Task<AuthorizationPolicy>? _cachedDefaultPolicy;
    private Task<AuthorizationPolicy?>? _cachedFallbackPolicy;

    /// <summary>
    /// Creates a new instance of <see cref="DefaultAuthorizationPolicyProvider"/>.
    /// </summary>
    /// <param name="options">The options used to configure this instance.</param>
    public DefaultAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        ArgumentNullThrowHelper.ThrowIfNull(options);

        _options = options.Value;
    }

    /// <summary>
    /// Gets the default authorization policy.
    /// </summary>
    /// <returns>The default authorization policy.</returns>
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        if (_cachedDefaultPolicy == null || _cachedDefaultPolicy.Result != _options.DefaultPolicy)
        {
            _cachedDefaultPolicy = Task.FromResult(_options.DefaultPolicy);
        }

        return _cachedDefaultPolicy;
    }

    /// <summary>
    /// Gets the fallback authorization policy.
    /// </summary>
    /// <returns>The fallback authorization policy.</returns>
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        if (_cachedFallbackPolicy == null || _cachedFallbackPolicy.Result != _options.FallbackPolicy)
        {
            _cachedFallbackPolicy = Task.FromResult(_options.FallbackPolicy);
        }

        return _cachedFallbackPolicy;
    }

    /// <summary>
    /// Gets a <see cref="AuthorizationPolicy"/> from the given <paramref name="policyName"/>
    /// </summary>
    /// <param name="policyName">The policy name to retrieve.</param>
    /// <returns>The named <see cref="AuthorizationPolicy"/>.</returns>
    public virtual Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // MVC caches policies specifically for this class, so this method MUST return the same policy per
        // policyName for every request or it could allow undesired access. It also must return synchronously.
        // A change to either of these behaviors would require shipping a patch of MVC as well.
        return _options.GetPolicyTask(policyName);
    }

#if NETCOREAPP
    /// <summary>
    /// Determines if policies from this provider can be cached, which is true only for this type.
    /// </summary>
    public virtual bool AllowsCachingPolicies => GetType() == typeof(DefaultAuthorizationPolicyProvider);
#endif
}
