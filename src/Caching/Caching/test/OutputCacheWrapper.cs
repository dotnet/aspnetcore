// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OutputCaching;

namespace Microsoft.Extensions.Caching.Distributed.Tests;

// context: experiment for https://github.com/dotnet/aspnetcore/issues/44696
internal class OutputCacheWrapper(HybridCache underlying) : IOutputCacheStore
{
    ValueTask IOutputCacheStore.EvictByTagAsync(string tag, CancellationToken cancellationToken)
        => underlying.RemoveTagAsync(tag, cancellationToken);

    async ValueTask<byte[]?> IOutputCacheStore.GetAsync(string key, CancellationToken cancellationToken)
        => (await underlying.GetAsync<byte[]>(key, null, cancellationToken)).Value;

    ValueTask IOutputCacheStore.SetAsync(string key, byte[]? value, string[]? tags, TimeSpan validFor, CancellationToken cancellationToken)
        => value is null
            ? underlying.RemoveKeyAsync(key, cancellationToken)
            : underlying.SetAsync<byte[]>(key, value, new HybridCacheEntryOptions(validFor), tags, cancellationToken);
}
