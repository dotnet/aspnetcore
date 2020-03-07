// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class HealthCheckApplicationBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseHealthChecks(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.AspNetCore.Http.PathString path) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseHealthChecks(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.AspNetCore.Http.PathString path, Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions options) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseHealthChecks(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.AspNetCore.Http.PathString path, int port) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseHealthChecks(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.AspNetCore.Http.PathString path, int port, Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions options) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseHealthChecks(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.AspNetCore.Http.PathString path, string port) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseHealthChecks(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.AspNetCore.Http.PathString path, string port, Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions options) { throw null; }
    }
    public static partial class HealthCheckEndpointRouteBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapHealthChecks(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern) { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapHealthChecks(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern, Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions options) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Diagnostics.HealthChecks
{
    public partial class HealthCheckMiddleware
    {
        public HealthCheckMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions> healthCheckOptions, Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService healthCheckService) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task InvokeAsync(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
    }
    public partial class HealthCheckOptions
    {
        public HealthCheckOptions() { }
        public bool AllowCachingResponses { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Func<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration, bool> Predicate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Func<Microsoft.AspNetCore.Http.HttpContext, Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport, System.Threading.Tasks.Task> ResponseWriter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IDictionary<Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus, int> ResultStatusCodes { get { throw null; } set { } }
    }
}
