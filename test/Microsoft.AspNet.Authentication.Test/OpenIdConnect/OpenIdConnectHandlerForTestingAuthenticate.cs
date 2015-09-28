// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IdentityModel.Tokens.Jwt;
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
    public class OpenIdConnectHandlerForTestingAuthenticate : OpenIdConnectHandler
    {
        public OpenIdConnectHandlerForTestingAuthenticate()
                    : base(null)
        {
        }

        protected override async Task<bool> HandleUnauthorizedAsync(ChallengeContext context)
        {
            return await base.HandleUnauthorizedAsync(context);
        }

        protected override Task<OpenIdConnectTokenEndpointResponse> RedeemAuthorizationCodeAsync(string authorizationCode, string redirectUri)
        {
            var jsonResponse = new JObject();
            jsonResponse.Add(OpenIdConnectParameterNames.IdToken, "test token");
            return Task.FromResult(new OpenIdConnectTokenEndpointResponse(jsonResponse));
        }

        protected override Task<AuthenticationTicket> GetUserInformationAsync(OpenIdConnectMessage message, JwtSecurityToken jwt, AuthenticationTicket ticket)
        {
            var claimsIdentity = (ClaimsIdentity)ticket.Principal.Identity;
            if (claimsIdentity == null)
            {
                claimsIdentity = new ClaimsIdentity();
            }
            claimsIdentity.AddClaim(new Claim("test claim", "test value"));
            return Task.FromResult(new AuthenticationTicket(new ClaimsPrincipal(claimsIdentity), ticket.Properties, ticket.AuthenticationScheme));
        }
    }
}
