// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Base class used by <see cref="IAuthenticationHandler"/> methods.
    /// </summary>
    public class AuthenticateContext : BaseAuthenticationContext
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <param name="authenticationScheme">The name of the authentication scheme.</param>
        public AuthenticateContext(HttpContext context, string authenticationScheme) : base(context, authenticationScheme, properties: null)
        { }
    }
}
