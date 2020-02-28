// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Microsoft.AspNetCore.Authorization
{
    /// <summary>
    /// Used for building policies during application startup.
    /// </summary>
    public class AuthorizationPolicyBuilder
    {
        /// <summary>
        /// Creates a new instance of <see cref="AuthorizationPolicyBuilder"/>
        /// </summary>
        /// <param name="authenticationSchemes">An array of authentication schemes the policy should be evaluated against.</param>
        public AuthorizationPolicyBuilder(params string[] authenticationSchemes)
        {
            AddAuthenticationSchemes(authenticationSchemes);
        }

        /// <summary>
        /// Creates a new instance of <see cref="AuthorizationPolicyBuilder"/>.
        /// </summary>
        /// <param name="policy">The <see cref="AuthorizationPolicy"/> to build.</param>
        public AuthorizationPolicyBuilder(AuthorizationPolicy policy)
        {
            Combine(policy);
        }

        /// <summary>
        /// Gets or sets a list of <see cref="IAuthorizationRequirement"/>s which must succeed for
        /// this policy to be successful.
        /// </summary>
        public IList<IAuthorizationRequirement> Requirements { get; set; } = new List<IAuthorizationRequirement>();

        /// <summary>
        /// Gets or sets a list authentication schemes the <see cref="AuthorizationPolicyBuilder.Requirements"/> 
        /// are evaluated against.
        /// </summary>
        public IList<string> AuthenticationSchemes { get; set; } = new List<string>();

        /// <summary>
        /// Adds the specified authentication <paramref name="schemes"/> to the
        /// <see cref="AuthorizationPolicyBuilder.AuthenticationSchemes"/> for this instance.
        /// </summary>
        /// <param name="schemes">The schemes to add.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public AuthorizationPolicyBuilder AddAuthenticationSchemes(params string[] schemes)
        {
            foreach (var authType in schemes)
            {
                AuthenticationSchemes.Add(authType);
            }
            return this;
        }

        /// <summary>
        /// Adds the specified <paramref name="requirements"/> to the
        /// <see cref="AuthorizationPolicyBuilder.Requirements"/> for this instance.
        /// </summary>
        /// <param name="requirements">The authorization requirements to add.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public AuthorizationPolicyBuilder AddRequirements(params IAuthorizationRequirement[] requirements)
        {
            foreach (var req in requirements)
            {
                Requirements.Add(req);
            }
            return this;
        }

        /// <summary>
        /// Combines the specified <paramref name="policy"/> into the current instance.
        /// </summary>
        /// <param name="policy">The <see cref="AuthorizationPolicy"/> to combine.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
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

        /// <summary>
        /// Adds a <see cref="ClaimsAuthorizationRequirement"/>
        /// to the current instance.
        /// </summary>
        /// <param name="claimType">The claim type required.</param>
        /// <param name="allowedValues">Values the claim must process one or more of for evaluation to succeed.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public AuthorizationPolicyBuilder RequireClaim(string claimType, params string[] allowedValues)
        {
            if (claimType == null)
            {
                throw new ArgumentNullException(nameof(claimType));
            }

            return RequireClaim(claimType, (IEnumerable<string>)allowedValues);
        }

        /// <summary>
        /// Adds a <see cref="ClaimsAuthorizationRequirement"/>
        /// to the current instance.
        /// </summary>
        /// <param name="claimType">The claim type required.</param>
        /// <param name="allowedValues">Values the claim must process one or more of for evaluation to succeed.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public AuthorizationPolicyBuilder RequireClaim(string claimType, IEnumerable<string> allowedValues)
        {
            if (claimType == null)
            {
                throw new ArgumentNullException(nameof(claimType));
            }

            Requirements.Add(new ClaimsAuthorizationRequirement(claimType, allowedValues));
            return this;
        }

        /// <summary>
        /// Adds a <see cref="ClaimsAuthorizationRequirement"/>
        /// to the current instance.
        /// </summary>
        /// <param name="claimType">The claim type required, with no restrictions on claim value.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public AuthorizationPolicyBuilder RequireClaim(string claimType)
        {
            if (claimType == null)
            {
                throw new ArgumentNullException(nameof(claimType));
            }

            Requirements.Add(new ClaimsAuthorizationRequirement(claimType, allowedValues: null));
            return this;
        }

        /// <summary>
        /// Adds a <see cref="RolesAuthorizationRequirement"/>
        /// to the current instance.
        /// </summary>
        /// <param name="roles">The allowed roles.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public AuthorizationPolicyBuilder RequireRole(params string[] roles)
        {
            if (roles == null)
            {
                throw new ArgumentNullException(nameof(roles));
            }

            return RequireRole((IEnumerable<string>)roles);
        }

        /// <summary>
        /// Adds a <see cref="RolesAuthorizationRequirement"/>
        /// to the current instance.
        /// </summary>
        /// <param name="roles">The allowed roles.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public AuthorizationPolicyBuilder RequireRole(IEnumerable<string> roles)
        {
            if (roles == null)
            {
                throw new ArgumentNullException(nameof(roles));
            }

            Requirements.Add(new RolesAuthorizationRequirement(roles));
            return this;
        }

        /// <summary>
        /// Adds a <see cref="NameAuthorizationRequirement"/>
        /// to the current instance.
        /// </summary>
        /// <param name="userName">The user name the current user must possess.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public AuthorizationPolicyBuilder RequireUserName(string userName)
        {
            if (userName == null)
            {
                throw new ArgumentNullException(nameof(userName));
            }

            Requirements.Add(new NameAuthorizationRequirement(userName));
            return this;
        }

        /// <summary>
        /// Adds a <see cref="DenyAnonymousAuthorizationRequirement"/> to the current instance.
        /// </summary>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public AuthorizationPolicyBuilder RequireAuthenticatedUser()
        {
            Requirements.Add(new DenyAnonymousAuthorizationRequirement());
            return this;
        }

        /// <summary>
        /// Adds an <see cref="AssertionRequirement"/> to the current instance.
        /// </summary>
        /// <param name="handler">The handler to evaluate during authorization.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public AuthorizationPolicyBuilder RequireAssertion(Func<AuthorizationHandlerContext, bool> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Requirements.Add(new AssertionRequirement(handler));
            return this;
        }

        /// <summary>
        /// Adds an <see cref="AssertionRequirement"/> to the current instance.
        /// </summary>
        /// <param name="handler">The handler to evaluate during authorization.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public AuthorizationPolicyBuilder RequireAssertion(Func<AuthorizationHandlerContext, Task<bool>> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Requirements.Add(new AssertionRequirement(handler));
            return this;
        }

        /// <summary>
        /// Builds a new <see cref="AuthorizationPolicy"/> from the requirements 
        /// in this instance.
        /// </summary>
        /// <returns>
        /// A new <see cref="AuthorizationPolicy"/> built from the requirements in this instance.
        /// </returns>
        public AuthorizationPolicy Build()
        {
            return new AuthorizationPolicy(Requirements, AuthenticationSchemes.Distinct());
        }
    }
}
