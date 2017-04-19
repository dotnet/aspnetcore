// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect
{
    /// <summary>
    /// Specifies events which the <see cref="OpenIdConnectHandler" />invokes to enable developer control over the authentication process.
    /// </summary>
    public class OpenIdConnectEvents : RemoteAuthenticationEvents
    {
        /// <summary>
        /// Invoked if exceptions are thrown during request processing. The exceptions will be re-thrown after this event unless suppressed.
        /// </summary>
        public Func<AuthenticationFailedContext, Task> OnAuthenticationFailed { get; set; } = context => TaskCache.CompletedTask;

        /// <summary>
        /// Invoked after security token validation if an authorization code is present in the protocol message.
        /// </summary>
        public Func<AuthorizationCodeReceivedContext, Task> OnAuthorizationCodeReceived { get; set; } = context => TaskCache.CompletedTask;

        /// <summary>
        /// Invoked when a protocol message is first received.
        /// </summary>
        public Func<MessageReceivedContext, Task> OnMessageReceived { get; set; } = context => TaskCache.CompletedTask;

        /// <summary>
        /// Invoked before redirecting to the identity provider to authenticate.
        /// </summary>
        public Func<RedirectContext, Task> OnRedirectToIdentityProvider { get; set; } = context => TaskCache.CompletedTask;

        /// <summary>
        /// Invoked before redirecting to the identity provider to sign out.
        /// </summary>
        public Func<RedirectContext, Task> OnRedirectToIdentityProviderForSignOut { get; set; } = context => TaskCache.CompletedTask;

        /// <summary>
        /// Invoked when a request is received on the RemoteSignOutPath.
        /// </summary>
        public Func<RemoteSignOutContext, Task> OnRemoteSignOut { get; set; } = context => TaskCache.CompletedTask;

        /// <summary>
        /// Invoked after "authorization code" is redeemed for tokens at the token endpoint.
        /// </summary>
        public Func<TokenResponseReceivedContext, Task> OnTokenResponseReceived { get; set; } = context => TaskCache.CompletedTask;

        /// <summary>
        /// Invoked when an IdToken has been validated and produced an AuthenticationTicket.
        /// </summary>
        public Func<TokenValidatedContext, Task> OnTokenValidated { get; set; } = context => TaskCache.CompletedTask;

        /// <summary>
        /// Invoked when user information is retrieved from the UserInfoEndpoint.
        /// </summary>
        public Func<UserInformationReceivedContext, Task> OnUserInformationReceived { get; set; } = context => TaskCache.CompletedTask;

        public virtual Task AuthenticationFailed(AuthenticationFailedContext context) => OnAuthenticationFailed(context);

        public virtual Task AuthorizationCodeReceived(AuthorizationCodeReceivedContext context) => OnAuthorizationCodeReceived(context);

        public virtual Task MessageReceived(MessageReceivedContext context) => OnMessageReceived(context);

        public virtual Task RedirectToIdentityProvider(RedirectContext context) => OnRedirectToIdentityProvider(context);

        public virtual Task RedirectToIdentityProviderForSignOut(RedirectContext context) => OnRedirectToIdentityProviderForSignOut(context);

        public virtual Task RemoteSignOut(RemoteSignOutContext context) => OnRemoteSignOut(context);

        public virtual Task TokenResponseReceived(TokenResponseReceivedContext context) => OnTokenResponseReceived(context);

        public virtual Task TokenValidated(TokenValidatedContext context) => OnTokenValidated(context);

        public virtual Task UserInformationReceived(UserInformationReceivedContext context) => OnUserInformationReceived(context);
    }
}