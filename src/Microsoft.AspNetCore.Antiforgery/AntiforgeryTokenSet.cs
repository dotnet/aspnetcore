// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Antiforgery
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
        /// <param name="formFieldName">The name of the form field used for the request token.</param>
        /// <param name="headerName">The name of the header used for the request token.</param>
        public AntiforgeryTokenSet(
            string requestToken,
            string cookieToken,
            string formFieldName,
            string headerName)
        {
            if (string.IsNullOrEmpty(requestToken))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(requestToken));
            }

            if (formFieldName == null)
            {
                throw new ArgumentNullException(nameof(formFieldName));
            }

            RequestToken = requestToken;

            // Cookie Token is allowed to be null in the case when the old cookie is valid
            // and there is no new cookieToken generated.
            CookieToken = cookieToken;

            FormFieldName = formFieldName;
            HeaderName = headerName;
        }

        /// <summary>
        /// Gets the request token.
        /// </summary>
        public string RequestToken { get; }

        /// <summary>
        /// Gets the name of the form field used for the request token.
        /// </summary>
        public string FormFieldName { get; }

        /// <summary>
        /// Gets the name of the header used for the request token.
        /// </summary>
        public string HeaderName { get; }

        /// <summary>
        /// Gets the cookie token.
        /// </summary>
        public string CookieToken { get; }
    }
}