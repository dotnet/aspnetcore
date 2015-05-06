// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Authorization
{
    /// <summary>
    /// Requirement that ensures a specific Name
    /// </summary>
    public class NameAuthorizationRequirement : AuthorizationHandler<NameAuthorizationRequirement>, IAuthorizationRequirement
    {
        public NameAuthorizationRequirement([NotNull] string requiredName)
        {
            RequiredName = requiredName;
        }

        public string RequiredName { get; }

        public override void Handle(AuthorizationContext context, NameAuthorizationRequirement requirement)
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
