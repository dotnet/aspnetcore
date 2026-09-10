// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// Provides programmatic configuration used by <see cref="IAuthorizationService"/> and <see cref="IAuthorizationPolicyProvider"/>.
/// </summary>
public class AuthorizationOptions
{
    private static readonly Task<AuthorizationPolicy?> _nullPolicyTask = Task.FromResult<AuthorizationPolicy?>(null);

    private Dictionary<string, Task<AuthorizationPolicy?>> PolicyMap { get; } = new Dictionary<string, Task<AuthorizationPolicy?>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Determines whether authorization handlers should be invoked after <see cref="AuthorizationHandlerContext.HasFailed"/>.
    /// Defaults to true.
    /// </summary>
    public bool InvokeHandlersAfterFailure { get; set; } = true;

    /// <summary>
    /// Gets or sets the default authorization policy. Defaults to requiring authenticated users.
    /// </summary>
    /// <remarks>
    /// - The `DefaultPolicy` applies whenever authorization is required, but no specific policy is set.
    /// - If an `[Authorize]` attribute is present without a policy name, the `DefaultPolicy` is used instead of the `FallbackPolicy`.
    /// - This behavior ensures that endpoints explicitly requesting authorization (via `[Authorize]` or `RequireAuthorization()`) default to a secure policy.
    /// - When non-default behavior is needed, developers should define named policies.
    /// </remarks>
    public AuthorizationPolicy DefaultPolicy { get; set; } = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

    /// <summary>
    /// Gets or sets the fallback authorization policy used by <see cref="AuthorizationPolicy.CombineAsync(IAuthorizationPolicyProvider, IEnumerable{IAuthorizeData})"/>
    /// when no authorization metadata (e.g., `[Authorize]` attribute, `RequireAuthorization()`) is explicitly provided for a resource.
    /// </summary>
    /// <remarks>
    /// - The `FallbackPolicy` only applies when there are no authorization attributes or explicit policies set.
    /// - If a resource has an `[Authorize]` attribute (even without a policy name), the `DefaultPolicy` is used instead of the `FallbackPolicy`.
    /// - This means `FallbackPolicy` is mainly relevant for middleware-based authorization flows where no per-endpoint authorization is specified.
    /// - By default, `FallbackPolicy` is `null`, meaning it has no effect unless explicitly set.
    /// </remarks>
    public AuthorizationPolicy? FallbackPolicy { get; set; }

    /// <summary>
    /// Add an authorization policy with the provided name.
    /// </summary>
    /// <param name="name">The name of the policy.</param>
    /// <param name="policy">The authorization policy.</param>
    public void AddPolicy(string name, AuthorizationPolicy policy)
    {
        ArgumentNullThrowHelper.ThrowIfNull(name);
        ArgumentNullThrowHelper.ThrowIfNull(policy);

        PolicyMap[name] = Task.FromResult<AuthorizationPolicy?>(policy);
    }

    /// <summary>
    /// Add a policy that is built from a delegate with the provided name.
    /// </summary>
    /// <param name="name">The name of the policy.</param>
    /// <param name="configurePolicy">The delegate that will be used to build the policy.</param>
    public void AddPolicy(string name, Action<AuthorizationPolicyBuilder> configurePolicy)
    {
        ArgumentNullThrowHelper.ThrowIfNull(name);
        ArgumentNullThrowHelper.ThrowIfNull(configurePolicy);

        var policyBuilder = new AuthorizationPolicyBuilder();
        configurePolicy(policyBuilder);
        PolicyMap[name] = Task.FromResult<AuthorizationPolicy?>(policyBuilder.Build());
    }

    /// <summary>
    /// Returns the policy for the specified name, or null if a policy with the name does not exist.
    /// </summary>
    /// <param name="name">The name of the policy to return.</param>
    /// <returns>The policy for the specified name, or null if a policy with the name does not exist.</returns>
    public AuthorizationPolicy? GetPolicy(string name)
    {
        ArgumentNullThrowHelper.ThrowIfNull(name);

        if (PolicyMap.TryGetValue(name, out var value))
        {
            return value.Result;
        }

        return null;
    }

    internal Task<AuthorizationPolicy?> GetPolicyTask(string name)
    {
        ArgumentNullThrowHelper.ThrowIfNull(name);

        if (PolicyMap.TryGetValue(name, out var value))
        {
            return value;
        }

        return _nullPolicyTask;
    }
}
