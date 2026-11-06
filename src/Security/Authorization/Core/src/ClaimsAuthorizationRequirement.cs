// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Authorization.Infrastructure;

/// <summary>
/// Implements an <see cref="IAuthorizationHandler"/> and <see cref="IAuthorizationRequirement"/>
/// which requires at least one instance of the specified claim type, and, if allowed values are specified,
/// the claim value must be any of the allowed values.
/// </summary>
public class ClaimsAuthorizationRequirement : AuthorizationHandler<ClaimsAuthorizationRequirement>, IAuthorizationRequirement
{
    private readonly bool _emptyAllowedValues;

    /// <summary>
    /// Creates a new instance of <see cref="ClaimsAuthorizationRequirement"/>.
    /// </summary>
    /// <param name="claimType">The claim type that must be present.</param>
    /// <param name="allowedValues">Optional list of claim values. If specified, the claim must match one or more of these values.</param>
    public ClaimsAuthorizationRequirement(string claimType, IEnumerable<string>? allowedValues)
    {
        ArgumentNullThrowHelper.ThrowIfNull(claimType);

        ClaimType = claimType;
        AllowedValues = allowedValues;
        _emptyAllowedValues = AllowedValues == null || !AllowedValues.Any();
    }

    /// <summary>
    /// Gets the claim type that must be present.
    /// </summary>
    public string ClaimType { get; }

    /// <summary>
    /// Gets the optional list of claim values, which, if present,
    /// the claim must match.
    /// </summary>
    public IEnumerable<string>? AllowedValues { get; }

    /// <summary>
    /// Makes a decision if authorization is allowed based on the claims requirements specified.
    /// </summary>
    /// <param name="context">The authorization context.</param>
    /// <param name="requirement">The requirement to evaluate.</param>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ClaimsAuthorizationRequirement requirement)
    {
        var user = context.User;
        if (user != null)
        {
            var found = false;
            if (user.GetType() == typeof(ClaimsPrincipal))
            {
                // ClaimsPrincipal.Claims flattens the claims of every identity through an iterator. When it isn't overridden,
                // enumerate the identities' claims directly to avoid allocating the iterator and its nested enumerators.
                var identities = user.Identities;
                if (identities is IList<ClaimsIdentity> identityList)
                {
                    for (var i = 0; i < identityList.Count && !found; i++)
                    {
                        found = HasMatchingClaim(identityList[i].Claims, requirement);
                    }
                }
                else
                {
                    foreach (var identity in identities)
                    {
                        if (HasMatchingClaim(identity.Claims, requirement))
                        {
                            found = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                found = HasMatchingClaim(user.Claims, requirement);
            }

            if (found)
            {
                context.Succeed(requirement);
            }
        }
        return Task.CompletedTask;
    }

    private static bool HasMatchingClaim(IEnumerable<Claim> claims, ClaimsAuthorizationRequirement requirement)
    {
        if (claims is IList<Claim> claimList)
        {
            for (var i = 0; i < claimList.Count; i++)
            {
                if (IsMatch(claimList[i], requirement))
                {
                    return true;
                }
            }
        }
        else
        {
            foreach (var claim in claims)
            {
                if (IsMatch(claim, requirement))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsMatch(Claim claim, ClaimsAuthorizationRequirement requirement)
        => string.Equals(claim.Type, requirement.ClaimType, StringComparison.OrdinalIgnoreCase)
            && (requirement._emptyAllowedValues || requirement.AllowedValues!.Contains(claim.Value, StringComparer.Ordinal));

    /// <summary>
    /// Returns a string representation of the requirement.
    /// </summary>
    /// <returns>A description of the claim requirement.</returns>
    public override string ToString()
    {
        var value = (_emptyAllowedValues)
            ? string.Empty
            : $" and Claim.Value is one of the following values: ({string.Join("|", AllowedValues!)})";

        return $"{nameof(ClaimsAuthorizationRequirement)}:Claim.Type={ClaimType}{value}";
    }
}
