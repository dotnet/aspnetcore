// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Authorization
{
    public class AuthorizationOptions
    {
        // TODO: make this case insensitive
        private IDictionary<string, AuthorizationPolicy> PolicyMap { get; } = new Dictionary<string, AuthorizationPolicy>();

        public void AddPolicy([NotNull] string name, [NotNull] AuthorizationPolicy policy)
        {
            PolicyMap[name] = policy;
        }

        public void AddPolicy([NotNull] string name, [NotNull] Action<AuthorizationPolicyBuilder> configurePolicy)
        {
            var policyBuilder = new AuthorizationPolicyBuilder();
            configurePolicy(policyBuilder);
            PolicyMap[name] = policyBuilder.Build();
        }

        public AuthorizationPolicy GetPolicy([NotNull] string name)
        {
            return PolicyMap.ContainsKey(name) ? PolicyMap[name] : null;
        }
   }
}