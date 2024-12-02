// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authorization.Infrastructure;

/// <summary>
/// Implements an <see cref="IAuthorizationHandler"/> and <see cref="IAuthorizationRequirement"/> which requires the current user must be authenticated.
/// This calls <see cref="AuthorizationHandlerContext.Succeed(IAuthorizationRequirement)"/> for authenticated users. Like all built-in requirements,
/// it never calls <see cref="AuthorizationHandlerContext.Fail()"/>. The <see cref="DefaultAuthorizationEvaluator"/> produces a failed <see cref="AuthorizationResult" /> 
/// when any requirement has not succeeded even if other requirements have succeeded, and no requirement has explicitly failed.
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
        var user = context.User;
        var userIsAnonymous =
            user?.Identity == null ||
            !user.Identities.Any(i => i.IsAuthenticated);
        if (!userIsAnonymous)
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
