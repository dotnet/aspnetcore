// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.HttpsPolicy
{
    /// <summary>
    /// Options for the HttpsRedirection middleware
    /// </summary>
    public class HttpsRedirectionOptions
    {
        /// <summary>
        /// The status code to redirect the response to.
        /// </summary>
        public int RedirectStatusCode { get; set; } = StatusCodes.Status302Found;

        /// <summary>
        /// The HTTPS port to be added to the redirected URL.
        /// </summary>
        /// <remarks>
        /// If the HttpsPort is not set, we will try to get the HttpsPort from the following:
        /// 1. HTTPS_PORT environment variable
        /// 2. IServerAddressesFeature
        /// 3. 443 (or not set) 
        /// </remarks>
        public int? HttpsPort { get; set; }
    }
}
