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
            return Task.FromResult(_options.DefaultPolicy);
        }

        /// <summary>
        /// Gets a <see cref="AuthorizationPolicy"/> from the given <paramref name="policyName"/>
        /// </summary>
        /// <param name="policyName">The policy name to retrieve.</param>
        /// <returns>The named <see cref="AuthorizationPolicy"/>.</returns>
        public virtual Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
        {
            // MVC relies on DefaultAuthorizationPolicyProvider providing the same policy for the same requests.
            return Task.FromResult(_options.GetPolicy(policyName));
        }
    }
}
