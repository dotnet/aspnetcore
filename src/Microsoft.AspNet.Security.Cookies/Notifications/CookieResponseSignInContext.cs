// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Security.Claims;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Abstractions.Security;
using Microsoft.AspNet.Security.Notifications;

namespace Microsoft.AspNet.Security.Cookies
{
    /// <summary>
    /// Context object passed to the ICookieAuthenticationProvider method ResponseSignIn.
    /// </summary>    
    public class CookieResponseSignInContext : BaseContext<CookieAuthenticationOptions>
    {
        /// <summary>
        /// Creates a new instance of the context object.
        /// </summary>
        /// <param name="context">The OWIN request context</param>
        /// <param name="options">The middleware options</param>
        /// <param name="authenticationType">Initializes AuthenticationType property</param>
        /// <param name="identity">Initializes Identity property</param>
        /// <param name="properties">Initializes Extra property</param>
        /// <param name="cookieOptions">Initializes options for the authentication cookie.</param>
        public CookieResponseSignInContext(
            HttpContext context,
            CookieAuthenticationOptions options,
            string authenticationType,
            ClaimsIdentity identity,
            AuthenticationProperties properties,
            CookieOptions cookieOptions)
            : base(context, options)
        {
            AuthenticationType = authenticationType;
            Identity = identity;
            Properties = properties;
            CookieOptions = cookieOptions;
        }

        /// <summary>
        /// The name of the AuthenticationType creating a cookie
        /// </summary>
        public string AuthenticationType { get; private set; }

        /// <summary>
        /// Contains the claims about to be converted into the outgoing cookie.
        /// May be replaced or altered during the ResponseSignIn call.
        /// </summary>
        public ClaimsIdentity Identity { get; set; }

        /// <summary>
        /// Contains the extra data about to be contained in the outgoing cookie.
        /// May be replaced or altered during the ResponseSignIn call.
        /// </summary>
        public AuthenticationProperties Properties { get; set; }

        /// <summary>
        /// The options for creating the outgoing cookie.
        /// May be replace or altered during the ResponseSignIn call.
        /// </summary>
        public CookieOptions CookieOptions { get; set; }
    }
}
