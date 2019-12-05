// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Caching.Distributed
{
    public static partial class DistributedCacheEntryExtensions
    {
        public static Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions SetAbsoluteExpiration(this Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions options, System.DateTimeOffset absolute) { throw null; }
        public static Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions SetAbsoluteExpiration(this Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions options, System.TimeSpan relative) { throw null; }
        public static Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions SetSlidingExpiration(this Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions options, System.TimeSpan offset) { throw null; }
    }
    public partial class DistributedCacheEntryOptions
    {
        public DistributedCacheEntryOptions() { }
        public System.DateTimeOffset? AbsoluteExpiration { get { throw null; } set { } }
        public System.TimeSpan? AbsoluteExpirationRelativeToNow { get { throw null; } set { } }
        public System.TimeSpan? SlidingExpiration { get { throw null; } set { } }
    }
    public static partial class DistributedCacheExtensions
    {
        public static string GetString(this Microsoft.Extensions.Caching.Distributed.IDistributedCache cache, string key) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.Task<string> GetStringAsync(this Microsoft.Extensions.Caching.Distributed.IDistributedCache cache, string key, System.Threading.CancellationToken token = default(System.Threading.CancellationToken)) { throw null; }
        public static void Set(this Microsoft.Extensions.Caching.Distributed.IDistributedCache cache, string key, byte[] value) { }
        public static System.Threading.Tasks.Task SetAsync(this Microsoft.Extensions.Caching.Distributed.IDistributedCache cache, string key, byte[] value, System.Threading.CancellationToken token = default(System.Threading.CancellationToken)) { throw null; }
        public static void SetString(this Microsoft.Extensions.Caching.Distributed.IDistributedCache cache, string key, string value) { }
        public static void SetString(this Microsoft.Extensions.Caching.Distributed.IDistributedCache cache, string key, string value, Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions options) { }
        public static System.Threading.Tasks.Task SetStringAsync(this Microsoft.Extensions.Caching.Distributed.IDistributedCache cache, string key, string value, Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions options, System.Threading.CancellationToken token = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task SetStringAsync(this Microsoft.Extensions.Caching.Distributed.IDistributedCache cache, string key, string value, System.Threading.CancellationToken token = default(System.Threading.CancellationToken)) { throw null; }
    }
    public partial interface IDistributedCache
    {
        byte[] Get(string key);
        System.Threading.Tasks.Task<byte[]> GetAsync(string key, System.Threading.CancellationToken token = default(System.Threading.CancellationToken));
        void Refresh(string key);
        System.Threading.Tasks.Task RefreshAsync(string key, System.Threading.CancellationToken token = default(System.Threading.CancellationToken));
        void Remove(string key);
        System.Threading.Tasks.Task RemoveAsync(string key, System.Threading.CancellationToken token = default(System.Threading.CancellationToken));
        void Set(string key, byte[] value, Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions options);
        System.Threading.Tasks.Task SetAsync(string key, byte[] value, Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions options, System.Threading.CancellationToken token = default(System.Threading.CancellationToken));
    }
}
namespace Microsoft.Extensions.Caching.Memory
{
    public static partial class CacheEntryExtensions
    {
        public static Microsoft.Extensions.Caching.Memory.ICacheEntry AddExpirationToken(this Microsoft.Extensions.Caching.Memory.ICacheEntry entry, Microsoft.Extensions.Primitives.IChangeToken expirationToken) { throw null; }
        public static Microsoft.Extensions.Caching.Memory.ICacheEntry RegisterPostEvictionCallback(this Microsoft.Extensions.Caching.Memory.ICacheEntry entry, Microsoft.Extensions.Caching.Memory.PostEvictionDelegate callback) { throw null; }
        public static Microsoft.Extensions.Caching.Memory.ICacheEntry RegisterPostEvictionCallback(this Microsoft.Extensions.Caching.Memory.ICacheEntry entry, Microsoft.Extensions.Caching.Memory.PostEvictionDelegate callback, object state) { throw null; }
        public static Microsoft.Extensions.Caching.Memory.ICacheEntry SetAbsoluteExpiration(this Microsoft.Extensions.Caching.Memory.ICacheEntry entry, System.DateTimeOffset absolute) { throw null; }
        public static Microsoft.Extensions.Caching.Memory.ICacheEntry SetAbsoluteExpiration(this Microsoft.Extensions.Caching.Memory.ICacheEntry entry, System.TimeSpan relative) { throw null; }
        public static Microsoft.Extensions.Caching.Memory.ICacheEntry SetOptions(this Microsoft.Extensions.Caching.Memory.ICacheEntry entry, Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions options) { throw null; }
        public static Microsoft.Extensions.Caching.Memory.ICacheEntry SetPriority(this Microsoft.Extensions.Caching.Memory.ICacheEntry entry, Microsoft.Extensions.Caching.Memory.CacheItemPriority priority) { throw null; }
        public static Microsoft.Extensions.Caching.Memory.ICacheEntry SetSize(this Microsoft.Extensions.Caching.Memory.ICacheEntry entry, long size) { throw null; }
        public static Microsoft.Extensions.Caching.Memory.ICacheEntry SetSlidingExpiration(this Microsoft.Extensions.Caching.Memory.ICacheEntry entry, System.TimeSpan offset) { throw null; }
        public static Microsoft.Extensions.Caching.Memory.ICacheEntry SetValue(this Microsoft.Extensions.Caching.Memory.ICacheEntry entry, object value) { throw null; }
    }
    public static partial class CacheExtensions
    {
        public static object Get(this Microsoft.Extensions.Caching.Memory.IMemoryCache cache, object key) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.Task<TItem> GetOrCreateAsync<TItem>(this Microsoft.Extensions.Caching.Memory.IMemoryCache cache, object key, System.Func<Microsoft.Extensions.Caching.Memory.ICacheEntry, System.Threading.Tasks.Task<TItem>> factory) { throw null; }
        public static TItem GetOrCreate<TItem>(this Microsoft.Extensions.Caching.Memory.IMemoryCache cache, object key, System.Func<Microsoft.Extensions.Caching.Memory.ICacheEntry, TItem> factory) { throw null; }
        public static TItem Get<TItem>(this Microsoft.Extensions.Caching.Memory.IMemoryCache cache, object key) { throw null; }
        public static TItem Set<TItem>(this Microsoft.Extensions.Caching.Memory.IMemoryCache cache, object key, TItem value) { throw null; }
        public static TItem Set<TItem>(this Microsoft.Extensions.Caching.Memory.IMemoryCache cache, object key, TItem value, Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions options) { throw null; }
        public static TItem Set<TItem>(this Microsoft.Extensions.Caching.Memory.IMemoryCache cache, object key, TItem value, Microsoft.Extensions.Primitives.IChangeToken expirationToken) { throw null; }
        public static TItem Set<TItem>(this Microsoft.Extensions.Caching.Memory.IMemoryCache cache, object key, TItem value, System.DateTimeOffset absoluteExpiration) { throw null; }
        public static TItem Set<TItem>(this Microsoft.Extensions.Caching.Memory.IMemoryCache cache, object key, TItem value, System.TimeSpan absoluteExpirationRelativeToNow) { throw null; }
        public static bool TryGetValue<TItem>(this Microsoft.Extensions.Caching.Memory.IMemoryCache cache, object key, out TItem value) { throw null; }
    }
    public enum CacheItemPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        NeverRemove = 3,
    }
    public enum EvictionReason
    {
        None = 0,
        Removed = 1,
        Replaced = 2,
        Expired = 3,
        TokenExpired = 4,
        Capacity = 5,
    }
    public partial interface ICacheEntry : System.IDisposable
    {
        System.DateTimeOffset? AbsoluteExpiration { get; set; }
        System.TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
        System.Collections.Generic.IList<Microsoft.Extensions.Primitives.IChangeToken> ExpirationTokens { get; }
        object Key { get; }
        System.Collections.Generic.IList<Microsoft.Extensions.Caching.Memory.PostEvictionCallbackRegistration> PostEvictionCallbacks { get; }
        Microsoft.Extensions.Caching.Memory.CacheItemPriority Priority { get; set; }
        long? Size { get; set; }
        System.TimeSpan? SlidingExpiration { get; set; }
        object Value { get; set; }
    }
    public partial interface IMemoryCache : System.IDisposable
    {
        Microsoft.Extensions.Caching.Memory.ICacheEntry CreateEntry(object key);
        void Remove(object key);
        bool TryGetValue(object key, out object value);
    }
    public static partial class MemoryCacheEntryExtensions
    {
        public static Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions AddExpirationToken(this Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions options, Microsoft.Extensions.Primitives.IChangeToken expirationToken) { throw null; }
        public static Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions RegisterPostEvictionCallback(this Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions options, Microsoft.Extensions.Caching.Memory.PostEvictionDelegate callback) { throw null; }
        public static Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions RegisterPostEvictionCallback(this Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions options, Microsoft.Extensions.Caching.Memory.PostEvictionDelegate callback, object state) { throw null; }
        public static Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions SetAbsoluteExpiration(this Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions options, System.DateTimeOffset absolute) { throw null; }
        public static Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions SetAbsoluteExpiration(this Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions options, System.TimeSpan relative) { throw null; }
        public static Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions SetPriority(this Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions options, Microsoft.Extensions.Caching.Memory.CacheItemPriority priority) { throw null; }
        public static Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions SetSize(this Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions options, long size) { throw null; }
        public static Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions SetSlidingExpiration(this Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions options, System.TimeSpan offset) { throw null; }
    }
    public partial class MemoryCacheEntryOptions
    {
        public MemoryCacheEntryOptions() { }
        public System.DateTimeOffset? AbsoluteExpiration { get { throw null; } set { } }
        public System.TimeSpan? AbsoluteExpirationRelativeToNow { get { throw null; } set { } }
        public System.Collections.Generic.IList<Microsoft.Extensions.Primitives.IChangeToken> ExpirationTokens { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.Extensions.Caching.Memory.PostEvictionCallbackRegistration> PostEvictionCallbacks { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.Extensions.Caching.Memory.CacheItemPriority Priority { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public long? Size { get { throw null; } set { } }
        public System.TimeSpan? SlidingExpiration { get { throw null; } set { } }
    }
    public partial class PostEvictionCallbackRegistration
    {
        public PostEvictionCallbackRegistration() { }
        public Microsoft.Extensions.Caching.Memory.PostEvictionDelegate EvictionCallback { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public object State { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public delegate void PostEvictionDelegate(object key, object value, Microsoft.Extensions.Caching.Memory.EvictionReason reason, object state);
}
namespace Microsoft.Extensions.Internal
{
    public partial interface ISystemClock
    {
        System.DateTimeOffset UtcNow { get; }
    }
    public partial class SystemClock : Microsoft.Extensions.Internal.ISystemClock
    {
        public SystemClock() { }
        public System.DateTimeOffset UtcNow { get { throw null; } }
    }
}
