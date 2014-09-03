// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Security;
using Microsoft.AspNet.Security.OAuth;
using Microsoft.Framework.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Security.MicrosoftAccount
{
    internal class MicrosoftAccountAuthenticationHandler : OAuthAuthenticationHandler<MicrosoftAccountAuthenticationOptions, IMicrosoftAccountAuthenticationNotifications>
    {
        public MicrosoftAccountAuthenticationHandler(HttpClient httpClient, ILogger logger)
            : base(httpClient, logger)
        {
        }

        protected override async Task<AuthenticationTicket> GetUserInformationAsync(AuthenticationProperties properties, TokenResponse tokens)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, Options.UserInformationEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
            HttpResponseMessage graphResponse = await Backchannel.SendAsync(request, Context.RequestAborted);
            graphResponse.EnsureSuccessStatusCode();
            string accountString = await graphResponse.Content.ReadAsStringAsync();
            JObject accountInformation = JObject.Parse(accountString);

            var context = new MicrosoftAccountAuthenticatedContext(Context, Options, accountInformation, tokens);
            context.Properties = properties;
            context.Identity = new ClaimsIdentity(
                new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, context.Id, ClaimValueTypes.String, Options.AuthenticationType),
                        new Claim(ClaimTypes.Name, context.Name, ClaimValueTypes.String, Options.AuthenticationType),
                        new Claim("urn:microsoftaccount:id", context.Id, ClaimValueTypes.String, Options.AuthenticationType),
                        new Claim("urn:microsoftaccount:name", context.Name, ClaimValueTypes.String, Options.AuthenticationType)
                    },
                Options.AuthenticationType,
                ClaimsIdentity.DefaultNameClaimType,
                ClaimsIdentity.DefaultRoleClaimType);

            if (!string.IsNullOrWhiteSpace(context.Email))
            {
                context.Identity.AddClaim(new Claim(ClaimTypes.Email, context.Email, ClaimValueTypes.String, Options.AuthenticationType));
            }

            await Options.Notifications.Authenticated(context);

            return new AuthenticationTicket(context.Identity, context.Properties);
        }
    }
}
