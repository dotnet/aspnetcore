// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace TestSite;

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
