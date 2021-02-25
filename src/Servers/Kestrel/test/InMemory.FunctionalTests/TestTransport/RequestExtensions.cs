// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Http
{
    internal static class RequestExtensions
    {
        internal static bool? CanHaveBody(this HttpRequest request)
        {
            return request.HttpContext.Features.Get<IHttpRequestBodyDetectionFeature>()?.CanHaveBody;
        }
    }
}
