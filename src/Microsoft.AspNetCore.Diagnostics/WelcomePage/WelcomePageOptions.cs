// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Options for the WelcomePageMiddleware.
    /// </summary>
    public class WelcomePageOptions
    {
        /// <summary>
        /// Specifies which requests paths will be responded to. Exact matches only. Leave null to handle all requests.
        /// </summary>
        public PathString Path { get; set; }
    }
}
