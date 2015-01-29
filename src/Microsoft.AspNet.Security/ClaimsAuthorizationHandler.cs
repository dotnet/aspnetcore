// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Security
{
    public class ClaimsAuthorizationHandler : AuthorizationHandler<ClaimsAuthorizationRequirement>
    {
        public override Task<bool> CheckAsync(AuthorizationContext context, ClaimsAuthorizationRequirement requirement)
        {
            if (context.Context.User == null)
            {
                return Task.FromResult(false);
            }

            bool found = false;
            if (requirement.AllowedValues == null || !requirement.AllowedValues.Any())
            {
                found = context.Context.User.Claims.Any(c => string.Equals(c.Type, requirement.ClaimType, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                found = context.Context.User.Claims.Any(c => string.Equals(c.Type, requirement.ClaimType, StringComparison.OrdinalIgnoreCase)
                                                    && requirement.AllowedValues.Contains(c.Value, StringComparer.Ordinal));
            }
            return Task.FromResult(found);
        }
    }
}
