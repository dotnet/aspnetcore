// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Net.Http.Headers
{
    /// <summary>
    /// Determines whether to send a cookies on "same-site" or "cross-site" requests
    /// RFC Draft: https://tools.ietf.org/html/draft-ietf-httpbis-cookie-same-site-00
    /// </summary>
    /// <remarks>
    /// This mirrors Microsoft.AspNetCore.Http.SameSiteMode
    /// </remarks>
    public enum SameSiteMode
    {
        /// <summary>The cookie will not be sent along with "same-site" requests or with "cross-site" top-level navigations</summary>
        None = 0,
        /// <summary>The cookie will be sent with "same-site" requests, and with "cross-site" top-level navigations</summary>
        Lax,
        /// <summary>The cookie will only be sent along with "same-site" requests</summary>
        Strict
    }
}
