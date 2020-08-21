// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authorization.Infrastructure
{
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
            if (allowedRoles == null)
            {
                throw new ArgumentNullException(nameof(allowedRoles));
            }

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
            return Task.CompletedTask;
        }

        public override string ToString()
        {
            var roles = $"User.IsInRole must be true for one of the following roles: ({string.Join("|", AllowedRoles)})";

            return $"{nameof(RolesAuthorizationRequirement)}:{roles}";
        }
    }
}
