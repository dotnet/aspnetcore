// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.MiddlewareAnalysis
{
    public partial class AnalysisBuilder : Microsoft.AspNetCore.Builder.IApplicationBuilder
    {
        public AnalysisBuilder(Microsoft.AspNetCore.Builder.IApplicationBuilder inner) { }
        public System.IServiceProvider ApplicationServices { get { throw null; } set { } }
        public System.Collections.Generic.IDictionary<string, object> Properties { get { throw null; } }
        public Microsoft.AspNetCore.Http.Features.IFeatureCollection ServerFeatures { get { throw null; } }
        public Microsoft.AspNetCore.Http.RequestDelegate Build() { throw null; }
        public Microsoft.AspNetCore.Builder.IApplicationBuilder New() { throw null; }
        public Microsoft.AspNetCore.Builder.IApplicationBuilder Use(System.Func<Microsoft.AspNetCore.Http.RequestDelegate, Microsoft.AspNetCore.Http.RequestDelegate> middleware) { throw null; }
    }
    public partial class AnalysisMiddleware
    {
        public AnalysisMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, System.Diagnostics.DiagnosticSource diagnosticSource, string middlewareName) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
    }
    public partial class AnalysisStartupFilter : Microsoft.AspNetCore.Hosting.IStartupFilter
    {
        public AnalysisStartupFilter() { }
        public System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> Configure(System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> next) { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class AnalysisServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddMiddlewareAnalysis(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
    }
}
