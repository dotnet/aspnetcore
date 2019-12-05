// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Caching.Distributed
{
    public partial class MemoryDistributedCache : Microsoft.Extensions.Caching.Distributed.IDistributedCache
    {
        public MemoryDistributedCache(Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions> optionsAccessor) { }
        public MemoryDistributedCache(Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions> optionsAccessor, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public byte[] Get(string key) { throw null; }
        public System.Threading.Tasks.Task<byte[]> GetAsync(string key, System.Threading.CancellationToken token = default(System.Threading.CancellationToken)) { throw null; }
        public void Refresh(string key) { }
        public System.Threading.Tasks.Task RefreshAsync(string key, System.Threading.CancellationToken token = default(System.Threading.CancellationToken)) { throw null; }
        public void Remove(string key) { }
        public System.Threading.Tasks.Task RemoveAsync(string key, System.Threading.CancellationToken token = default(System.Threading.CancellationToken)) { throw null; }
        public void Set(string key, byte[] value, Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions options) { }
        public System.Threading.Tasks.Task SetAsync(string key, byte[] value, Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions options, System.Threading.CancellationToken token = default(System.Threading.CancellationToken)) { throw null; }
    }
}
namespace Microsoft.Extensions.Caching.Memory
{
    public partial class MemoryCache : Microsoft.Extensions.Caching.Memory.IMemoryCache, System.IDisposable
    {
        public MemoryCache(Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Caching.Memory.MemoryCacheOptions> optionsAccessor) { }
        public MemoryCache(Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Caching.Memory.MemoryCacheOptions> optionsAccessor, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public int Count { get { throw null; } }
        public void Compact(double percentage) { }
        public Microsoft.Extensions.Caching.Memory.ICacheEntry CreateEntry(object key) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~MemoryCache() { }
        public void Remove(object key) { }
        public bool TryGetValue(object key, out object result) { throw null; }
    }
    public partial class MemoryCacheOptions : Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Caching.Memory.MemoryCacheOptions>
    {
        public MemoryCacheOptions() { }
        public Microsoft.Extensions.Internal.ISystemClock Clock { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public double CompactionPercentage { get { throw null; } set { } }
        public System.TimeSpan ExpirationScanFrequency { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        Microsoft.Extensions.Caching.Memory.MemoryCacheOptions Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Caching.Memory.MemoryCacheOptions>.Value { get { throw null; } }
        public long? SizeLimit { get { throw null; } set { } }
    }
    public partial class MemoryDistributedCacheOptions : Microsoft.Extensions.Caching.Memory.MemoryCacheOptions
    {
        public MemoryDistributedCacheOptions() { }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class MemoryCacheServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddDistributedMemoryCache(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddDistributedMemoryCache(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions> setupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddMemoryCache(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddMemoryCache(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.Extensions.Caching.Memory.MemoryCacheOptions> setupAction) { throw null; }
    }
}
