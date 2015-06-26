// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Authorization
{
    // Must belong to with one of specified roles
    // If AllowedRoles is null or empty, that means any role is valid
    public class RolesAuthorizationRequirement : AuthorizationHandler<RolesAuthorizationRequirement>, IAuthorizationRequirement
    {
        public RolesAuthorizationRequirement([NotNull] IEnumerable<string> allowedRoles)
        {
            if (allowedRoles.Count() == 0)
            {
                throw new InvalidOperationException(Resources.Exception_RoleRequirementEmpty);
            }
            AllowedRoles = allowedRoles;
        }

        public IEnumerable<string> AllowedRoles { get; }

        protected override void Handle(AuthorizationContext context, RolesAuthorizationRequirement requirement)
        {
            if (context.User != null)
            {
                bool found = false;
                if (requirement.AllowedRoles == null || !requirement.AllowedRoles.Any())
                {
                    // Review: What do we want to do here?  No roles requested is auto success?
                }
                else
                {
                    found = requirement.AllowedRoles.Any(r => context.User.IsInRole(r));
                }
                if (found)
                {
                    context.Succeed(requirement);
                }
            }
        }

    }
}
