// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Context used to sign out.
    /// </summary>
    public class SignOutContext : BaseAuthenticationContext
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="authenticationScheme">The name of the authentication scheme.</param>
        /// <param name="properties">The properties.</param>
        public SignOutContext(HttpContext context, string authenticationScheme, AuthenticationProperties properties)
            : base(context, authenticationScheme, properties)
        { }
    }
}