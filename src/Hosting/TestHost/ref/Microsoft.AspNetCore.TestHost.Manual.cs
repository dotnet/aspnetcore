// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.TestHost
{
    internal abstract partial class ApplicationWrapper
    {
        protected ApplicationWrapper() { }
        internal abstract object CreateContext(Microsoft.AspNetCore.Http.Features.IFeatureCollection features);
        internal abstract void DisposeContext(object context, System.Exception exception);
        internal abstract System.Threading.Tasks.Task ProcessRequestAsync(object context);
    }
    internal partial class ApplicationWrapper<TContext> : Microsoft.AspNetCore.TestHost.ApplicationWrapper, Microsoft.AspNetCore.Hosting.Server.IHttpApplication<TContext>
    {
        public ApplicationWrapper(Microsoft.AspNetCore.Hosting.Server.IHttpApplication<TContext> application, System.Action preProcessRequestAsync) { }
        internal override object CreateContext(Microsoft.AspNetCore.Http.Features.IFeatureCollection features) { throw null; }
        internal override void DisposeContext(object context, System.Exception exception) { }
        TContext Microsoft.AspNetCore.Hosting.Server.IHttpApplication<TContext>.CreateContext(Microsoft.AspNetCore.Http.Features.IFeatureCollection features) { throw null; }
        void Microsoft.AspNetCore.Hosting.Server.IHttpApplication<TContext>.DisposeContext(TContext context, System.Exception exception) { }
        System.Threading.Tasks.Task Microsoft.AspNetCore.Hosting.Server.IHttpApplication<TContext>.ProcessRequestAsync(TContext context) { throw null; }
        internal override System.Threading.Tasks.Task ProcessRequestAsync(object context) { throw null; }
    }
    public partial class ClientHandler : System.Net.Http.HttpMessageHandler
    {
        internal ClientHandler(Microsoft.AspNetCore.Http.PathString pathBase, Microsoft.AspNetCore.TestHost.ApplicationWrapper application) { }
        internal bool AllowSynchronousIO { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal bool PreserveExecutionContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    internal partial class ResponseFeature : Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature, Microsoft.AspNetCore.Http.Features.IHttpResponseFeature
    {
        public ResponseFeature(System.Action<System.Exception> abort) { }
        public System.IO.Stream Body { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal System.IO.Pipelines.PipeWriter BodyWriter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool HasStarted { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.IHeaderDictionary Headers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ReasonPhrase { get { throw null; } set { } }
        public int StatusCode { get { throw null; } set { } }
        public System.IO.Stream Stream { get { throw null; } }
        public System.IO.Pipelines.PipeWriter Writer { get { throw null; } }
        public System.Threading.Tasks.Task CompleteAsync() { throw null; }
        public void DisableBuffering() { }
        public System.Threading.Tasks.Task FireOnResponseCompletedAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task FireOnSendingHeadersAsync() { throw null; }
        public void OnCompleted(System.Func<object, System.Threading.Tasks.Task> callback, object state) { }
        public void OnStarting(System.Func<object, System.Threading.Tasks.Task> callback, object state) { }
        public System.Threading.Tasks.Task SendFileAsync(string path, long offset, long? count, System.Threading.CancellationToken cancellation) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task StartAsync(System.Threading.CancellationToken token = default(System.Threading.CancellationToken)) { throw null; }
    }
}
