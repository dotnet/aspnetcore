// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.AspNet.Authorization
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

        protected override void Handle(AuthorizationContext context, NameAuthorizationRequirement requirement)
        {
            if (context.User != null)
            {
                // REVIEW: Do we need to do normalization?  casing/loc?
                if (context.User.Identities.Any(i => string.Equals(i.Name, requirement.RequiredName)))
                {
                    context.Succeed(requirement);
                }
            }
        }
    }
}
