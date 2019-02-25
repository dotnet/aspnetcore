// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Net.Http.Headers
{
    // RFC Draft: https://tools.ietf.org/html/draft-ietf-httpbis-cookie-same-site-00
    public enum SameSiteMode
    {
        /// <summary>The cookie will not be sent along with "same-site" requests, with "cross-site" top-level navigations</summary>
        None = 0,
        /// <summary>The cookie will be sent with "same-site" requests, and with "cross-site" top-level navigations</summary>
        Lax,
        /// <summary>The cookie will only be sent along with "same-site" requests</summary>
        Strict
    }
}
