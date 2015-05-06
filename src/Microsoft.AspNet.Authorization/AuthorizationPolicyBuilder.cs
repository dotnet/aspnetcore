// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Authorization
{
    public class AuthorizationPolicyBuilder
    {
        public AuthorizationPolicyBuilder(params string[] activeAuthenticationSchemes)
        {
            AddAuthenticationSchemes(activeAuthenticationSchemes);
        }

        public AuthorizationPolicyBuilder(AuthorizationPolicy policy)
        {
            Combine(policy);
        }

        public IList<IAuthorizationRequirement> Requirements { get; set; } = new List<IAuthorizationRequirement>();
        public IList<string> ActiveAuthenticationSchemes { get; set; } = new List<string>();

        public AuthorizationPolicyBuilder AddAuthenticationSchemes(params string[] activeAuthTypes)
        {
            foreach (var authType in activeAuthTypes)
            {
                ActiveAuthenticationSchemes.Add(authType);
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

        public AuthorizationPolicyBuilder Combine([NotNull] AuthorizationPolicy policy)
        {
            AddAuthenticationSchemes(policy.ActiveAuthenticationSchemes.ToArray());
            AddRequirements(policy.Requirements.ToArray());
            return this;
        }

        public AuthorizationPolicyBuilder RequireClaim([NotNull] string claimType, params string[] requiredValues)
        {
            return RequireClaim(claimType, (IEnumerable<string>)requiredValues);
        }

        public AuthorizationPolicyBuilder RequireClaim([NotNull] string claimType, IEnumerable<string> requiredValues)
        {
            Requirements.Add(new ClaimsAuthorizationRequirement(claimType, requiredValues));
            return this;
        }

        public AuthorizationPolicyBuilder RequireClaim([NotNull] string claimType)
        {
            Requirements.Add(new ClaimsAuthorizationRequirement(claimType, allowedValues: null));
            return this;
        }

        public AuthorizationPolicyBuilder RequireRole([NotNull] params string[] roles)
        {
            return RequireRole((IEnumerable<string>)roles);
        }

        public AuthorizationPolicyBuilder RequireRole([NotNull] IEnumerable<string> roles)
        {
            Requirements.Add(new RolesAuthorizationRequirement(roles));
            return this;
        }

        public AuthorizationPolicyBuilder RequireUserName([NotNull] string userName)
        {
            Requirements.Add(new NameAuthorizationRequirement(userName));
            return this;
        }

        public AuthorizationPolicyBuilder RequireAuthenticatedUser()
        {
            Requirements.Add(new DenyAnonymousAuthorizationRequirement());
            return this;
        }

        public AuthorizationPolicy Build()
        {
            return new AuthorizationPolicy(Requirements, ActiveAuthenticationSchemes.Distinct());
        }
    }
}