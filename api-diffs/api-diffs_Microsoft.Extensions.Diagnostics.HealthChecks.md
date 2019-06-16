# Microsoft.Extensions.Diagnostics.HealthChecks

``` diff
 namespace Microsoft.Extensions.Diagnostics.HealthChecks {
     public sealed class HealthCheckContext {
         public HealthCheckContext();
         public HealthCheckRegistration Registration { get; set; }
     }
     public sealed class HealthCheckPublisherOptions {
         public HealthCheckPublisherOptions();
         public TimeSpan Delay { get; set; }
         public TimeSpan Period { get; set; }
         public Func<HealthCheckRegistration, bool> Predicate { get; set; }
         public TimeSpan Timeout { get; set; }
     }
     public sealed class HealthCheckRegistration {
         public HealthCheckRegistration(string name, IHealthCheck instance, Nullable<HealthStatus> failureStatus, IEnumerable<string> tags);
+        public HealthCheckRegistration(string name, IHealthCheck instance, Nullable<HealthStatus> failureStatus, IEnumerable<string> tags, Nullable<TimeSpan> timeout);
         public HealthCheckRegistration(string name, Func<IServiceProvider, IHealthCheck> factory, Nullable<HealthStatus> failureStatus, IEnumerable<string> tags);
+        public HealthCheckRegistration(string name, Func<IServiceProvider, IHealthCheck> factory, Nullable<HealthStatus> failureStatus, IEnumerable<string> tags, Nullable<TimeSpan> timeout);
         public Func<IServiceProvider, IHealthCheck> Factory { get; set; }
         public HealthStatus FailureStatus { get; set; }
         public string Name { get; set; }
         public ISet<string> Tags { get; }
+        public TimeSpan Timeout { get; set; }
     }
     public struct HealthCheckResult {
         public HealthCheckResult(HealthStatus status, string description = null, Exception exception = null, IReadOnlyDictionary<string, object> data = null);
         public IReadOnlyDictionary<string, object> Data { get; }
         public string Description { get; }
         public Exception Exception { get; }
         public HealthStatus Status { get; }
         public static HealthCheckResult Degraded(string description = null, Exception exception = null, IReadOnlyDictionary<string, object> data = null);
         public static HealthCheckResult Healthy(string description = null, IReadOnlyDictionary<string, object> data = null);
         public static HealthCheckResult Unhealthy(string description = null, Exception exception = null, IReadOnlyDictionary<string, object> data = null);
     }
     public abstract class HealthCheckService {
         protected HealthCheckService();
         public abstract Task<HealthReport> CheckHealthAsync(Func<HealthCheckRegistration, bool> predicate, CancellationToken cancellationToken = default(CancellationToken));
         public Task<HealthReport> CheckHealthAsync(CancellationToken cancellationToken = default(CancellationToken));
     }
     public sealed class HealthCheckServiceOptions {
         public HealthCheckServiceOptions();
         public ICollection<HealthCheckRegistration> Registrations { get; }
     }
     public sealed class HealthReport {
         public HealthReport(IReadOnlyDictionary<string, HealthReportEntry> entries, TimeSpan totalDuration);
         public IReadOnlyDictionary<string, HealthReportEntry> Entries { get; }
         public HealthStatus Status { get; }
         public TimeSpan TotalDuration { get; }
     }
     public struct HealthReportEntry {
         public HealthReportEntry(HealthStatus status, string description, TimeSpan duration, Exception exception, IReadOnlyDictionary<string, object> data);
+        public HealthReportEntry(HealthStatus status, string description, TimeSpan duration, Exception exception, IReadOnlyDictionary<string, object> data, IEnumerable<string> tags = null);
         public IReadOnlyDictionary<string, object> Data { get; }
         public string Description { get; }
         public TimeSpan Duration { get; }
         public Exception Exception { get; }
         public HealthStatus Status { get; }
+        public IEnumerable<string> Tags { get; }
     }
     public enum HealthStatus {
         Degraded = 1,
         Healthy = 2,
         Unhealthy = 0,
     }
     public interface IHealthCheck {
         Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken));
     }
     public interface IHealthCheckPublisher {
         Task PublishAsync(HealthReport report, CancellationToken cancellationToken);
     }
 }
```

