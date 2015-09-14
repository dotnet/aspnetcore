// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Authentication.OpenIdConnect
{
    /// <summary>
    /// Specifies events which the <see cref="OpenIdConnectMiddleware" />invokes to enable developer control over the authentication process.
    /// </summary>
    public class OpenIdConnectEvents : IOpenIdConnectEvents
    {
        /// <summary>
        /// Invoked if exceptions are thrown during request processing. The exceptions will be re-thrown after this event unless suppressed.
        /// </summary>
        public Func<AuthenticationFailedContext, Task> OnAuthenticationFailed { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Invoked after security token validation if an authorization code is present in the protocol message.
        /// </summary>
        public Func<AuthorizationCodeReceivedContext, Task> OnAuthorizationCodeReceived { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Invoked after "authorization code" is redeemed for tokens at the token endpoint.
        /// </summary>
        public Func<AuthorizationCodeRedeemedContext, Task> OnAuthorizationCodeRedeemed { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Invoked when a protocol message is first received.
        /// </summary>
        public Func<MessageReceivedContext, Task> OnMessageReceived { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Invoked to manipulate redirects to the identity provider for SignIn, SignOut, or Challenge.
        /// </summary>
        public Func<RedirectToIdentityProviderContext, Task> OnRedirectToIdentityProvider { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Invoked with the security token that has been extracted from the protocol message.
        /// </summary>
        public Func<SecurityTokenReceivedContext, Task> OnSecurityTokenReceived { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Invoked after the security token has passed validation and a ClaimsIdentity has been generated.
        /// </summary>
        public Func<SecurityTokenValidatedContext, Task> OnSecurityTokenValidated { get; set; } = context => Task.FromResult(0);

        public virtual Task AuthenticationFailed(AuthenticationFailedContext context) => OnAuthenticationFailed(context);

        public virtual Task AuthorizationCodeReceived(AuthorizationCodeReceivedContext context) => OnAuthorizationCodeReceived(context);

        public virtual Task AuthorizationCodeRedeemed(AuthorizationCodeRedeemedContext context) => OnAuthorizationCodeRedeemed(context);

        public virtual Task MessageReceived(MessageReceivedContext context) => OnMessageReceived(context);

        public virtual Task RedirectToIdentityProvider(RedirectToIdentityProviderContext context) => OnRedirectToIdentityProvider(context);

        public virtual Task SecurityTokenReceived(SecurityTokenReceivedContext context) => OnSecurityTokenReceived(context);

        public virtual Task SecurityTokenValidated(SecurityTokenValidatedContext context) => OnSecurityTokenValidated(context);
    }
}