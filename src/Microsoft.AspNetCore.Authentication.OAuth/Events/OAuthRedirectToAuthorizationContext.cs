// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.OAuth
{
    /// <summary>
    /// Context passed when a Challenge causes a redirect to authorize endpoint in the handler.
    /// </summary>
    public class OAuthRedirectToAuthorizationContext : BaseContext
    {
        /// <summary>
        /// Creates a new context object.
        /// </summary>
        /// <param name="context">The HTTP request context.</param>
        /// <param name="options">The <see cref="OAuthOptions"/>.</param>
        /// <param name="properties">The authentication properties of the challenge.</param>
        /// <param name="redirectUri">The initial redirect URI.</param>
        public OAuthRedirectToAuthorizationContext(HttpContext context, OAuthOptions options, AuthenticationProperties properties, string redirectUri)
            : base(context)
        {
            RedirectUri = redirectUri;
            Properties = properties;
            Options = options;
        }

        public OAuthOptions Options { get; }

        /// <summary>
        /// Gets the URI used for the redirect operation.
        /// </summary>
        public string RedirectUri { get; private set; }

        /// <summary>
        /// Gets the authentication properties of the challenge.
        /// </summary>
        public AuthenticationProperties Properties { get; private set; }
    }
}
