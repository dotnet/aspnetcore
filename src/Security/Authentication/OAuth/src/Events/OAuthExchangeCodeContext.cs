// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.OAuth
{
    /// <summary>
    /// Contains information about the exchanging code for the access token context.
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
        /// <param name="tokenRequestParameters">The parameters that will be sent as query string for the token request</param>
        public OAuthExchangeCodeContext(
            AuthenticationProperties properties,
            HttpContext context,
            AuthenticationScheme scheme,
            OAuthOptions options,
            Dictionary<string, string> tokenRequestParameters)
            : base(context, scheme, options, properties)
        {
            TokenRequestParameters = tokenRequestParameters;
        }

        /// <summary>
        /// Gets the code returned by the authentication provider after user authenticates
        /// </summary>
        public Dictionary<string, string> TokenRequestParameters { get; }
    }
}
