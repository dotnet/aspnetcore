// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Http.Authentication
{
    /// <summary>
    /// Used to store the results of an Authenticate call.
    /// </summary>
    public class AuthenticateInfo
    {
        /// <summary>
        /// The <see cref="ClaimsPrincipal"/>.
        /// </summary>
        public ClaimsPrincipal Principal { get; set; }

        /// <summary>
        /// The <see cref="AuthenticationProperties"/>.
        /// </summary>
        public AuthenticationProperties Properties { get; set; }

        /// <summary>
        /// The <see cref="AuthenticationDescription"/>.
        /// </summary>
        public AuthenticationDescription Description { get; set; }
    }
}
