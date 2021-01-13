// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.ResponseCompression
{
    internal partial class ResponseCompressionBody : System.IO.Stream, Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature, Microsoft.AspNetCore.Http.Features.IHttpsCompressionFeature
    {
        internal ResponseCompressionBody(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.ResponseCompression.IResponseCompressionProvider provider, Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature innerBodyFeature) { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override long Length { get { throw null; } }
        Microsoft.AspNetCore.Http.Features.HttpsCompressionMode Microsoft.AspNetCore.Http.Features.IHttpsCompressionFeature.Mode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override long Position { get { throw null; } set { } }
        public System.IO.Stream Stream { get { throw null; } }
        public System.IO.Pipelines.PipeWriter Writer { get { throw null; } }
        public override System.IAsyncResult BeginWrite(byte[] buffer, int offset, int count, System.AsyncCallback callback, object state) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task CompleteAsync() { throw null; }
        public void DisableBuffering() { }
        public override void EndWrite(System.IAsyncResult asyncResult) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        internal System.Threading.Tasks.Task FinishCompressionAsync() { throw null; }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public System.Threading.Tasks.Task SendFileAsync(string path, long offset, long? count, System.Threading.CancellationToken cancellation) { throw null; }
        public override void SetLength(long value) { }
        public System.Threading.Tasks.Task StartAsync(System.Threading.CancellationToken token = default(System.Threading.CancellationToken)) { throw null; }
        public override void Write(byte[] buffer, int offset, int count) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
}
