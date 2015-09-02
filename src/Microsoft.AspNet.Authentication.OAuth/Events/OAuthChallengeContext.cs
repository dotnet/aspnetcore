// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Authentication.OAuth
{
    /// <summary>
    /// Specifies the HTTP response header for the bearer authentication scheme.
    /// </summary>
    public class OAuthChallengeContext : BaseContext
    {
        /// <summary>
        /// Initializes a new <see cref="OAuthRequestTokenContext"/>
        /// </summary>
        /// <param name="context">HTTP environment</param>
        /// <param name="challenge">The www-authenticate header value.</param>
        public OAuthChallengeContext(
            HttpContext context,
            string challenge)
            : base(context)
        {
            Challenge = challenge;
        }

        /// <summary>
        /// The www-authenticate header value.
        /// </summary>
        public string Challenge { get; protected set; }
    }
}
