// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Net.Http.Headers
{
    // RFC Draft: https://tools.ietf.org/html/draft-ietf-httpbis-cookie-same-site-00
    public enum SameSiteMode
    {
        None = 0,
        Lax,
        Strict
    }
}
