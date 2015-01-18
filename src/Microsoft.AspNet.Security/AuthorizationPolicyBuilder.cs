// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Microsoft.AspNet.Security
{
    public class AuthorizationPolicyBuilder
    {
        public AuthorizationPolicyBuilder(params string[] activeAuthenticationTypes)
        {
            AddAuthenticationTypes(activeAuthenticationTypes);
        }

        public AuthorizationPolicyBuilder(AuthorizationPolicy policy)
        {
            Combine(policy);
        }

        public IList<IAuthorizationRequirement> Requirements { get; set; } = new List<IAuthorizationRequirement>();
        public IList<string> ActiveAuthenticationTypes { get; set; } = new List<string>();

        public AuthorizationPolicyBuilder AddAuthenticationTypes(params string[] activeAuthTypes)
        {
            foreach (var authType in activeAuthTypes)
            {
                ActiveAuthenticationTypes.Add(authType);
            }
            return this;
        }

        public AuthorizationPolicyBuilder AddRequirements(params IAuthorizationRequirement[] requirements)
        {
            foreach (var req in requirements)
            {
                Requirements.Add(req);
            }
            return this;
        }

        public AuthorizationPolicyBuilder Combine(AuthorizationPolicy policy)
        {
            AddAuthenticationTypes(policy.ActiveAuthenticationTypes.ToArray());
            AddRequirements(policy.Requirements.ToArray());
            return this;
        }

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