// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authorization
{
    /// <summary>
    /// The default implementation of a policy provider,
    /// which provides a <see cref="AuthorizationPolicy"/> for a particular name.
    /// </summary>
    public class DefaultAuthorizationPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly AuthorizationOptions _options;
        private Task<AuthorizationPolicy> _cachedDefaultPolicy;
        private Task<AuthorizationPolicy> _cachedRequiredPolicy;

        /// <summary>
        /// Creates a new instance of <see cref="DefaultAuthorizationPolicyProvider"/>.
        /// </summary>
        /// <param name="options">The options used to configure this instance.</param>
        public DefaultAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options.Value;
        }

        /// <summary>
        /// Gets the default authorization policy.
        /// </summary>
        /// <returns>The default authorization policy.</returns>
        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        {
            return GetCachedPolicy(ref _cachedDefaultPolicy, _options.DefaultPolicy);
        }

        /// <summary>
        /// Gets the required authorization policy.
        /// </summary>
        /// <returns>The required authorization policy.</returns>
        public Task<AuthorizationPolicy> GetRequiredPolicyAsync()
        {
            return GetCachedPolicy(ref _cachedRequiredPolicy, _options.RequiredPolicy);
        }

        private Task<AuthorizationPolicy> GetCachedPolicy(ref Task<AuthorizationPolicy> cachedPolicy, AuthorizationPolicy currentPolicy)
        {
            var local = cachedPolicy;
            if (local == null || local.Result != currentPolicy)
            {
                cachedPolicy = local = Task.FromResult(currentPolicy);
            }
            return local;
        }

        /// <summary>
        /// Gets a <see cref="AuthorizationPolicy"/> from the given <paramref name="policyName"/>
        /// </summary>
        /// <param name="policyName">The policy name to retrieve.</param>
        /// <returns>The named <see cref="AuthorizationPolicy"/>.</returns>
        public virtual Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
        {
            // MVC caches policies specifically for this class, so this method MUST return the same policy per
            // policyName for every request or it could allow undesired access. It also must return synchronously.
            // A change to either of these behaviors would require shipping a patch of MVC as well.
            return Task.FromResult(_options.GetPolicy(policyName));
        }
    }
}
