// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.OAuth
{
    /// <summary>
    /// Contains information about the relevant to exchanging the code for the access token.
    /// </summary>
    public class OAuthExchangeCodeContext : PropertiesContext<OAuthOptions>
    {
        /// <summary>
        /// Initializes a new <see cref="OAuthExchangeCodeContext"/>.
        /// </summary>
        /// <param name="properties">The <see cref="AuthenticationProperties"/>.</param>
        /// <param name="context">The HTTP environment.</param>
        /// <param name="scheme">The authentication scheme.</param>
        /// <param name="options">The options used by the authentication middleware.</param>
        /// <param name="backchannel">The HTTP client used by the authentication middleware</param>
        /// <param name="redirectUri">The redirect URI</param>
        /// <param name="code">The authorization code gained after user authentication</param>
        public OAuthExchangeCodeContext(
            AuthenticationProperties properties,
            HttpContext context,
            AuthenticationScheme scheme,
            OAuthOptions options,
            HttpClient backchannel,
            string redirectUri,
            string code)
            : base(context, scheme, options, properties)
        {
            Backchannel = backchannel ?? throw new ArgumentNullException(nameof(backchannel));
            RedirectUri = redirectUri;
            Code = code;
        }

        /// <summary>
        /// Gets the backchannel used to communicate with the provider.
        /// </summary>
        public HttpClient Backchannel { get; }

        /// <summary>
        /// Gets the URI used for the redirect operation.
        /// </summary>
        public string RedirectUri { get; }

        /// <summary>
        /// Gets the code returned by the authentication provider after user authenticates
        /// </summary>
        public string Code { get; }

    }
}
