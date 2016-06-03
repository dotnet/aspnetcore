// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authorization.Infrastructure
{
    /// <summary>
    /// Requirement that ensures a specific Name
    /// </summary>
    public class NameAuthorizationRequirement : AuthorizationHandler<NameAuthorizationRequirement>, IAuthorizationRequirement
    {
        public NameAuthorizationRequirement(string requiredName)
        {
            if (requiredName == null)
            {
                throw new ArgumentNullException(nameof(requiredName));
            }

            RequiredName = requiredName;
        }

        public string RequiredName { get; }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, NameAuthorizationRequirement requirement)
        {
            if (context.User != null)
            {
                // REVIEW: Do we need to do normalization?  casing/loc?
                if (context.User.Identities.Any(i => string.Equals(i.Name, requirement.RequiredName)))
                {
                    context.Succeed(requirement);
                }
            }
            return Task.FromResult(0);
        }
    }
}
