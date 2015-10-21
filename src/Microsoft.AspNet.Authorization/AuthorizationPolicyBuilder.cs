// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Authorization.Infrastructure;

namespace Microsoft.AspNet.Authorization
{
    public class AuthorizationPolicyBuilder
    {
        public AuthorizationPolicyBuilder(params string[] authenticationSchemes)
        {
            AddAuthenticationSchemes(authenticationSchemes);
        }

        public AuthorizationPolicyBuilder(AuthorizationPolicy policy)
        {
            Combine(policy);
        }

        public IList<IAuthorizationRequirement> Requirements { get; set; } = new List<IAuthorizationRequirement>();
        public IList<string> AuthenticationSchemes { get; set; } = new List<string>();

        public AuthorizationPolicyBuilder AddAuthenticationSchemes(params string[] schemes)
        {
            foreach (var authType in schemes)
            {
                AuthenticationSchemes.Add(authType);
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
            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            AddAuthenticationSchemes(policy.AuthenticationSchemes.ToArray());
            AddRequirements(policy.Requirements.ToArray());
            return this;
        }

        public AuthorizationPolicyBuilder RequireClaim(string claimType, params string[] requiredValues)
        {
            if (claimType == null)
            {
                throw new ArgumentNullException(nameof(claimType));
            }

            return RequireClaim(claimType, (IEnumerable<string>)requiredValues);
        }

        public AuthorizationPolicyBuilder RequireClaim(string claimType, IEnumerable<string> requiredValues)
        {
            if (claimType == null)
            {
                throw new ArgumentNullException(nameof(claimType));
            }

            Requirements.Add(new ClaimsAuthorizationRequirement(claimType, requiredValues));
            return this;
        }

        public AuthorizationPolicyBuilder RequireClaim(string claimType)
        {
            if (claimType == null)
            {
                throw new ArgumentNullException(nameof(claimType));
            }

            Requirements.Add(new ClaimsAuthorizationRequirement(claimType, allowedValues: null));
            return this;
        }

        public AuthorizationPolicyBuilder RequireRole(params string[] roles)
        {
            if (roles == null)
            {
                throw new ArgumentNullException(nameof(roles));
            }

            return RequireRole((IEnumerable<string>)roles);
        }

        public AuthorizationPolicyBuilder RequireRole(IEnumerable<string> roles)
        {
            if (roles == null)
            {
                throw new ArgumentNullException(nameof(roles));
            }

            Requirements.Add(new RolesAuthorizationRequirement(roles));
            return this;
        }

        public AuthorizationPolicyBuilder RequireUserName(string userName)
        {
            if (userName == null)
            {
                throw new ArgumentNullException(nameof(userName));
            }

            Requirements.Add(new NameAuthorizationRequirement(userName));
            return this;
        }

        public AuthorizationPolicyBuilder RequireAuthenticatedUser()
        {
            Requirements.Add(new DenyAnonymousAuthorizationRequirement());
            return this;
        }

        public AuthorizationPolicyBuilder RequireDelegate(Action<AuthorizationContext, DelegateRequirement> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Requirements.Add(new DelegateRequirement(handler));
            return this;
        }

        public AuthorizationPolicy Build()
        {
            return new AuthorizationPolicy(Requirements, AuthenticationSchemes.Distinct());
        }
    }
}