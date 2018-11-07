// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Caching.Distributed
{
    public interface IDistributedCache
    {
        byte[] Get(string key);

        Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken));

        void Set(string key, byte[] value, DistributedCacheEntryOptions options);

        Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken));

        void Refresh(string key);

        Task RefreshAsync(string key, CancellationToken token = default(CancellationToken));

        void Remove(string key);

        Task RemoveAsync(string key, CancellationToken token = default(CancellationToken));
    }
}