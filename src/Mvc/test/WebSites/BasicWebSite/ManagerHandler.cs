// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace BasicWebSite;

public class ManagerHandler : AuthorizationHandler<OperationAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement)
    {
        if (context.User.HasClaim("Manager", "yes"))
        {
            context.Succeed(requirement);
        }
        return Task.FromResult(0);
    }
}
