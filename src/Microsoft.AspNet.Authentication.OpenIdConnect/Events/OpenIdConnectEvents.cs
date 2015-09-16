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
        /// Invoked when the authentication process completes.
        /// </summary>
        public Func<AuthenticationCompletedContext, Task> OnAuthenticationCompleted { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Invoked if exceptions are thrown during request processing. The exceptions will be re-thrown after this event unless suppressed.
        /// </summary>
        public Func<AuthenticationFailedContext, Task> OnAuthenticationFailed { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Invoked after the id token has passed validation and a ClaimsIdentity has been generated.
        /// </summary>
        public Func<AuthenticationValidatedContext, Task> OnAuthenticationValidated { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Invoked after security token validation if an authorization code is present in the protocol message.
        /// </summary>
        public Func<AuthorizationCodeReceivedContext, Task> OnAuthorizationCodeReceived { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Invoked when an authorization response is received.
        /// </summary>
        public Func<AuthorizationResponseReceivedContext, Task> OnAuthorizationResponseReceived { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Invoked when a protocol message is first received.
        /// </summary>
        public Func<MessageReceivedContext, Task> OnMessageReceived { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Invoked before redirecting to the identity provider to authenticate.
        /// </summary>
        public Func<RedirectContext, Task> OnRedirectToAuthenticationEndpoint { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Invoked before redirecting to the identity provider to sign out.
        /// </summary>
        public Func<RedirectContext, Task> OnRedirectToEndSessionEndpoint { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Invoked after "authorization code" is redeemed for tokens at the token endpoint.
        /// </summary>
        public Func<TokenResponseReceivedContext, Task> OnTokenResponseReceived { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Invoked when user information is retrieved from the UserInfoEndpoint.
        /// </summary>
        public Func<UserInformationReceivedContext, Task> OnUserInformationReceived { get; set; } = context => Task.FromResult(0);

        public virtual Task AuthenticationCompleted(AuthenticationCompletedContext context) => OnAuthenticationCompleted(context);

        public virtual Task AuthenticationFailed(AuthenticationFailedContext context) => OnAuthenticationFailed(context);

        public virtual Task AuthenticationValidated(AuthenticationValidatedContext context) => OnAuthenticationValidated(context);

        public virtual Task AuthorizationCodeReceived(AuthorizationCodeReceivedContext context) => OnAuthorizationCodeReceived(context);

        public virtual Task AuthorizationResponseReceived(AuthorizationResponseReceivedContext context) => OnAuthorizationResponseReceived(context);

        public virtual Task MessageReceived(MessageReceivedContext context) => OnMessageReceived(context);

        public virtual Task RedirectToAuthenticationEndpoint(RedirectContext context) => OnRedirectToAuthenticationEndpoint(context);

        public virtual Task RedirectToEndSessionEndpoint(RedirectContext context) => OnRedirectToEndSessionEndpoint(context);

        public virtual Task TokenResponseReceived(TokenResponseReceivedContext context) => OnTokenResponseReceived(context);

        public virtual Task UserInformationReceived(UserInformationReceivedContext context) => OnUserInformationReceived(context);
    }
}