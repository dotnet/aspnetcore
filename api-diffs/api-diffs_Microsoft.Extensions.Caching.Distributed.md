# Microsoft.Extensions.Caching.Distributed

``` diff
 namespace Microsoft.Extensions.Caching.Distributed {
     public class MemoryDistributedCache : IDistributedCache {
+        public MemoryDistributedCache(IOptions<MemoryDistributedCacheOptions> optionsAccessor, ILoggerFactory loggerFactory);
     }
 }
```

