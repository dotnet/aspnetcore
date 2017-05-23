// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity.Service.Configuration
{
    public class IdentityServiceOptionsSetup : IConfigureOptions<IdentityServiceOptions>
    {
        private readonly IOptions<IdentityOptions> _options;

        public IdentityServiceOptionsSetup(IOptions<IdentityOptions> options)
        {
            _options = options;
        }

        public void Configure(IdentityServiceOptions options)
        {
            options.IdTokenOptions.UserClaims
                .AddSingle(IdentityServiceClaimTypes.Subject, _options.Value.ClaimsIdentity.UserIdClaimType);

            options.IdTokenOptions.UserClaims
                .AddSingle(IdentityServiceClaimTypes.Name, _options.Value.ClaimsIdentity.UserNameClaimType);

            options.AccessTokenOptions.UserClaims
                .AddSingle(IdentityServiceClaimTypes.Name, _options.Value.ClaimsIdentity.UserNameClaimType);

            options.LoginPolicy = new AuthorizationPolicyBuilder(options.LoginPolicy)
                .AddAuthenticationSchemes(IdentityConstants.ApplicationScheme)
                .Build();
        }
    }
}
