// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.ResponseCaching.Internal
{
    public interface IResponseCache
    {
        IResponseCacheEntry Get(string key);
        Task<IResponseCacheEntry> GetAsync(string key);

        void Set(string key, IResponseCacheEntry entry, TimeSpan validFor);
        Task SetAsync(string key, IResponseCacheEntry entry, TimeSpan validFor);
    }
}
