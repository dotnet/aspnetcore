# Microsoft.AspNetCore.Mvc.TagHelpers.Cache

``` diff
 namespace Microsoft.AspNetCore.Mvc.TagHelpers.Cache {
     public class CacheTagKey : IEquatable<CacheTagKey> {
         public CacheTagKey(CacheTagHelper tagHelper, TagHelperContext context);
         public CacheTagKey(DistributedCacheTagHelper tagHelper);
         public bool Equals(CacheTagKey other);
         public override bool Equals(object obj);
         public string GenerateHashedKey();
         public string GenerateKey();
         public override int GetHashCode();
     }
     public class DistributedCacheTagHelperFormatter : IDistributedCacheTagHelperFormatter {
         public DistributedCacheTagHelperFormatter();
         public Task<HtmlString> DeserializeAsync(byte[] value);
         public Task<byte[]> SerializeAsync(DistributedCacheTagHelperFormattingContext context);
     }
     public class DistributedCacheTagHelperFormattingContext {
         public DistributedCacheTagHelperFormattingContext();
         public HtmlString Html { get; set; }
     }
     public class DistributedCacheTagHelperService : IDistributedCacheTagHelperService {
         public DistributedCacheTagHelperService(IDistributedCacheTagHelperStorage storage, IDistributedCacheTagHelperFormatter formatter, HtmlEncoder HtmlEncoder, ILoggerFactory loggerFactory);
         public Task<IHtmlContent> ProcessContentAsync(TagHelperOutput output, CacheTagKey key, DistributedCacheEntryOptions options);
     }
     public class DistributedCacheTagHelperStorage : IDistributedCacheTagHelperStorage {
         public DistributedCacheTagHelperStorage(IDistributedCache distributedCache);
         public Task<byte[]> GetAsync(string key);
         public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options);
     }
     public interface IDistributedCacheTagHelperFormatter {
         Task<HtmlString> DeserializeAsync(byte[] value);
         Task<byte[]> SerializeAsync(DistributedCacheTagHelperFormattingContext context);
     }
     public interface IDistributedCacheTagHelperService {
         Task<IHtmlContent> ProcessContentAsync(TagHelperOutput output, CacheTagKey key, DistributedCacheEntryOptions options);
     }
     public interface IDistributedCacheTagHelperStorage {
         Task<byte[]> GetAsync(string key);
         Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options);
     }
 }
```

