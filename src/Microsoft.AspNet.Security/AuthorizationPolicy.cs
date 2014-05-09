// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Security
{
    /// <summary>
    /// This class provides a base implementation for <see cref="IAuthorizationPolicy" />
    /// </summary>
    public abstract class AuthorizationPolicy : IAuthorizationPolicy
    {
        public int Order { get; set; }
        
        public virtual Task ApplyingAsync(AuthorizationPolicyContext context)
        {
            return Task.FromResult(0);
        }

        public virtual Task ApplyAsync(AuthorizationPolicyContext context) 
        {
            return Task.FromResult(0);
        }

        public virtual Task AppliedAsync(AuthorizationPolicyContext context)
        {
            return Task.FromResult(0);
        }
    }
}
