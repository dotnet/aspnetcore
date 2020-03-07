// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class DeveloperExceptionPageExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseDeveloperExceptionPage(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseDeveloperExceptionPage(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.AspNetCore.Builder.DeveloperExceptionPageOptions options) { throw null; }
    }
    public partial class DeveloperExceptionPageOptions
    {
        public DeveloperExceptionPageOptions() { }
        public Microsoft.Extensions.FileProviders.IFileProvider FileProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int SourceCodeLineCount { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public static partial class ExceptionHandlerExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseExceptionHandler(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseExceptionHandler(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.AspNetCore.Builder.ExceptionHandlerOptions options) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseExceptionHandler(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> configure) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseExceptionHandler(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, string errorHandlingPath) { throw null; }
    }
    public partial class ExceptionHandlerOptions
    {
        public ExceptionHandlerOptions() { }
        public Microsoft.AspNetCore.Http.RequestDelegate ExceptionHandler { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Http.PathString ExceptionHandlingPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public static partial class StatusCodePagesExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseStatusCodePages(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseStatusCodePages(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.AspNetCore.Builder.StatusCodePagesOptions options) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseStatusCodePages(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> configuration) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseStatusCodePages(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, System.Func<Microsoft.AspNetCore.Diagnostics.StatusCodeContext, System.Threading.Tasks.Task> handler) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseStatusCodePages(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, string contentType, string bodyFormat) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseStatusCodePagesWithRedirects(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, string locationFormat) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseStatusCodePagesWithReExecute(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, string pathFormat, string queryFormat = null) { throw null; }
    }
    public partial class StatusCodePagesOptions
    {
        public StatusCodePagesOptions() { }
        public System.Func<Microsoft.AspNetCore.Diagnostics.StatusCodeContext, System.Threading.Tasks.Task> HandleAsync { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public static partial class WelcomePageExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseWelcomePage(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseWelcomePage(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.AspNetCore.Builder.WelcomePageOptions options) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseWelcomePage(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.AspNetCore.Http.PathString path) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseWelcomePage(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, string path) { throw null; }
    }
    public partial class WelcomePageOptions
    {
        public WelcomePageOptions() { }
        public Microsoft.AspNetCore.Http.PathString Path { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
}
namespace Microsoft.AspNetCore.Diagnostics
{
    public partial class DeveloperExceptionPageMiddleware
    {
        public DeveloperExceptionPageMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Builder.DeveloperExceptionPageOptions> options, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.Hosting.IWebHostEnvironment hostingEnvironment, System.Diagnostics.DiagnosticSource diagnosticSource, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Diagnostics.IDeveloperPageExceptionFilter> filters) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
    public partial class ExceptionHandlerFeature : Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature, Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature
    {
        public ExceptionHandlerFeature() { }
        public System.Exception Error { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Path { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class ExceptionHandlerMiddleware
    {
        public ExceptionHandlerMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Builder.ExceptionHandlerOptions> options, System.Diagnostics.DiagnosticListener diagnosticListener) { }
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
    public partial class StatusCodeContext
    {
        public StatusCodeContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Builder.StatusCodePagesOptions options, Microsoft.AspNetCore.Http.RequestDelegate next) { }
        public Microsoft.AspNetCore.Http.HttpContext HttpContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Http.RequestDelegate Next { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Builder.StatusCodePagesOptions Options { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class StatusCodePagesFeature : Microsoft.AspNetCore.Diagnostics.IStatusCodePagesFeature
    {
        public StatusCodePagesFeature() { }
        public bool Enabled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class StatusCodePagesMiddleware
    {
        public StatusCodePagesMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Builder.StatusCodePagesOptions> options) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
    public partial class StatusCodeReExecuteFeature : Microsoft.AspNetCore.Diagnostics.IStatusCodeReExecuteFeature
    {
        public StatusCodeReExecuteFeature() { }
        public string OriginalPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string OriginalPathBase { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string OriginalQueryString { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class WelcomePageMiddleware
    {
        public WelcomePageMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Builder.WelcomePageOptions> options) { }
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
}
