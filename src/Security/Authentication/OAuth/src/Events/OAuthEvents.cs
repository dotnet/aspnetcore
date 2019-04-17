// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authentication.OAuth
{
    /// <summary>
    /// Default implementation.
    /// </summary>
    public class OAuthEvents : RemoteAuthenticationEvents
    {
        /// <summary>
        /// Gets or sets the function that is invoked when the CreatingTicket method is invoked.
        /// </summary>
        public Func<OAuthCreatingTicketContext, Task> OnCreatingTicket { get; set; } = context => Task.CompletedTask;

        /// <summary>
        /// Gets or sets the delegate that is invoked when the RedirectToAuthorizationEndpoint method is invoked.
        /// </summary>
        public Func<RedirectContext<OAuthOptions>, Task> OnRedirectToAuthorizationEndpoint { get; set; } = context =>
        {
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        /// <summary>
        /// Gets or sets the delegate that is invoked when the ExchangeCode method is invoked.
        /// </summary>
        public Func<OAuthExchangeCodeContext, Task<OAuthTokenResponse>> OnExchangeCode { get; set; } = async context =>
        {
            var tokenRequestParameters = new Dictionary<string, string>()
            {
                { "client_id", context.Options.ClientId },
                { "redirect_uri", context.RedirectUri },
                { "client_secret", context.Options.ClientSecret },
                { "code", context.Code },
                { "grant_type", "authorization_code" },
            };

            var requestContent = new FormUrlEncodedContent(tokenRequestParameters);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, context.Options.TokenEndpoint);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            requestMessage.Content = requestContent;

            var response = await context.Backchannel.SendAsync(requestMessage, context.HttpContext.RequestAborted);
            if (response.IsSuccessStatusCode)
            {
                var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                return OAuthTokenResponse.Success(payload);
            }
            else
            {
                var error = "OAuth token endpoint failure: "
                    + $"Status: {response.StatusCode}; Headers: {response.Headers.ToString()}; Body: {await response.Content.ReadAsStringAsync()};";
                return OAuthTokenResponse.Failed(new Exception(error));
            }
        };


        /// <summary>
        /// Invoked after the provider successfully authenticates a user.
        /// </summary>
        /// <param name="context">Contains information about the login session as well as the user <see cref="System.Security.Claims.ClaimsIdentity"/>.</param>
        /// <returns>A <see cref="Task"/> representing the completed operation.</returns>
        public virtual Task CreatingTicket(OAuthCreatingTicketContext context) => OnCreatingTicket(context);

        /// <summary>
        /// Called when a Challenge causes a redirect to authorize endpoint in the OAuth handler.
        /// </summary>
        /// <param name="context">Contains redirect URI and <see cref="AuthenticationProperties"/> of the challenge.</param>
        public virtual Task RedirectToAuthorizationEndpoint(RedirectContext<OAuthOptions> context) => OnRedirectToAuthorizationEndpoint(context);

        /// <summary>
        /// Invoked after user authenticates on the provider to exchange the code gained for the access token.
        /// </summary>
        /// <param name="context">Contains the code returned, the redirect URI and the <see cref="AuthenticationProperties"/>.</param>
        /// <returns></returns>
        public virtual Task<OAuthTokenResponse> ExchangeCode(OAuthExchangeCodeContext context) => OnExchangeCode(context);
    
    }
}
