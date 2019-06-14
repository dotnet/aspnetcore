# Microsoft.Extensions.Diagnostics.HealthChecks

``` diff
 namespace Microsoft.Extensions.Diagnostics.HealthChecks {
     public sealed class HealthCheckRegistration {
+        public HealthCheckRegistration(string name, IHealthCheck instance, Nullable<HealthStatus> failureStatus, IEnumerable<string> tags, Nullable<TimeSpan> timeout);
+        public HealthCheckRegistration(string name, Func<IServiceProvider, IHealthCheck> factory, Nullable<HealthStatus> failureStatus, IEnumerable<string> tags, Nullable<TimeSpan> timeout);
+        public TimeSpan Timeout { get; set; }
     }
     public struct HealthReportEntry {
+        public HealthReportEntry(HealthStatus status, string description, TimeSpan duration, Exception exception, IReadOnlyDictionary<string, object> data, IEnumerable<string> tags = null);
+        public IEnumerable<string> Tags { get; }
     }
 }
```

