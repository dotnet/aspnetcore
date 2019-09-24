// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Authorization
{
    /// <summary>
    /// Provides programmatic configuration used by <see cref="IAuthorizationService"/> and <see cref="IAuthorizationPolicyProvider"/>.
    /// </summary>
    public class AuthorizationOptions
    {
        private IDictionary<string, AuthorizationPolicy> PolicyMap { get; } = new Dictionary<string, AuthorizationPolicy>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Determines whether authentication handlers should be invoked after a failure.
        /// Defaults to true.
        /// </summary>
        public bool InvokeHandlersAfterFailure { get; set; } = true;

        /// <summary>
        /// Gets or sets the default authorization policy. Defaults to require authenticated users.
        /// </summary>
        /// <remarks>
        /// The default policy used when evaluating <see cref="IAuthorizeData"/> with no policy name specified.
        /// </remarks>
        public AuthorizationPolicy DefaultPolicy { get; set; } = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

        /// <summary>
        /// Gets or sets the fallback authorization policy used by <see cref="AuthorizationPolicy.CombineAsync(IAuthorizationPolicyProvider, IEnumerable{IAuthorizeData})"/>
        /// when no IAuthorizeData have been provided. As a result, the AuthorizationMiddleware uses the fallback policy
        /// if there are no <see cref="IAuthorizeData"/> instances for a resource. If a resource has any <see cref="IAuthorizeData"/>
        /// then they are evaluated instead of the fallback policy. By default the fallback policy is null, and usually will have no 
        /// effect unless you have the AuthorizationMiddleware in your pipeline. It is not used in any way by the 
        /// default <see cref="IAuthorizationService"/>.
        /// </summary>
        public AuthorizationPolicy FallbackPolicy { get; set; }

        /// <summary>
        /// Add an authorization policy with the provided name.
        /// </summary>
        /// <param name="name">The name of the policy.</param>
        /// <param name="policy">The authorization policy.</param>
        public void AddPolicy(string name, AuthorizationPolicy policy)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            PolicyMap[name] = policy;
        }

        /// <summary>
        /// Add a policy that is built from a delegate with the provided name.
        /// </summary>
        /// <param name="name">The name of the policy.</param>
        /// <param name="configurePolicy">The delegate that will be used to build the policy.</param>
        public void AddPolicy(string name, Action<AuthorizationPolicyBuilder> configurePolicy)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (configurePolicy == null)
            {
                throw new ArgumentNullException(nameof(configurePolicy));
            }

            var policyBuilder = new AuthorizationPolicyBuilder();
            configurePolicy(policyBuilder);
            PolicyMap[name] = policyBuilder.Build();
        }

        /// <summary>
        /// Returns the policy for the specified name, or null if a policy with the name does not exist.
        /// </summary>
        /// <param name="name">The name of the policy to return.</param>
        /// <returns>The policy for the specified name, or null if a policy with the name does not exist.</returns>
        public AuthorizationPolicy GetPolicy(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return PolicyMap.ContainsKey(name) ? PolicyMap[name] : null;
        }
    }
}
