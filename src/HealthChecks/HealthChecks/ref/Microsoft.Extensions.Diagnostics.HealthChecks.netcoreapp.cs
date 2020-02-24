// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class HealthChecksBuilderAddCheckExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder AddCheck(this Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder builder, string name, Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck instance, Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus? failureStatus, System.Collections.Generic.IEnumerable<string> tags) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder AddCheck(this Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder builder, string name, Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck instance, Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus? failureStatus = default(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus?), System.Collections.Generic.IEnumerable<string> tags = null, System.TimeSpan? timeout = default(System.TimeSpan?)) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder AddCheck<T>(this Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder builder, string name, Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus? failureStatus, System.Collections.Generic.IEnumerable<string> tags) where T : class, Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder AddCheck<T>(this Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder builder, string name, Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus? failureStatus = default(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus?), System.Collections.Generic.IEnumerable<string> tags = null, System.TimeSpan? timeout = default(System.TimeSpan?)) where T : class, Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder AddTypeActivatedCheck<T>(this Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder builder, string name, Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus? failureStatus, System.Collections.Generic.IEnumerable<string> tags, params object[] args) where T : class, Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder AddTypeActivatedCheck<T>(this Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder builder, string name, Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus? failureStatus, System.Collections.Generic.IEnumerable<string> tags, System.TimeSpan timeout, params object[] args) where T : class, Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder AddTypeActivatedCheck<T>(this Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder builder, string name, Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus? failureStatus, params object[] args) where T : class, Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder AddTypeActivatedCheck<T>(this Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder builder, string name, params object[] args) where T : class, Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck { throw null; }
    }
    public static partial class HealthChecksBuilderDelegateExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder AddAsyncCheck(this Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder builder, string name, System.Func<System.Threading.CancellationToken, System.Threading.Tasks.Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult>> check, System.Collections.Generic.IEnumerable<string> tags) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder AddAsyncCheck(this Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder builder, string name, System.Func<System.Threading.CancellationToken, System.Threading.Tasks.Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult>> check, System.Collections.Generic.IEnumerable<string> tags = null, System.TimeSpan? timeout = default(System.TimeSpan?)) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder AddAsyncCheck(this Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder builder, string name, System.Func<System.Threading.Tasks.Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult>> check, System.Collections.Generic.IEnumerable<string> tags) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder AddAsyncCheck(this Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder builder, string name, System.Func<System.Threading.Tasks.Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult>> check, System.Collections.Generic.IEnumerable<string> tags = null, System.TimeSpan? timeout = default(System.TimeSpan?)) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder AddCheck(this Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder builder, string name, System.Func<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> check, System.Collections.Generic.IEnumerable<string> tags) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder AddCheck(this Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder builder, string name, System.Func<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> check, System.Collections.Generic.IEnumerable<string> tags = null, System.TimeSpan? timeout = default(System.TimeSpan?)) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder AddCheck(this Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder builder, string name, System.Func<System.Threading.CancellationToken, Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> check, System.Collections.Generic.IEnumerable<string> tags) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder AddCheck(this Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder builder, string name, System.Func<System.Threading.CancellationToken, Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> check, System.Collections.Generic.IEnumerable<string> tags = null, System.TimeSpan? timeout = default(System.TimeSpan?)) { throw null; }
    }
    public static partial class HealthCheckServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder AddHealthChecks(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
    }
    public partial interface IHealthChecksBuilder
    {
        Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; }
        Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder Add(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration registration);
    }
}
namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    public sealed partial class HealthCheckPublisherOptions
    {
        public HealthCheckPublisherOptions() { }
        public System.TimeSpan Delay { get { throw null; } set { } }
        public System.TimeSpan Period { get { throw null; } set { } }
        public System.Func<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration, bool> Predicate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.TimeSpan Timeout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public abstract partial class HealthCheckService
    {
        protected HealthCheckService() { }
        public abstract System.Threading.Tasks.Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport> CheckHealthAsync(System.Func<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration, bool> predicate, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public System.Threading.Tasks.Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport> CheckHealthAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public sealed partial class HealthCheckServiceOptions
    {
        public HealthCheckServiceOptions() { }
        public System.Collections.Generic.ICollection<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration> Registrations { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
}
