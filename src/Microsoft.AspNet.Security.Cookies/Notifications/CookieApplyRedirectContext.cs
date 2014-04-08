// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Security.Notifications;

namespace Microsoft.AspNet.Security.Cookies
{
    /// <summary>
    /// Context passed when a Challenge, SignIn, or SignOut causes a redirect in the cookie middleware 
    /// </summary>
    public class CookieApplyRedirectContext : BaseContext<CookieAuthenticationOptions>
    {
        /// <summary>
        /// Creates a new context object.
        /// </summary>
        /// <param name="context">The OWIN request context</param>
        /// <param name="options">The cookie middleware options</param>
        /// <param name="redirectUri">The initial redirect URI</param>
        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "2#", Justification = "Represents header value")]
        public CookieApplyRedirectContext(HttpContext context, CookieAuthenticationOptions options, string redirectUri)
            : base(context, options)
        {
            RedirectUri = redirectUri;
        }

        /// <summary>
        /// Gets or Sets the URI used for the redirect operation.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Represents header value")]
        public string RedirectUri { get; set; }
    }
}
