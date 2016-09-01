// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.ResponseCaching.Internal
{
    internal class NoopCacheKeyModifier : IResponseCachingCacheKeyModifier
    {
        public string CreatKeyPrefix(HttpContext httpContext) => null;
    }
}
