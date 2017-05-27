// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class IdentityServiceAuthorizationOptionsSetup : IConfigureOptions<AuthorizationOptions>
    {
        private readonly IOptions<IdentityServiceOptions> _identityServiceOptions;

        public IdentityServiceAuthorizationOptionsSetup(IOptions<IdentityServiceOptions> identityServiceOptions)
        {
            _identityServiceOptions = identityServiceOptions;
        }

        public void Configure(AuthorizationOptions options)
        {
            options.AddPolicy(IdentityServiceOptions.LoginPolicyName, _identityServiceOptions.Value.LoginPolicy);
            options.AddPolicy(IdentityServiceOptions.SessionPolicyName, _identityServiceOptions.Value.SessionPolicy);
            options.AddPolicy(IdentityServiceOptions.ManagementPolicyName, _identityServiceOptions.Value.ManagementPolicy);
        }
    }
}
