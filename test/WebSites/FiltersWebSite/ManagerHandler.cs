// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Authorization;
using Microsoft.Framework.DependencyInjection;

namespace FiltersWebSite
{
    public class ManagerHandler : AuthorizationHandler<OperationAuthorizationRequirement>
    {
        public override void Handle(AuthorizationContext context, OperationAuthorizationRequirement requirement)
        {
            if (context.User.HasClaim("Manager", "yes"))
            {
                context.Succeed(requirement);
            }
        }
    }
}
