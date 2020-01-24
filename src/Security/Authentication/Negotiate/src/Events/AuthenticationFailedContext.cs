// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Negotiate
{
    /// <summary>
    /// State for the AuthenticationFailed event.
    /// </summary>
    public class AuthenticationFailedContext : RemoteAuthenticationContext<NegotiateOptions>
    {
        /// <summary>
        /// Creates a <see cref="AuthenticationFailedContext"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="scheme"></param>
        /// <param name="options"></param>
        public AuthenticationFailedContext(
            HttpContext context,
            AuthenticationScheme scheme,
            NegotiateOptions options)
            : base(context, scheme, options, properties: null) { }

        /// <summary>
        /// The exception that occurred while processing the authentication.
        /// </summary>
        public Exception Exception { get; set; }
    }
}
