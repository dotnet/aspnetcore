// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.Views
{
    internal partial class DatabaseErrorPage : Microsoft.Extensions.RazorViews.BaseView
    {
        public DatabaseErrorPage() { }
        public Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.Views.DatabaseErrorPageModel Model { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task ExecuteAsync() { throw null; }
        public string JavaScriptEncode(string content) { throw null; }
        public string UrlEncode(string content) { throw null; }
    }
    internal partial class DatabaseErrorPageModel
    {
        public DatabaseErrorPageModel(System.Type contextType, System.Exception exception, bool databaseExists, bool pendingModelChanges, System.Collections.Generic.IEnumerable<string> pendingMigrations, Microsoft.AspNetCore.Builder.DatabaseErrorPageOptions options) { }
        public virtual System.Type ContextType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public virtual bool DatabaseExists { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public virtual System.Exception Exception { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public virtual Microsoft.AspNetCore.Builder.DatabaseErrorPageOptions Options { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<string> PendingMigrations { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public virtual bool PendingModelChanges { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
}

namespace Microsoft.Extensions.RazorViews
{
    internal partial class AttributeValue
    {
        public AttributeValue(string prefix, object value, bool literal) { }
        public bool Literal { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Prefix { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public object Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public static Microsoft.Extensions.RazorViews.AttributeValue FromTuple(System.Tuple<string, object, bool> value) { throw null; }
        public static Microsoft.Extensions.RazorViews.AttributeValue FromTuple(System.Tuple<string, string, bool> value) { throw null; }
        public static implicit operator Microsoft.Extensions.RazorViews.AttributeValue (System.Tuple<string, object, bool> value) { throw null; }
    }
    internal abstract partial class BaseView
    {
        protected BaseView() { }
        protected Microsoft.AspNetCore.Http.HttpContext Context { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        protected System.Text.Encodings.Web.HtmlEncoder HtmlEncoder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected System.Text.Encodings.Web.JavaScriptEncoder JavaScriptEncoder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected System.IO.TextWriter Output { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        protected Microsoft.AspNetCore.Http.HttpRequest Request { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        protected Microsoft.AspNetCore.Http.HttpResponse Response { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        protected System.Text.Encodings.Web.UrlEncoder UrlEncoder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected void BeginWriteAttribute(string name, string begining, int startPosition, string ending, int endPosition, int thingy) { }
        protected void EndWriteAttribute() { }
        public abstract System.Threading.Tasks.Task ExecuteAsync();
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task ExecuteAsync(System.IO.Stream stream) { throw null; }
        protected string HtmlEncodeAndReplaceLineBreaks(string input) { throw null; }
        protected virtual System.IO.TextWriter PopWriter() { throw null; }
        protected virtual void PushWriter(System.IO.TextWriter writer) { }
        protected void Write(Microsoft.Extensions.RazorViews.HelperResult result) { }
        protected void Write(object value) { }
        protected void Write(string value) { }
        protected void WriteAttribute(string name, string leader, string trailer, params Microsoft.Extensions.RazorViews.AttributeValue[] values) { }
        protected void WriteAttributeValue(string thingy, int startPostion, object value, int endValue, int dealyo, bool yesno) { }
        protected void WriteLiteral(object value) { }
        protected void WriteLiteral(string value) { }
    }
    internal partial class HelperResult
    {
        public HelperResult(System.Action<System.IO.TextWriter> action) { }
        public System.Action<System.IO.TextWriter> WriteAction { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void WriteTo(System.IO.TextWriter writer) { }
    }
}
