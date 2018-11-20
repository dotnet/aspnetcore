// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Contains user identity information as well as additional authentication state.
    /// </summary>
    public class AuthenticationTicket
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationTicket"/> class
        /// </summary>
        /// <param name="principal">the <see cref="ClaimsPrincipal"/> that represents the authenticated user.</param>
        /// <param name="properties">additional properties that can be consumed by the user or runtime.</param>
        /// <param name="authenticationScheme">the authentication middleware that was responsible for this ticket.</param>
        public AuthenticationTicket(ClaimsPrincipal principal, AuthenticationProperties properties, string authenticationScheme)
        {
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }

            AuthenticationScheme = authenticationScheme;
            Principal = principal;
            Properties = properties ?? new AuthenticationProperties();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationTicket"/> class
        /// </summary>
        /// <param name="principal">the <see cref="ClaimsPrincipal"/> that represents the authenticated user.</param>
        /// <param name="authenticationScheme">the authentication middleware that was responsible for this ticket.</param>
        public AuthenticationTicket(ClaimsPrincipal principal, string authenticationScheme) 
            : this(principal, properties: null, authenticationScheme: authenticationScheme)
        { }

        /// <summary>
        /// Gets the authentication type.
        /// </summary>
        public string AuthenticationScheme { get; private set; }

        /// <summary>
        /// Gets the claims-principal with authenticated user identities.
        /// </summary>
        public ClaimsPrincipal Principal { get; private set; }

        /// <summary>
        /// Additional state values for the authentication session.
        /// </summary>
        public AuthenticationProperties Properties { get; private set; }
    }
}
