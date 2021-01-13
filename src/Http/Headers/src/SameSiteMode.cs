// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Net.Http.Headers
{
    /// <summary>
    /// Indicates if the client should include a cookie on "same-site" or "cross-site" requests.
    /// RFC Draft: https://tools.ietf.org/html/draft-ietf-httpbis-rfc6265bis-03#section-4.1.1
    /// </summary>
    // This mirrors Microsoft.AspNetCore.Http.SameSiteMode
    public enum SameSiteMode
    {
        /// <summary>No SameSite field will be set, the client should follow its default cookie policy.</summary>
        Unspecified = -1,
        /// <summary>Indicates the client should disable same-site restrictions.</summary>
        None = 0,
        /// <summary>Indicates the client should send the cookie with "same-site" requests, and with "cross-site" top-level navigations.</summary>
        Lax,
        /// <summary>Indicates the client should only send the cookie with "same-site" requests.</summary>
        Strict
    }
}
