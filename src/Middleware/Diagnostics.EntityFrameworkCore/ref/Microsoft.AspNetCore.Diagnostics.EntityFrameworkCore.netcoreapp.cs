// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class DatabaseErrorPageExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseDatabaseErrorPage(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseDatabaseErrorPage(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.AspNetCore.Builder.DatabaseErrorPageOptions options) { throw null; }
    }
    public partial class DatabaseErrorPageOptions
    {
        public DatabaseErrorPageOptions() { }
        public virtual Microsoft.AspNetCore.Http.PathString MigrationsEndPointPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public static partial class MigrationsEndPointExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseMigrationsEndPoint(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseMigrationsEndPoint(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.AspNetCore.Builder.MigrationsEndPointOptions options) { throw null; }
    }
    public partial class MigrationsEndPointOptions
    {
        public static Microsoft.AspNetCore.Http.PathString DefaultPath;
        public MigrationsEndPointOptions() { }
        public virtual Microsoft.AspNetCore.Http.PathString Path { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore
{
    public partial class DatabaseErrorPageMiddleware : System.IObserver<System.Collections.Generic.KeyValuePair<string, object>>, System.IObserver<System.Diagnostics.DiagnosticListener>
    {
        public DatabaseErrorPageMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Builder.DatabaseErrorPageOptions> options) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
        void System.IObserver<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.OnCompleted() { }
        void System.IObserver<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.OnError(System.Exception error) { }
        void System.IObserver<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.OnNext(System.Collections.Generic.KeyValuePair<string, object> keyValuePair) { }
        void System.IObserver<System.Diagnostics.DiagnosticListener>.OnCompleted() { }
        void System.IObserver<System.Diagnostics.DiagnosticListener>.OnError(System.Exception error) { }
        void System.IObserver<System.Diagnostics.DiagnosticListener>.OnNext(System.Diagnostics.DiagnosticListener diagnosticListener) { }
    }
    public partial class MigrationsEndPointMiddleware
    {
        public MigrationsEndPointMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.MigrationsEndPointMiddleware> logger, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Builder.MigrationsEndPointOptions> options) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
}
