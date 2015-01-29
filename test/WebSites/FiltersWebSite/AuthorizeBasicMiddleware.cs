// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http.Security;
using Microsoft.AspNet.Security;
using Microsoft.AspNet.Security.Infrastructure;
using Microsoft.Framework.OptionsModel;

namespace FiltersWebSite
{
    public class BasicOptions : AuthenticationOptions
    {
        public BasicOptions()
        {
            AuthenticationType = "Basic";
            AuthenticationMode = AuthenticationMode.Passive;
        }

    }

    public class AuthorizeBasicMiddleware : AuthenticationMiddleware<BasicOptions>
    {
        public AuthorizeBasicMiddleware(
            RequestDelegate next, 
            IServiceProvider services, 
            IOptions<BasicOptions> options) : 
                base(next, services, options, null)
        { }

        protected override AuthenticationHandler<BasicOptions> CreateHandler()
        {
            return new BasicAuthenticationHandler();
        }
    }

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
                        new Claim(ClaimTypes.Role, "Administrator"),
                        new Claim(ClaimTypes.NameIdentifier, "John")},
                        "Basic");

            return new AuthenticationTicket(id, new AuthenticationProperties());
        }
    }
}