// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    public sealed partial class HealthCheckContext
    {
        public HealthCheckContext() { }
        public Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration Registration { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public sealed partial class HealthCheckRegistration
    {
        public HealthCheckRegistration(string name, Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck instance, Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus? failureStatus, System.Collections.Generic.IEnumerable<string> tags) { }
        public HealthCheckRegistration(string name, Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck instance, Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus? failureStatus, System.Collections.Generic.IEnumerable<string> tags, System.TimeSpan? timeout) { }
        public HealthCheckRegistration(string name, System.Func<System.IServiceProvider, Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck> factory, Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus? failureStatus, System.Collections.Generic.IEnumerable<string> tags) { }
        public HealthCheckRegistration(string name, System.Func<System.IServiceProvider, Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck> factory, Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus? failureStatus, System.Collections.Generic.IEnumerable<string> tags, System.TimeSpan? timeout) { }
        public System.Func<System.IServiceProvider, Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck> Factory { get { throw null; } set { } }
        public Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus FailureStatus { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Name { get { throw null; } set { } }
        public System.Collections.Generic.ISet<string> Tags { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.TimeSpan Timeout { get { throw null; } set { } }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct HealthCheckResult
    {
        private object _dummy;
        private int _dummyPrimitive;
        public HealthCheckResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus status, string description = null, System.Exception exception = null, System.Collections.Generic.IReadOnlyDictionary<string, object> data = null) { throw null; }
        public System.Collections.Generic.IReadOnlyDictionary<string, object> Data { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Description { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Exception Exception { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus Status { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public static Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult Degraded(string description = null, System.Exception exception = null, System.Collections.Generic.IReadOnlyDictionary<string, object> data = null) { throw null; }
        public static Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult Healthy(string description = null, System.Collections.Generic.IReadOnlyDictionary<string, object> data = null) { throw null; }
        public static Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult Unhealthy(string description = null, System.Exception exception = null, System.Collections.Generic.IReadOnlyDictionary<string, object> data = null) { throw null; }
    }
    public sealed partial class HealthReport
    {
        public HealthReport(System.Collections.Generic.IReadOnlyDictionary<string, Microsoft.Extensions.Diagnostics.HealthChecks.HealthReportEntry> entries, System.TimeSpan totalDuration) { }
        public System.Collections.Generic.IReadOnlyDictionary<string, Microsoft.Extensions.Diagnostics.HealthChecks.HealthReportEntry> Entries { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus Status { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.TimeSpan TotalDuration { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct HealthReportEntry
    {
        private object _dummy;
        private int _dummyPrimitive;
        public HealthReportEntry(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus status, string description, System.TimeSpan duration, System.Exception exception, System.Collections.Generic.IReadOnlyDictionary<string, object> data) { throw null; }
        public HealthReportEntry(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus status, string description, System.TimeSpan duration, System.Exception exception, System.Collections.Generic.IReadOnlyDictionary<string, object> data, System.Collections.Generic.IEnumerable<string> tags = null) { throw null; }
        public System.Collections.Generic.IReadOnlyDictionary<string, object> Data { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Description { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.TimeSpan Duration { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Exception Exception { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus Status { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IEnumerable<string> Tags { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public enum HealthStatus
    {
        Unhealthy = 0,
        Degraded = 1,
        Healthy = 2,
    }
    public partial interface IHealthCheck
    {
        System.Threading.Tasks.Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
    }
    public partial interface IHealthCheckPublisher
    {
        System.Threading.Tasks.Task PublishAsync(Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report, System.Threading.CancellationToken cancellationToken);
    }
}
