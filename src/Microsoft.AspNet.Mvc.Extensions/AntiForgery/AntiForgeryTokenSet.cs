// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Extensions;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// The anti-forgery token pair (cookie and form token) for a request.
    /// </summary>
    public class AntiForgeryTokenSet
    {
        /// <summary>
        /// Creates the anti-forgery token pair (cookie and form token) for a request.
        /// </summary>
        /// <param name="formToken">The token that is supplied in the request form body.</param>
        /// <param name="cookieToken">The token that is supplied in the request cookie.</param>
        public AntiForgeryTokenSet(string formToken, string cookieToken)
        {
            if (string.IsNullOrEmpty(formToken))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(formToken));
            }

            FormToken = formToken;

            // Cookie Token is allowed to be null in the case when the old cookie is valid
            // and there is no new cookieToken generated.
            CookieToken = cookieToken;
        }

        /// <summary>
        /// The token that is supplied in the request form body.
        /// </summary>
        public string FormToken { get; private set; }

        /// The cookie token is allowed to be null.
        /// This would be the case when the old cookie token is still valid.
        /// In such cases a call to GetTokens would return a token set with null cookie token.
        public string CookieToken { get; private set; }
    }
}