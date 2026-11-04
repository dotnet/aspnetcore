// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Http;

internal static class RequestExtensions
{
    internal static bool? CanHaveBody(this HttpRequest request)
    {
        return request.HttpContext.Features.Get<IHttpRequestBodyDetectionFeature>()?.CanHaveBody;
    }
}
