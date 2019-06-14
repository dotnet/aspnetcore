# Microsoft.AspNetCore.Cors.Infrastructure

``` diff
 namespace Microsoft.AspNetCore.Cors.Infrastructure {
     public class CorsMiddleware {
-        public CorsMiddleware(RequestDelegate next, ICorsService corsService, CorsPolicy policy);

-        public CorsMiddleware(RequestDelegate next, ICorsService corsService, ICorsPolicyProvider policyProvider);

-        public CorsMiddleware(RequestDelegate next, ICorsService corsService, ICorsPolicyProvider policyProvider, ILoggerFactory loggerFactory);

-        public CorsMiddleware(RequestDelegate next, ICorsService corsService, ICorsPolicyProvider policyProvider, ILoggerFactory loggerFactory, string policyName);

-        public CorsMiddleware(RequestDelegate next, ICorsService corsService, ICorsPolicyProvider policyProvider, string policyName);

+        public CorsMiddleware(RequestDelegate next, ICorsService corsService, ILoggerFactory loggerFactory);
+        public CorsMiddleware(RequestDelegate next, ICorsService corsService, ILoggerFactory loggerFactory, string policyName);
-        public Task Invoke(HttpContext context);

+        public Task Invoke(HttpContext context, ICorsPolicyProvider corsPolicyProvider);
     }
     public class CorsService : ICorsService {
-        public CorsService(IOptions<CorsOptions> options);

     }
+    public interface ICorsMetadata
+    public interface ICorsPolicyMetadata : ICorsMetadata {
+        CorsPolicy Policy { get; }
+    }
-    public interface IDisableCorsAttribute
+    public interface IDisableCorsAttribute : ICorsMetadata
-    public interface IEnableCorsAttribute
+    public interface IEnableCorsAttribute : ICorsMetadata
 }
```

