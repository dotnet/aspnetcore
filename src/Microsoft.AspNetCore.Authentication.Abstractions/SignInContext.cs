// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Context used for sign out.
    /// </summary>
    public class SignInContext : BaseAuthenticationContext
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="authenticationScheme">The name of the authentication scheme.</param>
        /// <param name="principal">The user to sign in.</param>
        /// <param name="properties">The properties.</param>
        public SignInContext(HttpContext context, string authenticationScheme, ClaimsPrincipal principal, AuthenticationProperties properties)
            : base(context, authenticationScheme, properties)
        {
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }
            Principal = principal;
        }

        /// <summary>
        /// The user to sign in.
        /// </summary>
        public ClaimsPrincipal Principal { get; }
    }
}