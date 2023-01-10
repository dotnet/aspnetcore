// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// Represents a collection of authorization requirements and the scheme or
/// schemes they are evaluated against, all of which must succeed
/// for authorization to succeed.
/// </summary>
public class AuthorizationPolicy
{
    /// <summary>
    /// Creates a new instance of <see cref="AuthorizationPolicy"/>.
    /// </summary>
    /// <param name="requirements">
    /// The list of <see cref="IAuthorizationRequirement"/>s which must succeed for
    /// this policy to be successful.
    /// </param>
    /// <param name="authenticationSchemes">
    /// The authentication schemes the <paramref name="requirements"/> are evaluated against.
    /// </param>
    public AuthorizationPolicy(IEnumerable<IAuthorizationRequirement> requirements, IEnumerable<string> authenticationSchemes)
    {
        ArgumentNullThrowHelper.ThrowIfNull(requirements);
        ArgumentNullThrowHelper.ThrowIfNull(authenticationSchemes);

        if (!requirements.Any())
        {
            throw new InvalidOperationException(Resources.Exception_AuthorizationPolicyEmpty);
        }
        Requirements = new List<IAuthorizationRequirement>(requirements).AsReadOnly();
        AuthenticationSchemes = new List<string>(authenticationSchemes).AsReadOnly();
    }

    /// <summary>
    /// Gets a readonly list of <see cref="IAuthorizationRequirement"/>s which must succeed for
    /// this policy to be successful.
    /// </summary>
    public IReadOnlyList<IAuthorizationRequirement> Requirements { get; }

    /// <summary>
    /// Gets a readonly list of the authentication schemes the <see cref="AuthorizationPolicy.Requirements"/>
    /// are evaluated against.
    /// </summary>
    public IReadOnlyList<string> AuthenticationSchemes { get; }

    /// <summary>
    /// Combines the specified <see cref="AuthorizationPolicy"/> into a single policy.
    /// </summary>
    /// <param name="policies">The authorization policies to combine.</param>
    /// <returns>
    /// A new <see cref="AuthorizationPolicy"/> which represents the combination of the
    /// specified <paramref name="policies"/>.
    /// </returns>
    public static AuthorizationPolicy Combine(params AuthorizationPolicy[] policies)
    {
        ArgumentNullThrowHelper.ThrowIfNull(policies);

        return Combine((IEnumerable<AuthorizationPolicy>)policies);
    }

    /// <summary>
    /// Combines the specified <see cref="AuthorizationPolicy"/> into a single policy.
    /// </summary>
    /// <param name="policies">The authorization policies to combine.</param>
    /// <returns>
    /// A new <see cref="AuthorizationPolicy"/> which represents the combination of the
    /// specified <paramref name="policies"/>.
    /// </returns>
    public static AuthorizationPolicy Combine(IEnumerable<AuthorizationPolicy> policies)
    {
        ArgumentNullThrowHelper.ThrowIfNull(policies);

        var builder = new AuthorizationPolicyBuilder();
        foreach (var policy in policies)
        {
            builder.Combine(policy);
        }
        return builder.Build();
    }

    /// <summary>
    /// Combines the <see cref="AuthorizationPolicy"/> provided by the specified
    /// <paramref name="policyProvider"/>.
    /// </summary>
    /// <param name="policyProvider">A <see cref="IAuthorizationPolicyProvider"/> which provides the policies to combine.</param>
    /// <param name="authorizeData">A collection of authorization data used to apply authorization to a resource.</param>
    /// <returns>
    /// A new <see cref="AuthorizationPolicy"/> which represents the combination of the
    /// authorization policies provided by the specified <paramref name="policyProvider"/>.
    /// </returns>
    public static Task<AuthorizationPolicy?> CombineAsync(IAuthorizationPolicyProvider policyProvider,
        IEnumerable<IAuthorizeData> authorizeData) => CombineAsync(policyProvider, authorizeData,
            Enumerable.Empty<AuthorizationPolicy>());

    /// <summary>
    /// Combines the <see cref="AuthorizationPolicy"/> provided by the specified
    /// <paramref name="policyProvider"/>.
    /// </summary>
    /// <param name="policyProvider">A <see cref="IAuthorizationPolicyProvider"/> which provides the policies to combine.</param>
    /// <param name="authorizeData">A collection of authorization data used to apply authorization to a resource.</param>
    /// <param name="policies">A collection of <see cref="AuthorizationPolicy"/> policies to combine.</param>
    /// <returns>
    /// A new <see cref="AuthorizationPolicy"/> which represents the combination of the
    /// authorization policies provided by the specified <paramref name="policyProvider"/>.
    /// </returns>
    public static async Task<AuthorizationPolicy?> CombineAsync(IAuthorizationPolicyProvider policyProvider,
        IEnumerable<IAuthorizeData> authorizeData,
        IEnumerable<AuthorizationPolicy> policies)
    {
        ArgumentNullThrowHelper.ThrowIfNull(policyProvider);
        ArgumentNullThrowHelper.ThrowIfNull(authorizeData);

        var anyPolicies = policies.Any();

        // Avoid allocating enumerator if the data is known to be empty
        var skipEnumeratingData = false;
        if (authorizeData is IList<IAuthorizeData> dataList)
        {
            skipEnumeratingData = dataList.Count == 0;
        }

        AuthorizationPolicyBuilder? policyBuilder = null;
        if (!skipEnumeratingData)
        {
            foreach (var authorizeDatum in authorizeData)
            {
                if (policyBuilder == null)
                {
                    policyBuilder = new AuthorizationPolicyBuilder();
                }

                var useDefaultPolicy = !(anyPolicies);
                if (!string.IsNullOrWhiteSpace(authorizeDatum.Policy))
                {
                    var policy = await policyProvider.GetPolicyAsync(authorizeDatum.Policy).ConfigureAwait(false);
                    if (policy == null)
                    {
                        throw new InvalidOperationException(Resources.FormatException_AuthorizationPolicyNotFound(authorizeDatum.Policy));
                    }
                    policyBuilder.Combine(policy);
                    useDefaultPolicy = false;
                }

                var rolesSplit = authorizeDatum.Roles?.Split(',');
                if (rolesSplit?.Length > 0)
                {
                    var trimmedRolesSplit = rolesSplit.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim());
                    policyBuilder.RequireRole(trimmedRolesSplit);
                    useDefaultPolicy = false;
                }

                var authTypesSplit = authorizeDatum.AuthenticationSchemes?.Split(',');
                if (authTypesSplit?.Length > 0)
                {
                    foreach (var authType in authTypesSplit)
                    {
                        if (!string.IsNullOrWhiteSpace(authType))
                        {
                            policyBuilder.AuthenticationSchemes.Add(authType.Trim());
                        }
                    }
                }

                if (useDefaultPolicy)
                {
                    policyBuilder.Combine(await policyProvider.GetDefaultPolicyAsync().ConfigureAwait(false));
                }
            }
        }

        if (anyPolicies)
        {
            policyBuilder ??= new();

            foreach (var policy in policies)
            {
                policyBuilder.Combine(policy);
            }
        }

        // If we have no policy by now, use the fallback policy if we have one
        if (policyBuilder == null)
        {
            var fallbackPolicy = await policyProvider.GetFallbackPolicyAsync().ConfigureAwait(false);
            if (fallbackPolicy != null)
            {
                return fallbackPolicy;
            }
        }

        return policyBuilder?.Build();
    }
}
