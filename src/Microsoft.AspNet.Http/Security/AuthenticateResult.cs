// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Security.Principal;

namespace Microsoft.AspNet.Http.Security
{
    /// <summary>
    /// Acts as the return value from calls to the IAuthenticationManager's AuthenticeAsync methods.
    /// </summary>
    public class AuthenticationResult
    {
        /// <summary>
        /// Create an instance of the result object
        /// </summary>
        /// <param name="identity">Assigned to Identity. May be null.</param>
        /// <param name="properties">Assigned to Properties. Contains extra information carried along with the identity.</param>
        /// <param name="description">Assigned to Description. Contains information describing the authentication provider.</param>
        public AuthenticationResult(IIdentity identity, [NotNull] AuthenticationProperties properties, [NotNull] AuthenticationDescription description)
        {
            if (identity != null)
            {
                Identity = identity as ClaimsIdentity ?? new ClaimsIdentity(identity);
            }
            Properties = properties;
            Description = description;
        }

        /// <summary>
        /// Contains the claims that were authenticated by the given AuthenticationType. If the authentication
        /// type was not successful the Identity property will be null.
        /// </summary>
        public ClaimsIdentity Identity { get; private set; }

        /// <summary>
        /// Contains extra values that were provided with the original SignIn call.
        /// </summary>
        public AuthenticationProperties Properties { get; private set; }

        /// <summary>
        /// Contains description properties for the middleware authentication type in general. Does not
        /// vary per request.
        /// </summary>
        public AuthenticationDescription Description { get; private set; }
    }
}
