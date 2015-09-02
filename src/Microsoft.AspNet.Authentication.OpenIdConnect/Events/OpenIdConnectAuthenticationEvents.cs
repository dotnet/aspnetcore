// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNet.Authentication.OpenIdConnect
{
    /// <summary>
    /// Specifies events which the <see cref="OpenIdConnectAuthenticationMiddleware" />invokes to enable developer control over the authentication process.
    /// </summary>
    public class OpenIdConnectAuthenticationEvents
    {
        /// <summary>
        /// Creates a new set of events. Each event has a default no-op behavior unless otherwise documented.
        /// </summary>
        public OpenIdConnectAuthenticationEvents()
        {
            AuthenticationFailed = context => Task.FromResult(0);
            AuthorizationCodeReceived = context => Task.FromResult(0);
            AuthorizationCodeRedeemed = context => Task.FromResult(0);
            MessageReceived = context => Task.FromResult(0);
            SecurityTokenReceived = context => Task.FromResult(0);
            SecurityTokenValidated = context => Task.FromResult(0);
            RedirectToIdentityProvider = context => Task.FromResult(0);
        }

        /// <summary>
        /// Invoked if exceptions are thrown during request processing. The exceptions will be re-thrown after this event unless suppressed.
        /// </summary>
        public Func<AuthenticationFailedContext<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>, Task> AuthenticationFailed { get; set; }

        /// <summary>
        /// Invoked after security token validation if an authorization code is present in the protocol message.
        /// </summary>
        public Func<AuthorizationCodeReceivedContext, Task> AuthorizationCodeReceived { get; set; }

        /// <summary>
        /// Invoked after "authorization code" is redeemed for tokens at the token endpoint.
        /// </summary>
        public Func<AuthorizationCodeRedeemedContext, Task> AuthorizationCodeRedeemed { get; set; }

        /// <summary>
        /// Invoked when a protocol message is first received.
        /// </summary>
        public Func<MessageReceivedContext<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>, Task> MessageReceived { get; set; }

        /// <summary>
        /// Invoked to manipulate redirects to the identity provider for SignIn, SignOut, or Challenge.
        /// </summary>
        public Func<RedirectToIdentityProviderContext<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>, Task> RedirectToIdentityProvider { get; set; }

        /// <summary>
        /// Invoked with the security token that has been extracted from the protocol message.
        /// </summary>
        public Func<SecurityTokenReceivedContext<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>, Task> SecurityTokenReceived { get; set; }

        /// <summary>
        /// Invoked after the security token has passed validation and a ClaimsIdentity has been generated.
        /// </summary>
        public Func<SecurityTokenValidatedContext<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>, Task> SecurityTokenValidated { get; set; }
    }
}