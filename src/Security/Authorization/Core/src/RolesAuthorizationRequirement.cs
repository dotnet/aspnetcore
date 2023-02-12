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
/// which requires at least one role claim whose value must be any of the allowed roles.
/// </summary>
public class RolesAuthorizationRequirement : AuthorizationHandler<RolesAuthorizationRequirement>, IAuthorizationRequirement
{
    /// <summary>
    /// Creates a new instance of <see cref="RolesAuthorizationRequirement"/>.
    /// </summary>
    /// <param name="allowedRoles">A collection of allowed roles.</param>
    public RolesAuthorizationRequirement(IEnumerable<string> allowedRoles)
    {
        ArgumentNullThrowHelper.ThrowIfNull(allowedRoles);

        if (!allowedRoles.Any())
        {
            throw new InvalidOperationException(Resources.Exception_RoleRequirementEmpty);
        }
        AllowedRoles = allowedRoles;
    }

    /// <summary>
    /// Gets the collection of allowed roles.
    /// </summary>
    public IEnumerable<string> AllowedRoles { get; }

    /// <summary>
    /// Makes a decision if authorization is allowed based on a specific requirement.
    /// </summary>
    /// <param name="context">The authorization context.</param>
    /// <param name="requirement">The requirement to evaluate.</param>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RolesAuthorizationRequirement requirement)
    {
        if (context.User != null)
        {
            var found = false;

            foreach (var role in requirement.AllowedRoles)
            {
                if (context.User.IsInRole(role))
                {
                    found = true;
                    break;
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
        var roles = $"User.IsInRole must be true for one of the following roles: ({string.Join("|", AllowedRoles)})";

        return $"{nameof(RolesAuthorizationRequirement)}:{roles}";
    }
}
