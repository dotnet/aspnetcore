// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Authentication.Notifications;

namespace Microsoft.AspNet.Authentication.Cookies
{
    /// <summary>
    /// Context object passed to the ICookieAuthenticationProvider method ValidatePrincipal.
    /// </summary>
    public class CookieValidatePrincipalContext : BaseContext<CookieAuthenticationOptions>
    {
        /// <summary>
        /// Creates a new instance of the context object.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="ticket">Contains the initial values for identity and extra data</param>
        /// <param name="options"></param>
        public CookieValidatePrincipalContext([NotNull] HttpContext context, [NotNull] AuthenticationTicket ticket, [NotNull] CookieAuthenticationOptions options)
            : base(context, options)
        {
            Principal = ticket.Principal;
            Properties = ticket.Properties;
        }

        /// <summary>
        /// Contains the claims principal arriving with the request. May be altered to change the 
        /// details of the authenticated user.
        /// </summary>
        public ClaimsPrincipal Principal { get; private set; }

        /// <summary>
        /// Contains the extra meta-data arriving with the request ticket. May be altered.
        /// </summary>
        public AuthenticationProperties Properties { get; private set; }

        /// <summary>
        /// Called to replace the claims principal. The supplied principal will replace the value of the 
        /// Principal property, which determines the identity of the authenticated request.
        /// </summary>
        /// <param name="identity">The identity used as the replacement</param>
        public void ReplacePrincipal(IPrincipal principal)
        {
            Principal = new ClaimsPrincipal(principal);
        }

        /// <summary>
        /// Called to reject the incoming principal. This may be done if the application has determined the
        /// account is no longer active, and the request should be treated as if it was anonymous.
        /// </summary>
        public void RejectPrincipal()
        {
            Principal = null;
        }
    }
}
