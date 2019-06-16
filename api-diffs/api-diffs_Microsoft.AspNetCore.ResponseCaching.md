# Microsoft.AspNetCore.ResponseCaching

``` diff
 namespace Microsoft.AspNetCore.ResponseCaching {
     public interface IResponseCachingFeature {
         string[] VaryByQueryKeys { get; set; }
     }
     public class ResponseCachingFeature : IResponseCachingFeature {
         public ResponseCachingFeature();
         public string[] VaryByQueryKeys { get; set; }
     }
     public class ResponseCachingMiddleware {
-        public ResponseCachingMiddleware(RequestDelegate next, IOptions<ResponseCachingOptions> options, ILoggerFactory loggerFactory, IResponseCachingPolicyProvider policyProvider, IResponseCachingKeyProvider keyProvider);

+        public ResponseCachingMiddleware(RequestDelegate next, IOptions<ResponseCachingOptions> options, ILoggerFactory loggerFactory, ObjectPoolProvider poolProvider);
         public Task Invoke(HttpContext httpContext);
     }
     public class ResponseCachingOptions {
         public ResponseCachingOptions();
         public long MaximumBodySize { get; set; }
         public long SizeLimit { get; set; }
         public bool UseCaseSensitivePaths { get; set; }
     }
 }
```

