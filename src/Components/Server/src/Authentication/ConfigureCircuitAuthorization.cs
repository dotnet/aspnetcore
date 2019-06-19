// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Server
{
    internal class ConfigureCircuitAuthorization : IPostConfigureOptions<AuthorizationOptions>
    {
        public ConfigureCircuitAuthorization(
            IOptions<CircuitOptions> circuitOptions,
            IOptions<AuthenticationOptions> authenticationOptions)
        {
            CircuitOptions = circuitOptions.Value;
            AuthenticationOptions = authenticationOptions.Value;
        }

        public CircuitOptions CircuitOptions { get; }
        public AuthenticationOptions AuthenticationOptions { get; }

        public void PostConfigure(string name, AuthorizationOptions options)
        {
            if (options.GetPolicy(CircuitAuthenticationHandler.AuthorizationPolicyName) != null)
            {
                // Someone already configured the policy, so we do nothing.
                return;
            }

            var authorizationPolicyBuilder = new AuthorizationPolicyBuilder(CircuitAuthenticationHandler.AuthenticationType);

            if (AuthenticationOptions.DefaultScheme != null)
            {
                authorizationPolicyBuilder.AddAuthenticationSchemes(AuthenticationOptions.DefaultScheme);
            }

            var policy = authorizationPolicyBuilder
                .RequireClaim(CircuitAuthenticationHandler.IdClaimType)
                .Build();

            options.AddPolicy(CircuitAuthenticationHandler.AuthorizationPolicyName, policy);
        }
    }
}
