// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;

namespace Microsoft.AspNetCore.Authorization.Infrastructure
{
    public class DenyAnonymousAuthorizationRequirement : AuthorizationHandler<DenyAnonymousAuthorizationRequirement>, IAuthorizationRequirement
    {
        protected override void Handle(AuthorizationHandlerContext context, DenyAnonymousAuthorizationRequirement requirement)
        {
            var user = context.User;
            var userIsAnonymous =
                user?.Identity == null ||
                !user.Identities.Any(i => i.IsAuthenticated);
            if (!userIsAnonymous)
            {
                context.Succeed(requirement);
            }
        }
    }
}
