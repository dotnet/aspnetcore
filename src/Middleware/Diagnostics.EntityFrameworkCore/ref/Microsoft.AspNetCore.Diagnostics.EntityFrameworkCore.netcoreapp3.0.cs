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
namespace Microsoft.AspNetCore.DiagnosticsViewPage.Views
{
    [System.ObsoleteAttribute("This type is for internal use only and will be removed in a future version.")]
    public partial class AttributeValue
    {
        public AttributeValue(string prefix, object value, bool literal) { }
        public bool Literal { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Prefix { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public object Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public static Microsoft.AspNetCore.DiagnosticsViewPage.Views.AttributeValue FromTuple(System.Tuple<string, object, bool> value) { throw null; }
        public static Microsoft.AspNetCore.DiagnosticsViewPage.Views.AttributeValue FromTuple(System.Tuple<string, string, bool> value) { throw null; }
        public static implicit operator Microsoft.AspNetCore.DiagnosticsViewPage.Views.AttributeValue (System.Tuple<string, object, bool> value) { throw null; }
    }
    [System.ObsoleteAttribute("This type is for internal use only and will be removed in a future version.")]
    public abstract partial class BaseView
    {
        protected BaseView() { }
        protected Microsoft.AspNetCore.Http.HttpContext Context { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        protected System.Text.Encodings.Web.HtmlEncoder HtmlEncoder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected System.Text.Encodings.Web.JavaScriptEncoder JavaScriptEncoder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected System.IO.StreamWriter Output { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        protected Microsoft.AspNetCore.Http.HttpRequest Request { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        protected Microsoft.AspNetCore.Http.HttpResponse Response { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        protected System.Text.Encodings.Web.UrlEncoder UrlEncoder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected void BeginWriteAttribute(string name, string begining, int startPosition, string ending, int endPosition, int thingy) { }
        protected void EndWriteAttribute() { }
        public abstract System.Threading.Tasks.Task ExecuteAsync();
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
        protected string HtmlEncodeAndReplaceLineBreaks(string input) { throw null; }
        protected void Write(Microsoft.AspNetCore.DiagnosticsViewPage.Views.HelperResult result) { }
        protected void Write(object value) { }
        protected void Write(string value) { }
        protected void WriteAttributeTo(System.IO.TextWriter writer, string name, string leader, string trailer, params Microsoft.AspNetCore.DiagnosticsViewPage.Views.AttributeValue[] values) { }
        protected void WriteAttributeValue(string thingy, int startPostion, object value, int endValue, int dealyo, bool yesno) { }
        protected void WriteLiteral(object value) { }
        protected void WriteLiteral(string value) { }
        protected void WriteLiteralTo(System.IO.TextWriter writer, object value) { }
        protected void WriteLiteralTo(System.IO.TextWriter writer, string value) { }
        protected void WriteTo(System.IO.TextWriter writer, object value) { }
        protected void WriteTo(System.IO.TextWriter writer, string value) { }
    }
    [System.ObsoleteAttribute("This type is for internal use only and will be removed in a future version.")]
    public partial class HelperResult
    {
        public HelperResult(System.Action<System.IO.TextWriter> action) { }
        public System.Action<System.IO.TextWriter> WriteAction { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void WriteTo(System.IO.TextWriter writer) { }
    }
}
