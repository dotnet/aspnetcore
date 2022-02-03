// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.TestHost;

internal static class Utilities
{
    internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    internal static bool? CanHaveBody(this HttpRequest request)
    {
        return request.HttpContext.Features.Get<IHttpRequestBodyDetectionFeature>()?.CanHaveBody;
    }
}
