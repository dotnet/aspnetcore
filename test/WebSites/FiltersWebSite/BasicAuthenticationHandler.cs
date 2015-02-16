// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http.Security;
using Microsoft.AspNet.Security;
using Microsoft.AspNet.Security.Infrastructure;
using Microsoft.Framework.OptionsModel;

namespace FiltersWebSite
{
    public class BasicAuthenticationHandler : AuthenticationHandler<BasicOptions>
    {
        protected override void ApplyResponseChallenge()
        {
        }

        protected override void ApplyResponseGrant()
        {
        }

        protected override AuthenticationTicket AuthenticateCore()
        {
            var id = new ClaimsIdentity(
                new Claim[] {
                    new Claim("Permission", "CanViewPage"),
                    new Claim("Manager", "yes"),
                    new Claim(ClaimTypes.Role, "Administrator"),
                    new Claim(ClaimTypes.NameIdentifier, "John")
                },
                Options.AuthenticationType);
            return new AuthenticationTicket(id, new AuthenticationProperties());
        }
    }
}