// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Caching.Memory
{
    public class MemoryDistributedCacheOptions : MemoryCacheOptions
    {
        public MemoryDistributedCacheOptions()
            : base()
        {
            // Default size limit of 200 MB
            SizeLimit = 200 * 1024 * 1024;
        }
    }
}