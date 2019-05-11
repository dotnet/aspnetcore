// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Cors.Infrastructure
{
    /// <summary>
    /// Provides programmatic configuration for Cors.
    /// </summary>
    public class CorsOptions
    {
        private string _defaultPolicyName = "__DefaultCorsPolicy";

        // DefaultCorsPolicyProvider returns a Task<CorsPolicy>. We'll cache the value to be returned alongside
        // the actual policy instance to have a separate lookup.
        internal IDictionary<string, (CorsPolicy policy, Task<CorsPolicy> policyTask)> PolicyMap { get; }
            = new Dictionary<string, (CorsPolicy, Task<CorsPolicy>)>(StringComparer.Ordinal);

        public string DefaultPolicyName
        {
            get => _defaultPolicyName;
            set
            {
                _defaultPolicyName = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        /// Adds a new policy and sets it as the default.
        /// </summary>
        /// <param name="policy">The <see cref="CorsPolicy"/> policy to be added.</param>
        public void AddDefaultPolicy(CorsPolicy policy)
        {
            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            AddPolicy(DefaultPolicyName, policy);
        }

        /// <summary>
        /// Adds a new policy and sets it as the default.
        /// </summary>
        /// <param name="configurePolicy">A delegate which can use a policy builder to build a policy.</param>
        public void AddDefaultPolicy(Action<CorsPolicyBuilder> configurePolicy)
        {
            if (configurePolicy == null)
            {
                throw new ArgumentNullException(nameof(configurePolicy));
            }

            AddPolicy(DefaultPolicyName, configurePolicy);
        }

        /// <summary>
        /// Adds a new policy.
        /// </summary>
        /// <param name="name">The name of the policy.</param>
        /// <param name="policy">The <see cref="CorsPolicy"/> policy to be added.</param>
        public void AddPolicy(string name, CorsPolicy policy)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            PolicyMap[name] = (policy, Task.FromResult(policy));
        }

        /// <summary>
        /// Adds a new policy.
        /// </summary>
        /// <param name="name">The name of the policy.</param>
        /// <param name="configurePolicy">A delegate which can use a policy builder to build a policy.</param>
        public void AddPolicy(string name, Action<CorsPolicyBuilder> configurePolicy)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (configurePolicy == null)
            {
                throw new ArgumentNullException(nameof(configurePolicy));
            }

            var policyBuilder = new CorsPolicyBuilder();
            configurePolicy(policyBuilder);
            var policy = policyBuilder.Build();

            PolicyMap[name] = (policy, Task.FromResult(policy));
        }

        /// <summary>
        /// Gets the policy based on the <paramref name="name"/>
        /// </summary>
        /// <param name="name">The name of the policy to lookup.</param>
        /// <returns>The <see cref="CorsPolicy"/> if the policy was added.<c>null</c> otherwise.</returns>
        public CorsPolicy GetPolicy(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (PolicyMap.TryGetValue(name, out var result))
            {
                return result.policy;
            }

            return null;
        }
    }
}
