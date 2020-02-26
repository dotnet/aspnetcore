// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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