// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Authorization
{
    /// <summary>
    /// Provides programmatic configuration used by the <see cref="AuthorizationMiddleware"/>.
    /// </summary>
    public class AuthorizationMiddlewareOptions
    {
        /// <summary>
        /// Determines whether HttpContext will be passed as the resource to authorization instead of the matching endpoint.
        /// Defaults to false.
        /// </summary>
        public bool UseHttpContextAsResource { get; set; } = false;
    }
}
