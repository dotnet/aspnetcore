// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http
{
    // RFC Draft: https://tools.ietf.org/html/draft-ietf-httpbis-cookie-same-site-00
    // This mirrors Microsoft.Net.Http.Headers.SameSiteMode
    public enum SameSiteMode
    {
        None = 0,
        Lax,
        Strict
    }
}
