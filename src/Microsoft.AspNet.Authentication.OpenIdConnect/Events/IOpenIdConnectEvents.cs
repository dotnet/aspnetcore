// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Authentication.OpenIdConnect
{
    /// <summary>
    /// Specifies events which the <see cref="OpenIdConnectMiddleware" />invokes to enable developer control over the authentication process.
    /// </summary>
    public interface IOpenIdConnectEvents
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
        /// Invoked after "authorization code" is redeemed for tokens at the token endpoint.
        /// </summary>
        Task AuthorizationCodeRedeemed(AuthorizationCodeRedeemedContext context);

        /// <summary>
        /// Invoked when a protocol message is first received.
        /// </summary>
        Task MessageReceived(MessageReceivedContext context);

        /// <summary>
        /// Invoked to manipulate redirects to the identity provider for SignIn, SignOut, or Challenge.
        /// </summary>
        Task RedirectToIdentityProvider(RedirectToIdentityProviderContext context);

        /// <summary>
        /// Invoked with the security token that has been extracted from the protocol message.
        /// </summary>
        Task SecurityTokenReceived(SecurityTokenReceivedContext context);

        /// <summary>
        /// Invoked after the security token has passed validation and a ClaimsIdentity has been generated.
        /// </summary>
        Task SecurityTokenValidated(SecurityTokenValidatedContext context);
    }
}