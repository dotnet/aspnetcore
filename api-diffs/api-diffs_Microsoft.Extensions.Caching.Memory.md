# Microsoft.Extensions.Caching.Memory

``` diff
 namespace Microsoft.Extensions.Caching.Memory {
     public class MemoryCache : IDisposable, IMemoryCache {
+        public MemoryCache(IOptions<MemoryCacheOptions> optionsAccessor, ILoggerFactory loggerFactory);
     }
     public class MemoryCacheOptions : IOptions<MemoryCacheOptions> {
-        public bool CompactOnMemoryPressure { get; set; }

     }
 }
```

