// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
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
        if (context.User != null)
        {
            var found = false;
            if (requirement._emptyAllowedValues)
            {
                foreach (var claim in context.User.Claims)
                {
                    if (string.Equals(claim.Type, requirement.ClaimType, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }
            }
            else
            {
                foreach (var claim in context.User.Claims)
                {
                    if (string.Equals(claim.Type, requirement.ClaimType, StringComparison.OrdinalIgnoreCase)
                        && requirement.AllowedValues!.Contains(claim.Value, StringComparer.Ordinal))
                    {
                        found = true;
                        break;
                    }
                }
            }
            if (found)
            {
                context.Succeed(requirement);
            }
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var value = (_emptyAllowedValues)
            ? string.Empty
            : $" and Claim.Value is one of the following values: ({string.Join("|", AllowedValues!)})";

        return $"{nameof(ClaimsAuthorizationRequirement)}:Claim.Type={ClaimType}{value}";
    }
}
