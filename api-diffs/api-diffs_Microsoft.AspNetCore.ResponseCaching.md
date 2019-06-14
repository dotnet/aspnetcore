# Microsoft.AspNetCore.ResponseCaching

``` diff
 namespace Microsoft.AspNetCore.ResponseCaching {
     public class ResponseCachingMiddleware {
-        public ResponseCachingMiddleware(RequestDelegate next, IOptions<ResponseCachingOptions> options, ILoggerFactory loggerFactory, IResponseCachingPolicyProvider policyProvider, IResponseCachingKeyProvider keyProvider);

+        public ResponseCachingMiddleware(RequestDelegate next, IOptions<ResponseCachingOptions> options, ILoggerFactory loggerFactory, ObjectPoolProvider poolProvider);
     }
 }
```

