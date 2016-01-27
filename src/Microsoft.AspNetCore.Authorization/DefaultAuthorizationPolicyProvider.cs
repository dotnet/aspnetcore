// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authorization
{
    /// <summary>
    /// A type which can provide a <see cref="AuthorizationPolicy"/> for a particular name.
    /// </summary>
    public class DefaultAuthorizationPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly AuthorizationOptions _options;

        public DefaultAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options.Value;
        } 

        /// <summary>
        /// Gets a <see cref="AuthorizationPolicy"/> from the given <paramref name="policyName"/>
        /// </summary>
        /// <param name="policyName"></param>
        /// <returns></returns>
        public virtual Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
        {
            return Task.FromResult(_options.GetPolicy(policyName));
        }
    }
}
