// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.ResponseCaching
{
    internal class CachedResponse : IResponseCacheEntry
    {
        public DateTimeOffset Created { get; set; }

        public int StatusCode { get; set; }

        public IHeaderDictionary Headers { get; set; }

        public CachedResponseBody Body { get; set; }
    }
}
