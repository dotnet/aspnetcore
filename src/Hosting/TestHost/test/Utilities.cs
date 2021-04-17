// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.TestHost
{
    internal static class Utilities
    {
        internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

        internal static bool? CanHaveBody(this HttpRequest request)
        {
            return request.HttpContext.Features.Get<IHttpRequestBodyDetectionFeature>()?.CanHaveBody;
        }
    }
}
