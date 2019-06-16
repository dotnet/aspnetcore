# Microsoft.AspNetCore.Diagnostics.HealthChecks

``` diff
 namespace Microsoft.AspNetCore.Diagnostics.HealthChecks {
     public class HealthCheckMiddleware {
         public HealthCheckMiddleware(RequestDelegate next, IOptions<HealthCheckOptions> healthCheckOptions, HealthCheckService healthCheckService);
         public Task InvokeAsync(HttpContext httpContext);
     }
     public class HealthCheckOptions {
         public HealthCheckOptions();
         public bool AllowCachingResponses { get; set; }
         public Func<HealthCheckRegistration, bool> Predicate { get; set; }
         public Func<HttpContext, HealthReport, Task> ResponseWriter { get; set; }
-        public IDictionary<HealthStatus, int> ResultStatusCodes { get; }
+        public IDictionary<HealthStatus, int> ResultStatusCodes { get; set; }
     }
 }
```

