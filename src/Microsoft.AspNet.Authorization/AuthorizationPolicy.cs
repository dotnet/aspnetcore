// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.Authorization
{
    public class AuthorizationPolicy
    {
        public AuthorizationPolicy(IEnumerable<IAuthorizationRequirement> requirements, IEnumerable<string> authenticationSchemes)
        {
            if (requirements == null)
            {
                throw new ArgumentNullException(nameof(requirements));
            }

            if (authenticationSchemes == null)
            {
                throw new ArgumentNullException(nameof(authenticationSchemes));
            }

            if (requirements.Count() == 0)
            {
                throw new InvalidOperationException(Resources.Exception_AuthorizationPolicyEmpty);
            }
            Requirements = new List<IAuthorizationRequirement>(requirements).AsReadOnly();
            AuthenticationSchemes = new List<string>(authenticationSchemes).AsReadOnly();
        }

        public IReadOnlyList<IAuthorizationRequirement> Requirements { get; }
        public IReadOnlyList<string> AuthenticationSchemes { get; }

        public static AuthorizationPolicy Combine(params AuthorizationPolicy[] policies)
        {
            if (policies == null)
            {
                throw new ArgumentNullException(nameof(policies));
            }

            return Combine((IEnumerable<AuthorizationPolicy>)policies);
        }

        public static AuthorizationPolicy Combine(IEnumerable<AuthorizationPolicy> policies)
        {
            if (policies == null)
            {
                throw new ArgumentNullException(nameof(policies));
            }

            var builder = new AuthorizationPolicyBuilder();
            foreach (var policy in policies)
            {
                builder.Combine(policy);
            }
            return builder.Build();
        }

        public static AuthorizationPolicy Combine(AuthorizationOptions options, IEnumerable<IAuthorizeData> attributes)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (attributes == null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            var policyBuilder = new AuthorizationPolicyBuilder();
            var any = false;
            foreach (var authorizeAttribute in attributes.OfType<AuthorizeAttribute>())
            {
                any = true;
                var useDefaultPolicy = true;
                if (!string.IsNullOrWhiteSpace(authorizeAttribute.Policy))
                {
                    var policy = options.GetPolicy(authorizeAttribute.Policy);
                    if (policy == null)
                    {
                        throw new InvalidOperationException(Resources.FormatException_AuthorizationPolicyNotFound(authorizeAttribute.Policy));
                    }
                    policyBuilder.Combine(policy);
                    useDefaultPolicy = false;
                }
                var rolesSplit = authorizeAttribute.Roles?.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (rolesSplit != null && rolesSplit.Any())
                {
                    for (int i = 0; i < rolesSplit.Length; ++i)
                    {    
                        rolesSplit[i] = rolesSplit[i].Trim(); 
                    }

                    policyBuilder.RequireRole(rolesSplit.Where(r => !string.IsNullOrWhiteSpace(r)));
                    useDefaultPolicy = false;
                }
                var authTypesSplit = authorizeAttribute.ActiveAuthenticationSchemes?.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (authTypesSplit != null && authTypesSplit.Any())
                {
                    foreach (var authType in authTypesSplit)
                    {
                        var trimmedAuthType = authType.Trim();
                        if(!string.IsNullOrWhiteSpace(trimmedAuthType))
                        {
                            policyBuilder.AuthenticationSchemes.Add(trimmedAuthType);
                        }
                    }
                }
                if (useDefaultPolicy)
                {
                    policyBuilder.Combine(options.DefaultPolicy);
                }
            }
            return any ? policyBuilder.Build() : null;
        }
    }
}