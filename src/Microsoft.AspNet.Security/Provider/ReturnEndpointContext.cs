// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Security.Provider
{
    public abstract class ReturnEndpointContext : EndpointContext
    {
        protected ReturnEndpointContext(
            HttpContext context,
            AuthenticationTicket ticket)
            : base(context)
        {
            if (ticket != null)
            {
                Identity = ticket.Identity;
                Properties = ticket.Properties;
            }
        }

        public ClaimsIdentity Identity { get; set; }
        public AuthenticationProperties Properties { get; set; }

        public string SignInAsAuthenticationType { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "By design")]
        public string RedirectUri { get; set; }
    }
}
