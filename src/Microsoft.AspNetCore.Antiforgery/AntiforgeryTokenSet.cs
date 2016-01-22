// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Antiforgery
{
    /// <summary>
    /// The antiforgery token pair (cookie and request token) for a request.
    /// </summary>
    public class AntiforgeryTokenSet
    {
        /// <summary>
        /// Creates the antiforgery token pair (cookie and request token) for a request.
        /// </summary>
        /// <param name="requestToken">The token that is supplied in the request.</param>
        /// <param name="cookieToken">The token that is supplied in the request cookie.</param>
        public AntiforgeryTokenSet(string requestToken, string cookieToken)
        {
            if (string.IsNullOrEmpty(requestToken))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(requestToken));
            }

            RequestToken = requestToken;

            // Cookie Token is allowed to be null in the case when the old cookie is valid
            // and there is no new cookieToken generated.
            CookieToken = cookieToken;
        }

        /// <summary>
        /// The token that is supplied in the request.
        /// </summary>
        public string RequestToken { get; private set; }

        /// The cookie token is allowed to be null.
        /// This would be the case when the old cookie token is still valid.
        /// In such cases a call to GetTokens would return a token set with null cookie token.
        public string CookieToken { get; private set; }
    }
}