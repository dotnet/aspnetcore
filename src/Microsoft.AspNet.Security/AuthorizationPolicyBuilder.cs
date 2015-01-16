// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;

namespace Microsoft.AspNet.Security
{
    public class AuthorizationPolicyBuilder
    {
        public AuthorizationPolicyBuilder(params string[] activeAuthenticationTypes)
        {
            foreach (var authType in activeAuthenticationTypes) {
                ActiveAuthenticationTypes.Add(authType);
            }
        }

        public IList<IAuthorizationRequirement> Requirements { get; set; } = new List<IAuthorizationRequirement>();
        public IList<string> ActiveAuthenticationTypes { get; set; } = new List<string>();

        public AuthorizationPolicyBuilder RequiresClaim([NotNull] string claimType, params string[] requiredValues)
        {
            Requirements.Add(new ClaimsAuthorizationRequirement
            {
                ClaimType = claimType,
                AllowedValues = requiredValues
            });
            return this;
        }

        public AuthorizationPolicyBuilder RequiresClaim([NotNull] string claimType)
        {
            Requirements.Add(new ClaimsAuthorizationRequirement
            {
                ClaimType = claimType,
                AllowedValues = null
            });
            return this;
        }

        public AuthorizationPolicyBuilder RequiresRole([NotNull] params string[] roles)
        {
            RequiresClaim(ClaimTypes.Role, roles);
            return this;
        }

        public AuthorizationPolicyBuilder RequireAuthenticatedUser()
        {
            Requirements.Add(new DenyAnonymousAuthorizationRequirement());
            return this;
        }

        public AuthorizationPolicy Build()
        {
            return new AuthorizationPolicy(Requirements, ActiveAuthenticationTypes);
        }
    }
}
