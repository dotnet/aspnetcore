// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication.OpenIdConnect;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Http.Features.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Authentication.Tests.OpenIdConnect
{
    /// <summary>
    ///  Allows for custom processing of ApplyResponseChallenge, ApplyResponseGrant and AuthenticateCore
    /// </summary>
    public class OpenIdConnectAuthenticationHandlerForTestingAuthenticate : OpenIdConnectAuthenticationHandler
    {
        public OpenIdConnectAuthenticationHandlerForTestingAuthenticate()
                    : base(null)
        {
        }

        protected override async Task<bool> HandleUnauthorizedAsync(ChallengeContext context)
        {
            return await base.HandleUnauthorizedAsync(context);
        }

        protected override async Task<OpenIdConnectTokenEndpointResponse> RedeemAuthorizationCodeAsync(string authorizationCode, string redirectUri)
        {
            var jsonResponse = new JObject();
            jsonResponse.Add(OpenIdConnectParameterNames.IdToken, "test token");
            return new OpenIdConnectTokenEndpointResponse(jsonResponse);
        }

        protected override async Task<AuthenticationTicket> GetUserInformationAsync(AuthenticationProperties properties, OpenIdConnectMessage message, AuthenticationTicket ticket)
        {
            var claimsIdentity = (ClaimsIdentity)ticket.Principal.Identity;
            if (claimsIdentity == null)
            {
                claimsIdentity = new ClaimsIdentity();
            }
            claimsIdentity.AddClaim(new Claim("test claim", "test value"));
            return new AuthenticationTicket(new ClaimsPrincipal(claimsIdentity), ticket.Properties, ticket.AuthenticationScheme);
        }
    }
}
