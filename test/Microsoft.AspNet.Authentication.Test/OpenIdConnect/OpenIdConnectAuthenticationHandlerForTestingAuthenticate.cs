// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication.OpenIdConnect;
using Microsoft.AspNet.Http.Features.Authentication;

namespace Microsoft.AspNet.Authentication.Tests.OpenIdConnect
{
    /// <summary>
    ///  Allows for custom processing of ApplyResponseChallenge, ApplyResponseGrant and AuthenticateCore
    /// </summary>
    public class OpenIdConnectAuthenticationHandlerForTestingAuthenticate : OpenIdConnectAuthenticationHandler
    {
        public OpenIdConnectAuthenticationHandlerForTestingAuthenticate()
                    : base()
        {
        }

        protected override async Task<bool> HandleUnauthorizedAsync(ChallengeContext context)
        {
            return await base.HandleUnauthorizedAsync(context);
        }

        protected override Task HandleSignInAsync(SignInContext context)
        {
            return Task.FromResult(0);
        }

        protected override Task HandleSignOutAsync(SignOutContext context)
        {
            return Task.FromResult(0);
        }

        //public override bool ShouldHandleScheme(string authenticationScheme)
        //{
        //    return true;
        //}
    }
}
