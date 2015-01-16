// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using Microsoft.AspNet.Http.Security;

namespace Microsoft.AspNet.Security
{
    /// <summary>
    /// Contains user identity information as well as additional authentication state.
    /// </summary>
    public class AuthenticationTicket
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationTicket"/> class
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="properties"></param>
        public AuthenticationTicket(ClaimsIdentity identity, AuthenticationProperties properties)
        {
            Identity = identity;
            Properties = properties ?? new AuthenticationProperties();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationTicket"/> class
        /// </summary>
        /// <param name="identity">the <see cref="ClaimsPrincipal"/> that represents the authenticated user.</param>
        /// <param name="properties">additional properties that can be consumed by the user or runtime.</param>
        /// <param name="authenticationType">the authentication middleware that was responsible for this ticket.</param>
        public AuthenticationTicket(ClaimsPrincipal principal, AuthenticationProperties properties, string authenticationType)
        {
            AuthenticationType = authenticationType;
            Principal = principal;
            Properties = properties ?? new AuthenticationProperties();
        }

        /// <summary>
        /// Gets the authentication type.
        /// </summary>
        public string AuthenticationType { get; private set; }

        /// <summary>
        /// Gets the authenticated user identity.
        /// </summary>
        public ClaimsIdentity Identity { get; private set; }

        /// <summary>
        /// Gets the claims-principal with authenticated user identities.
        /// </summary>
        public ClaimsPrincipal Principal{ get; private set; }

        /// <summary>
        /// Additional state values for the authentication session.
        /// </summary>
        public AuthenticationProperties Properties { get; private set; }
    }
}
