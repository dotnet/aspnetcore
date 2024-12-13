// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Authorization.Policy;

/// <summary>
/// Default implementation for <see cref="IPolicyEvaluator"/>.
/// </summary>
public class PolicyEvaluator : IPolicyEvaluator
{
    private readonly IAuthorizationService _authorization;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="authorization">The authorization service.</param>
    public PolicyEvaluator(IAuthorizationService authorization)
    {
        _authorization = authorization;
    }

    /// <summary>
    /// Does authentication for <see cref="AuthorizationPolicy.AuthenticationSchemes"/> and sets the resulting
    /// <see cref="ClaimsPrincipal"/> to <see cref="HttpContext.User"/>.  If no schemes are set, this is a no-op.
    /// </summary>
    /// <param name="policy">The <see cref="AuthorizationPolicy"/>.</param>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns><see cref="AuthenticateResult.Success"/> unless all schemes specified by <see cref="AuthorizationPolicy.AuthenticationSchemes"/> failed to authenticate.  </returns>
    public virtual async Task<AuthenticateResult> AuthenticateAsync(AuthorizationPolicy policy, HttpContext context)
    {
        if (policy.AuthenticationSchemes != null && policy.AuthenticationSchemes.Count > 0)
        {
            ClaimsPrincipal? newPrincipal = null;
            DateTimeOffset? minExpiresUtc = null;
            foreach (var scheme in policy.AuthenticationSchemes)
            {
                var result = await context.AuthenticateAsync(scheme);
                if (result != null && result.Succeeded)
                {
                    newPrincipal = SecurityHelper.MergeUserPrincipal(newPrincipal, result.Principal);

                    if (minExpiresUtc is null || result.Properties?.ExpiresUtc < minExpiresUtc)
                    {
                        minExpiresUtc = result.Properties?.ExpiresUtc;
                    }
                }
            }

            if (newPrincipal != null)
            {
                context.User = newPrincipal;
                var ticket = new AuthenticationTicket(newPrincipal, string.Join(';', policy.AuthenticationSchemes));
                // ExpiresUtc is the easiest property to reason about when dealing with multiple schemes
                // SignalR will use this property to evaluate auth expiration for long running connections
                ticket.Properties.ExpiresUtc = minExpiresUtc;
                return AuthenticateResult.Success(ticket);
            }
            else
            {
                context.User = new ClaimsPrincipal(new ClaimsIdentity());
                return AuthenticateResult.NoResult();
            }
        }

        // No modifications made to the HttpContext so let's use the existing result if it exists
        return context.Features.Get<IAuthenticateResultFeature>()?.AuthenticateResult ?? DefaultAuthenticateResult(context);

        static AuthenticateResult DefaultAuthenticateResult(HttpContext context)
        {
            return (context.User?.Identity?.IsAuthenticated ?? false)
                ? AuthenticateResult.Success(new AuthenticationTicket(context.User, "context.User"))
                : AuthenticateResult.NoResult();
        }
    }

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
    public virtual async Task<PolicyAuthorizationResult> AuthorizeAsync(AuthorizationPolicy policy, AuthenticateResult authenticationResult, HttpContext context, object? resource)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var result = await _authorization.AuthorizeAsync(context.User, resource, policy);
        if (result.Succeeded)
        {
            return PolicyAuthorizationResult.Success();
        }

        // If authentication was successful, return forbidden, otherwise challenge
        return (authenticationResult.Succeeded)
            ? PolicyAuthorizationResult.Forbid(result.Failure)
            : PolicyAuthorizationResult.Challenge();
    }
}
