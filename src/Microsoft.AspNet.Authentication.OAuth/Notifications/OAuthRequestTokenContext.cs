// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Authentication.Notifications;

namespace Microsoft.AspNet.Authentication.OAuth
{
    /// <summary>
    /// Specifies the HTTP request header for the bearer authentication scheme.
    /// </summary>
    public class OAuthRequestTokenContext : BaseContext
    {
        /// <summary>
        /// Initializes a new <see cref="OAuthRequestTokenContext"/>
        /// </summary>
        /// <param name="context">HTTP environment</param>
        /// <param name="token">The authorization header value.</param>
        public OAuthRequestTokenContext(
            HttpContext context,
            string token)
            : base(context)
        {
            Token = token;
        }

        /// <summary>
        /// The authorization header value
        /// </summary>
        public string Token { get; set; }
    }
}
