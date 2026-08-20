// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Authorization.Infrastructure;

/// <summary>
/// Implements an <see cref="IAuthorizationHandler"/> and <see cref="IAuthorizationRequirement"/>
/// which requires the current user must be authenticated.
/// </summary>
public class DenyAnonymousAuthorizationRequirement : AuthorizationHandler<DenyAnonymousAuthorizationRequirement>, IAuthorizationRequirement
{
    /// <summary>
    /// Makes a decision if authorization is allowed based on a specific requirement.
    /// </summary>
    /// <param name="context">The authorization context.</param>
    /// <param name="requirement">The requirement to evaluate.</param>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, DenyAnonymousAuthorizationRequirement requirement)
    {
        if (SecurityHelper.IsAuthenticated(context.User))
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{nameof(DenyAnonymousAuthorizationRequirement)}: Requires an authenticated user.";
    }
}
