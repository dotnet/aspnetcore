// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Server
{
    internal class ConfigureCircuitAuthenticationPolicy : IPostConfigureOptions<CircuitOptions>
    {
        public ConfigureCircuitAuthenticationPolicy(IOptions<AuthenticationOptions> authenticationOptions)
        {
            AuthenticationOptions = authenticationOptions.Value;
        }

        public AuthenticationOptions AuthenticationOptions { get; }

        public void PostConfigure(string name, CircuitOptions options)
        {
            if (options.AuthorizationPolicy != null)
            {
                // Someone already configured the policy, so we do nothing.
                return;
            }

            var authorizationPolicyBuilder = new AuthorizationPolicyBuilder(CircuitAuthenticationHandler.AuthenticationType);

            if (AuthenticationOptions.DefaultScheme != null)
            {
                authorizationPolicyBuilder.AddAuthenticationSchemes(AuthenticationOptions.DefaultScheme);
            }

            options.AuthorizationPolicy = authorizationPolicyBuilder
                .RequireClaim(CircuitAuthenticationHandler.IdClaimType)
                .Build();
        }
    }
}
