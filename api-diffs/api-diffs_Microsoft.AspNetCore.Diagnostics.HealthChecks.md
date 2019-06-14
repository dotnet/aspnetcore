# Microsoft.AspNetCore.Diagnostics.HealthChecks

``` diff
 namespace Microsoft.AspNetCore.Diagnostics.HealthChecks {
     public class HealthCheckOptions {
-        public IDictionary<HealthStatus, int> ResultStatusCodes { get; }
+        public IDictionary<HealthStatus, int> ResultStatusCodes { get; set; }
     }
 }
```

