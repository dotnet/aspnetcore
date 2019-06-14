# Microsoft.AspNetCore.ResponseCaching.Internal

``` diff
-namespace Microsoft.AspNetCore.ResponseCaching.Internal {
 {
-    public class CachedResponse : IResponseCacheEntry {
 {
-        public CachedResponse();

-        public Stream Body { get; set; }

-        public DateTimeOffset Created { get; set; }

-        public IHeaderDictionary Headers { get; set; }

-        public int StatusCode { get; set; }

-    }
-    public class CachedVaryByRules : IResponseCacheEntry {
 {
-        public CachedVaryByRules();

-        public StringValues Headers { get; set; }

-        public StringValues QueryKeys { get; set; }

-        public string VaryByKeyPrefix { get; set; }

-    }
-    public interface IResponseCache {
 {
-        IResponseCacheEntry Get(string key);

-        Task<IResponseCacheEntry> GetAsync(string key);

-        void Set(string key, IResponseCacheEntry entry, TimeSpan validFor);

-        Task SetAsync(string key, IResponseCacheEntry entry, TimeSpan validFor);

-    }
-    public interface IResponseCacheEntry

-    public interface IResponseCachingKeyProvider {
 {
-        string CreateBaseKey(ResponseCachingContext context);

-        IEnumerable<string> CreateLookupVaryByKeys(ResponseCachingContext context);

-        string CreateStorageVaryByKey(ResponseCachingContext context);

-    }
-    public interface IResponseCachingPolicyProvider {
 {
-        bool AllowCacheLookup(ResponseCachingContext context);

-        bool AllowCacheStorage(ResponseCachingContext context);

-        bool AttemptResponseCaching(ResponseCachingContext context);

-        bool IsCachedEntryFresh(ResponseCachingContext context);

-        bool IsResponseCacheable(ResponseCachingContext context);

-    }
-    public class MemoryResponseCache : IResponseCache {
 {
-        public MemoryResponseCache(IMemoryCache cache);

-        public IResponseCacheEntry Get(string key);

-        public Task<IResponseCacheEntry> GetAsync(string key);

-        public void Set(string key, IResponseCacheEntry entry, TimeSpan validFor);

-        public Task SetAsync(string key, IResponseCacheEntry entry, TimeSpan validFor);

-    }
-    public class ResponseCachingContext {
 {
-        public Nullable<TimeSpan> CachedEntryAge { get; internal set; }

-        public CachedVaryByRules CachedVaryByRules { get; internal set; }

-        public HttpContext HttpContext { get; }

-        public Nullable<DateTimeOffset> ResponseTime { get; internal set; }

-    }
-    public class ResponseCachingKeyProvider : IResponseCachingKeyProvider {
 {
-        public ResponseCachingKeyProvider(ObjectPoolProvider poolProvider, IOptions<ResponseCachingOptions> options);

-        public string CreateBaseKey(ResponseCachingContext context);

-        public IEnumerable<string> CreateLookupVaryByKeys(ResponseCachingContext context);

-        public string CreateStorageVaryByKey(ResponseCachingContext context);

-    }
-    public class ResponseCachingPolicyProvider : IResponseCachingPolicyProvider {
 {
-        public ResponseCachingPolicyProvider();

-        public virtual bool AllowCacheLookup(ResponseCachingContext context);

-        public virtual bool AllowCacheStorage(ResponseCachingContext context);

-        public virtual bool AttemptResponseCaching(ResponseCachingContext context);

-        public virtual bool IsCachedEntryFresh(ResponseCachingContext context);

-        public virtual bool IsResponseCacheable(ResponseCachingContext context);

-    }
-}
```

