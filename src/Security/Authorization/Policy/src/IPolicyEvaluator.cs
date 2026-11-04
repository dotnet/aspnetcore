// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authorization.Policy;

/// <summary>
/// Base class for authorization handlers that need to be called for a specific requirement type.
/// </summary>
public interface IPolicyEvaluator
{
    /// <summary>
    /// Does authentication for <see cref="AuthorizationPolicy.AuthenticationSchemes"/> and sets the resulting
    /// <see cref="ClaimsPrincipal"/> to <see cref="HttpContext.User"/>.  If no schemes are set, this is a no-op.
    /// </summary>
    /// <param name="policy">The <see cref="AuthorizationPolicy"/>.</param>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns><see cref="AuthenticateResult.Success"/> unless all schemes specified by <see cref="AuthorizationPolicy.AuthenticationSchemes"/> fail to authenticate.  </returns>
    Task<AuthenticateResult> AuthenticateAsync(AuthorizationPolicy policy, HttpContext context);

    /// <summary>
    /// Attempts authorization for a policy using <see cref="IAuthorizationService"/>.
    /// </summary>
    /// <param name="policy">The <see cref="AuthorizationPolicy"/>.</param>
    /// <param name="authenticationResult">The result of a call to <see cref="AuthenticateAsync(AuthorizationPolicy, HttpContext)"/>.</param>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <param name="resource">
    /// An optional resource the policy should be checked with.
    /// If a resource is not required for policy evaluation you may pass null as the value.
    /// </param>
    /// <returns>Returns <see cref="PolicyAuthorizationResult.Success"/> if authorization succeeds.
    /// Otherwise returns <see cref="PolicyAuthorizationResult.Forbid(AuthorizationFailure)"/> if <see cref="AuthenticateResult.Succeeded"/>, otherwise
    /// returns  <see cref="PolicyAuthorizationResult.Challenge"/></returns>
    Task<PolicyAuthorizationResult> AuthorizeAsync(AuthorizationPolicy policy, AuthenticateResult authenticationResult, HttpContext context, object? resource);
}
