// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.ResponseCaching
{
    // TODO: Temporary interface for endpoints to specify options for response caching
    public class ResponseCacheFeature
    {
        public StringValues VaryByParams { get; set; }
    }
}
