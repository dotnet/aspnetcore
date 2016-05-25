// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect
{
    /// <summary>
    /// Specifies events which the <see cref="OpenIdConnectMiddleware" />invokes to enable developer control over the authentication process.
    /// </summary>
    public interface IOpenIdConnectEvents : IRemoteAuthenticationEvents
    {
        /// <summary>
        /// Invoked if exceptions are thrown during request processing. The exceptions will be re-thrown after this event unless suppressed.
        /// </summary>
        Task AuthenticationFailed(AuthenticationFailedContext context);

        /// <summary>
        /// Invoked after security token validation if an authorization code is present in the protocol message.
        /// </summary>
        Task AuthorizationCodeReceived(AuthorizationCodeReceivedContext context);

        /// <summary>
        /// Invoked when a protocol message is first received.
        /// </summary>
        Task MessageReceived(MessageReceivedContext context);

        /// <summary>
        /// Invoked before redirecting to the identity provider to authenticate.
        /// </summary>
        Task RedirectToIdentityProvider(RedirectContext context);

        /// <summary>
        /// Invoked before redirecting to the identity provider to sign out.
        /// </summary>
        Task RedirectToIdentityProviderForSignOut(RedirectContext context);

        /// <summary>
        /// Invoked when a request is received on the RemoteSignOutPath.
        /// </summary>
        Task RemoteSignOut(RemoteSignOutContext context);

        /// <summary>
        /// Invoked after "authorization code" is redeemed for tokens at the token endpoint.
        /// </summary>
        Task TokenResponseReceived(TokenResponseReceivedContext context);

        /// <summary>
        /// Invoked when an IdToken has been validated and produced an AuthenticationTicket.
        /// </summary>
        Task TokenValidated(TokenValidatedContext context);

        /// <summary>
        /// Invoked when user information is retrieved from the UserInfoEndpoint.
        /// </summary>
        Task UserInformationReceived(UserInformationReceivedContext context);
    }
}