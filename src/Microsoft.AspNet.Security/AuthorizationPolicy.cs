// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.Security
{
    public class AuthorizationPolicy
    {
        public AuthorizationPolicy(IEnumerable<IAuthorizationRequirement> requirements, IEnumerable<string> activeAuthenticationTypes)
        {
            Requirements = new List<IAuthorizationRequirement>(requirements).AsReadOnly();
            ActiveAuthenticationTypes = new List<string>(activeAuthenticationTypes).AsReadOnly();
        }

        public IReadOnlyList<IAuthorizationRequirement> Requirements { get; private set; }
        public IReadOnlyList<string> ActiveAuthenticationTypes { get; private set; }

        public static AuthorizationPolicy Combine([NotNull] params AuthorizationPolicy[] policies)
        {
            return Combine((IEnumerable<AuthorizationPolicy>)policies);
        }

        // TODO: Add unit tests
        public static AuthorizationPolicy Combine([NotNull] IEnumerable<AuthorizationPolicy> policies)
        {
            var builder = new AuthorizationPolicyBuilder();
            foreach (var policy in policies)
            {
                builder.Combine(policy);
            }
            return builder.Build();
        }

        public static AuthorizationPolicy Combine([NotNull] AuthorizationOptions options, [NotNull] IEnumerable<AuthorizeAttribute> attributes)
        {
            var policyBuilder = new AuthorizationPolicyBuilder();
            bool any = false;
            foreach (var authorizeAttribute in attributes.OfType<AuthorizeAttribute>())
            {
                any = true;
                var requireAnyAuthenticated = true;
                if (!string.IsNullOrWhiteSpace(authorizeAttribute.Policy))
                {
                    var policy = options.GetPolicy(authorizeAttribute.Policy);
                    if (policy == null)
                    {
                        throw new InvalidOperationException(Resources.FormatException_AuthorizationPolicyNotFound(authorizeAttribute.Policy));
                    }
                    policyBuilder.Combine(policy);
                    requireAnyAuthenticated = false;
                }
                var rolesSplit = authorizeAttribute.Roles?.Split(',');
                if (rolesSplit != null && rolesSplit.Any())
                {
                    policyBuilder.RequiresRole(rolesSplit);
                    requireAnyAuthenticated = false;
                }
                string[] authTypesSplit = authorizeAttribute.ActiveAuthenticationTypes?.Split(',');
                if (authTypesSplit != null && authTypesSplit.Any())
                {
                    foreach (var authType in authTypesSplit)
                    {
                        policyBuilder.ActiveAuthenticationTypes.Add(authType);
                    }
                }
                if (requireAnyAuthenticated)
                {
                    policyBuilder.RequireAuthenticatedUser();
                }
            }
            return any ? policyBuilder.Build() : null;
        }
    }
}
