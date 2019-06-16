# Microsoft.Extensions.Caching.Memory

``` diff
 namespace Microsoft.Extensions.Caching.Memory {
     public static class CacheEntryExtensions {
         public static ICacheEntry AddExpirationToken(this ICacheEntry entry, IChangeToken expirationToken);
         public static ICacheEntry RegisterPostEvictionCallback(this ICacheEntry entry, PostEvictionDelegate callback);
         public static ICacheEntry RegisterPostEvictionCallback(this ICacheEntry entry, PostEvictionDelegate callback, object state);
         public static ICacheEntry SetAbsoluteExpiration(this ICacheEntry entry, DateTimeOffset absolute);
         public static ICacheEntry SetAbsoluteExpiration(this ICacheEntry entry, TimeSpan relative);
         public static ICacheEntry SetOptions(this ICacheEntry entry, MemoryCacheEntryOptions options);
         public static ICacheEntry SetPriority(this ICacheEntry entry, CacheItemPriority priority);
         public static ICacheEntry SetSize(this ICacheEntry entry, long size);
         public static ICacheEntry SetSlidingExpiration(this ICacheEntry entry, TimeSpan offset);
         public static ICacheEntry SetValue(this ICacheEntry entry, object value);
     }
     public static class CacheExtensions {
         public static object Get(this IMemoryCache cache, object key);
         public static TItem Get<TItem>(this IMemoryCache cache, object key);
         public static TItem GetOrCreate<TItem>(this IMemoryCache cache, object key, Func<ICacheEntry, TItem> factory);
         public static Task<TItem> GetOrCreateAsync<TItem>(this IMemoryCache cache, object key, Func<ICacheEntry, Task<TItem>> factory);
         public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value);
         public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, MemoryCacheEntryOptions options);
         public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, IChangeToken expirationToken);
         public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, DateTimeOffset absoluteExpiration);
         public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, TimeSpan absoluteExpirationRelativeToNow);
         public static bool TryGetValue<TItem>(this IMemoryCache cache, object key, out TItem value);
     }
     public enum CacheItemPriority {
         High = 2,
         Low = 0,
         NeverRemove = 3,
         Normal = 1,
     }
     public enum EvictionReason {
         Capacity = 5,
         Expired = 3,
         None = 0,
         Removed = 1,
         Replaced = 2,
         TokenExpired = 4,
     }
     public interface ICacheEntry : IDisposable {
         Nullable<DateTimeOffset> AbsoluteExpiration { get; set; }
         Nullable<TimeSpan> AbsoluteExpirationRelativeToNow { get; set; }
         IList<IChangeToken> ExpirationTokens { get; }
         object Key { get; }
         IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; }
         CacheItemPriority Priority { get; set; }
         Nullable<long> Size { get; set; }
         Nullable<TimeSpan> SlidingExpiration { get; set; }
         object Value { get; set; }
     }
     public interface IMemoryCache : IDisposable {
         ICacheEntry CreateEntry(object key);
         void Remove(object key);
         bool TryGetValue(object key, out object value);
     }
     public class MemoryCache : IDisposable, IMemoryCache {
         public MemoryCache(IOptions<MemoryCacheOptions> optionsAccessor);
+        public MemoryCache(IOptions<MemoryCacheOptions> optionsAccessor, ILoggerFactory loggerFactory);
         public int Count { get; }
         public void Compact(double percentage);
         public ICacheEntry CreateEntry(object key);
         public void Dispose();
         protected virtual void Dispose(bool disposing);
         ~MemoryCache();
         public void Remove(object key);
         public bool TryGetValue(object key, out object result);
     }
     public static class MemoryCacheEntryExtensions {
         public static MemoryCacheEntryOptions AddExpirationToken(this MemoryCacheEntryOptions options, IChangeToken expirationToken);
         public static MemoryCacheEntryOptions RegisterPostEvictionCallback(this MemoryCacheEntryOptions options, PostEvictionDelegate callback);
         public static MemoryCacheEntryOptions RegisterPostEvictionCallback(this MemoryCacheEntryOptions options, PostEvictionDelegate callback, object state);
         public static MemoryCacheEntryOptions SetAbsoluteExpiration(this MemoryCacheEntryOptions options, DateTimeOffset absolute);
         public static MemoryCacheEntryOptions SetAbsoluteExpiration(this MemoryCacheEntryOptions options, TimeSpan relative);
         public static MemoryCacheEntryOptions SetPriority(this MemoryCacheEntryOptions options, CacheItemPriority priority);
         public static MemoryCacheEntryOptions SetSize(this MemoryCacheEntryOptions options, long size);
         public static MemoryCacheEntryOptions SetSlidingExpiration(this MemoryCacheEntryOptions options, TimeSpan offset);
     }
     public class MemoryCacheEntryOptions {
         public MemoryCacheEntryOptions();
         public Nullable<DateTimeOffset> AbsoluteExpiration { get; set; }
         public Nullable<TimeSpan> AbsoluteExpirationRelativeToNow { get; set; }
         public IList<IChangeToken> ExpirationTokens { get; }
         public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; }
         public CacheItemPriority Priority { get; set; }
         public Nullable<long> Size { get; set; }
         public Nullable<TimeSpan> SlidingExpiration { get; set; }
     }
     public class MemoryCacheOptions : IOptions<MemoryCacheOptions> {
         public MemoryCacheOptions();
         public ISystemClock Clock { get; set; }
         public double CompactionPercentage { get; set; }
-        public bool CompactOnMemoryPressure { get; set; }

         public TimeSpan ExpirationScanFrequency { get; set; }
         MemoryCacheOptions Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Caching.Memory.MemoryCacheOptions>.Value { get; }
         public Nullable<long> SizeLimit { get; set; }
     }
     public class MemoryDistributedCacheOptions : MemoryCacheOptions {
         public MemoryDistributedCacheOptions();
     }
     public class PostEvictionCallbackRegistration {
         public PostEvictionCallbackRegistration();
         public PostEvictionDelegate EvictionCallback { get; set; }
         public object State { get; set; }
     }
     public delegate void PostEvictionDelegate(object key, object value, EvictionReason reason, object state);
 }
```

