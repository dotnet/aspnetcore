# Microsoft.AspNetCore.Mvc.Cors

``` diff
 namespace Microsoft.AspNetCore.Mvc.Cors {
-    public class CorsAuthorizationFilter : IAsyncAuthorizationFilter, ICorsAuthorizationFilter, IFilterMetadata, IOrderedFilter {
+    public class CorsAuthorizationFilter : IAsyncAuthorizationFilter, IFilterMetadata, IOrderedFilter {
         public CorsAuthorizationFilter(ICorsService corsService, ICorsPolicyProvider policyProvider);
         public CorsAuthorizationFilter(ICorsService corsService, ICorsPolicyProvider policyProvider, ILoggerFactory loggerFactory);
         public int Order { get; }
         public string PolicyName { get; set; }
         public Task OnAuthorizationAsync(AuthorizationFilterContext context);
     }
 }
```

