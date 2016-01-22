// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.Extensions.Options;

namespace FiltersWebSite
{
    public class BasicAuthenticationHandler : AuthenticationHandler<BasicOptions>
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var principal = new ClaimsPrincipal();
            principal.AddIdentity(new ClaimsIdentity(
                new Claim[] {
                    new Claim("Permission", "CanViewPage"),
                    new Claim("Manager", "yes"),
                    new Claim(ClaimTypes.Role, "Administrator"),
                    new Claim(ClaimTypes.NameIdentifier, "John")
                },
                Options.AuthenticationScheme));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, 
                new AuthenticationProperties(), Options.AuthenticationScheme)));
        }
    }
}