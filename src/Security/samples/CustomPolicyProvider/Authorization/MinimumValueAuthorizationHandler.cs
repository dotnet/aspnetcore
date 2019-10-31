// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;

namespace CustomPolicyProvider
{
    internal class MinimumValueAuthorizationHandler : AuthorizationHandler<MinimumValueAuthorizationRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MinimumValueAuthorizationRequirement requirement)
        {
            var middlewareContext = (AuthorizationMiddlewareContext)context.Resource;
            var routeContext = middlewareContext.HttpContext.GetRouteData();

            if (!int.TryParse(routeContext.Values["value"].ToString(), out var total) || total < requirement.MinimumValue)
            {
                context.Fail();
            }
            else
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
