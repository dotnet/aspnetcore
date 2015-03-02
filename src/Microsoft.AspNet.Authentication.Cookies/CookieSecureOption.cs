// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


namespace Microsoft.AspNet.Authentication.Cookies
{
    /// <summary>
    /// Determines how the identity cookie's security property is set.
    /// </summary>
    public enum CookieSecureOption
    {
        /// <summary>
        /// If the URI that provides the cookie is HTTPS, then the cookie will only be returned to the server on 
        /// subsequent HTTPS requests. Otherwise if the URI that provides the cookie is HTTP, then the cookie will 
        /// be returned to the server on all HTTP and HTTPS requests. This is the default value because it ensures
        /// HTTPS for all authenticated requests on deployed servers, and also supports HTTP for localhost development 
        /// and for servers that do not have HTTPS support.
        /// </summary>
        SameAsRequest,

        /// <summary>
        /// CookieOptions.Secure is never marked true. Use this value when your login page is HTTPS, but other pages
        /// on the site which are HTTP also require authentication information. This setting is not recommended because
        /// the authentication information provided with an HTTP request may be observed and used by other computers
        /// on your local network or wireless connection.
        /// </summary>
        Never,

        /// <summary>
        /// CookieOptions.Secure is always marked true. Use this value when your login page and all subsequent pages
        /// requiring the authenticated identity are HTTPS. Local development will also need to be done with HTTPS urls.
        /// </summary>
        Always,
    }
}
