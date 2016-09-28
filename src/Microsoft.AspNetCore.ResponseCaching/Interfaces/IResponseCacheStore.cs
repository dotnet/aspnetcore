// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.ResponseCaching
{
    public interface IResponseCacheStore
    {
        Task<IResponseCacheEntry> GetAsync(string key);
        Task SetAsync(string key, IResponseCacheEntry entry, TimeSpan validFor);
    }
}
