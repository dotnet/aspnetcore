// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// Extension methods for <see cref="IAuthorizationService"/>.
/// </summary>
public static class AuthorizationServiceExtensions
{
    /// <summary>
    /// Checks if a user meets a specific requirement for the specified resource
    /// </summary>
    /// <param name="service">The <see cref="IAuthorizationService"/> providing authorization.</param>
    /// <param name="user">The user to evaluate the policy against.</param>
    /// <param name="resource">The resource to evaluate the policy against.</param>
    /// <param name="requirement">The requirement to evaluate the policy against.</param>
    /// <returns>
    /// A flag indicating whether requirement evaluation has succeeded or failed.
    /// This value is <c>true</c> when the user fulfills the policy, otherwise <c>false</c>.
    /// </returns>
    public static Task<AuthorizationResult> AuthorizeAsync(this IAuthorizationService service, ClaimsPrincipal user, object? resource, IAuthorizationRequirement requirement)
    {
        ArgumentNullThrowHelper.ThrowIfNull(service);
        ArgumentNullThrowHelper.ThrowIfNull(requirement);

        return service.AuthorizeAsync(user, resource, new IAuthorizationRequirement[] { requirement });
    }

    /// <summary>
    /// Checks if a user meets a specific authorization policy against the specified resource.
    /// </summary>
    /// <param name="service">The <see cref="IAuthorizationService"/> providing authorization.</param>
    /// <param name="user">The user to evaluate the policy against.</param>
    /// <param name="resource">The resource to evaluate the policy against.</param>
    /// <param name="policy">The policy to evaluate.</param>
    /// <returns>
    /// A flag indicating whether policy evaluation has succeeded or failed.
    /// This value is <c>true</c> when the user fulfills the policy, otherwise <c>false</c>.
    /// </returns>
    public static Task<AuthorizationResult> AuthorizeAsync(this IAuthorizationService service, ClaimsPrincipal user, object? resource, AuthorizationPolicy policy)
    {
        ArgumentNullThrowHelper.ThrowIfNull(service);
        ArgumentNullThrowHelper.ThrowIfNull(policy);

        return service.AuthorizeAsync(user, resource, policy.Requirements);
    }

    /// <summary>
    /// Checks if a user meets a specific authorization policy against the specified resource.
    /// </summary>
    /// <param name="service">The <see cref="IAuthorizationService"/> providing authorization.</param>
    /// <param name="user">The user to evaluate the policy against.</param>
    /// <param name="policy">The policy to evaluate.</param>
    /// <returns>
    /// A flag indicating whether policy evaluation has succeeded or failed.
    /// This value is <c>true</c> when the user fulfills the policy, otherwise <c>false</c>.
    /// </returns>
    public static Task<AuthorizationResult> AuthorizeAsync(this IAuthorizationService service, ClaimsPrincipal user, AuthorizationPolicy policy)
    {
        ArgumentNullThrowHelper.ThrowIfNull(service);
        ArgumentNullThrowHelper.ThrowIfNull(policy);

        return service.AuthorizeAsync(user, resource: null, policy: policy);
    }

    /// <summary>
    /// Checks if a user meets a specific authorization policy against the specified resource.
    /// </summary>
    /// <param name="service">The <see cref="IAuthorizationService"/> providing authorization.</param>
    /// <param name="user">The user to evaluate the policy against.</param>
    /// <param name="policyName">The name of the policy to evaluate.</param>
    /// <returns>
    /// A flag indicating whether policy evaluation has succeeded or failed.
    /// This value is <c>true</c> when the user fulfills the policy, otherwise <c>false</c>.
    /// </returns>
    public static Task<AuthorizationResult> AuthorizeAsync(this IAuthorizationService service, ClaimsPrincipal user, string policyName)
    {
        ArgumentNullThrowHelper.ThrowIfNull(service);
        ArgumentNullThrowHelper.ThrowIfNull(policyName);

        return service.AuthorizeAsync(user, resource: null, policyName: policyName);
    }
}
