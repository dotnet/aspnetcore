// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Provides failure context information to handler providers.
    /// </summary>
    public class RemoteFailureContext : HandleRequestContext<RemoteAuthenticationOptions>
    {
        public RemoteFailureContext(
            HttpContext context,
            AuthenticationScheme scheme,
            RemoteAuthenticationOptions options,
            Exception failure)
            : base(context, scheme, options)
        {
            Failure = failure;
        }

        /// <summary>
        /// User friendly error message for the error.
        /// </summary>
        public Exception Failure { get; set; }

        /// <summary>
        /// Additional state values for the authentication session.
        /// </summary>
        public AuthenticationProperties Properties { get; set; }
    }
}
