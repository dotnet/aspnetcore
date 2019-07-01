// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.ResponseCaching
{
    internal class CachedVaryByRules : IResponseCacheEntry
    {
        public string VaryByKeyPrefix { get; set; }

        public StringValues Headers { get; set; }

        public StringValues QueryKeys { get; set; }
    }
}
