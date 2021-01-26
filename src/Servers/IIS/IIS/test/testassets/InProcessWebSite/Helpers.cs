// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace TestSite
{
    public static class Helpers
    {
        internal static bool? CanHaveBody(this HttpRequest request)
        {
#if FORWARDCOMPAT
            return null;
#else
            return request.HttpContext.Features.Get<IHttpRequestBodyDetectionFeature>()?.CanHaveBody;
#endif
        }
    }
}
