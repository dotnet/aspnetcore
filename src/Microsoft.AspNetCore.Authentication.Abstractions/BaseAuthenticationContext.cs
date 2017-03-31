// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Base context for authentication.
    /// </summary>
    public abstract class BaseAuthenticationContext : BaseContext
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="authenticationScheme">The name of the scheme.</param>
        /// <param name="properties">The properties.</param>
        protected BaseAuthenticationContext(HttpContext context, string authenticationScheme, AuthenticationProperties properties) : base(context)
        {
            if (string.IsNullOrEmpty(authenticationScheme))
            {
                throw new ArgumentException(nameof(authenticationScheme));
            }

            AuthenticationScheme = authenticationScheme;
            Properties = properties ?? new AuthenticationProperties();
        }

        /// <summary>
        /// The name of the scheme.
        /// </summary>
        public string AuthenticationScheme { get; }

        /// <summary>
        /// Contains the extra meta-data arriving with the authentication. May be altered.
        /// </summary>
        public AuthenticationProperties Properties { get; protected set; }
    }
}
