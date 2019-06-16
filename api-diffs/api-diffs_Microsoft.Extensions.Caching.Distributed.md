# Microsoft.Extensions.Caching.Distributed

``` diff
 namespace Microsoft.Extensions.Caching.Distributed {
     public static class DistributedCacheEntryExtensions {
         public static DistributedCacheEntryOptions SetAbsoluteExpiration(this DistributedCacheEntryOptions options, DateTimeOffset absolute);
         public static DistributedCacheEntryOptions SetAbsoluteExpiration(this DistributedCacheEntryOptions options, TimeSpan relative);
         public static DistributedCacheEntryOptions SetSlidingExpiration(this DistributedCacheEntryOptions options, TimeSpan offset);
     }
     public class DistributedCacheEntryOptions {
         public DistributedCacheEntryOptions();
         public Nullable<DateTimeOffset> AbsoluteExpiration { get; set; }
         public Nullable<TimeSpan> AbsoluteExpirationRelativeToNow { get; set; }
         public Nullable<TimeSpan> SlidingExpiration { get; set; }
     }
     public static class DistributedCacheExtensions {
         public static string GetString(this IDistributedCache cache, string key);
         public static Task<string> GetStringAsync(this IDistributedCache cache, string key, CancellationToken token = default(CancellationToken));
         public static void Set(this IDistributedCache cache, string key, byte[] value);
         public static Task SetAsync(this IDistributedCache cache, string key, byte[] value, CancellationToken token = default(CancellationToken));
         public static void SetString(this IDistributedCache cache, string key, string value);
         public static void SetString(this IDistributedCache cache, string key, string value, DistributedCacheEntryOptions options);
         public static Task SetStringAsync(this IDistributedCache cache, string key, string value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken));
         public static Task SetStringAsync(this IDistributedCache cache, string key, string value, CancellationToken token = default(CancellationToken));
     }
     public interface IDistributedCache {
         byte[] Get(string key);
         Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken));
         void Refresh(string key);
         Task RefreshAsync(string key, CancellationToken token = default(CancellationToken));
         void Remove(string key);
         Task RemoveAsync(string key, CancellationToken token = default(CancellationToken));
         void Set(string key, byte[] value, DistributedCacheEntryOptions options);
         Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken));
     }
     public class MemoryDistributedCache : IDistributedCache {
         public MemoryDistributedCache(IOptions<MemoryDistributedCacheOptions> optionsAccessor);
+        public MemoryDistributedCache(IOptions<MemoryDistributedCacheOptions> optionsAccessor, ILoggerFactory loggerFactory);
         public byte[] Get(string key);
         public Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken));
         public void Refresh(string key);
         public Task RefreshAsync(string key, CancellationToken token = default(CancellationToken));
         public void Remove(string key);
         public Task RemoveAsync(string key, CancellationToken token = default(CancellationToken));
         public void Set(string key, byte[] value, DistributedCacheEntryOptions options);
         public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken));
     }
 }
```

