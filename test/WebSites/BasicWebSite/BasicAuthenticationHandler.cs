// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BasicWebSite
{
    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public BasicAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var principal = new ClaimsPrincipal();
            principal.AddIdentity(new ClaimsIdentity(
                new[]
                {
                    new Claim("Manager", "yes"),
                    new Claim(ClaimTypes.Role, "Administrator"),
                    new Claim(ClaimTypes.NameIdentifier, "John")
                },
                Scheme.Name));

            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(
                principal,
                new AuthenticationProperties(),
                Scheme.Name)));
        }
    }
}
