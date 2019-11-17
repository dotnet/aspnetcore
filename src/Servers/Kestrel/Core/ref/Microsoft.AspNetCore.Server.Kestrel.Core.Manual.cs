// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal partial class HttpConnectionContext
    {
        public HttpConnectionContext() { }
        public Microsoft.AspNetCore.Connections.ConnectionContext ConnectionContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.Features.IFeatureCollection ConnectionFeatures { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ConnectionId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Net.IPEndPoint LocalEndPoint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Buffers.MemoryPool<byte> MemoryPool { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols Protocols { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Net.IPEndPoint RemoteEndPoint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.ServiceContext ServiceContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ITimeoutControl TimeoutControl { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.IO.Pipelines.IDuplexPipe Transport { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    internal partial interface IRequestProcessor
    {
        void Abort(Microsoft.AspNetCore.Connections.ConnectionAbortedException ex);
        void HandleReadDataRateTimeout();
        void HandleRequestHeadersTimeout();
        void OnInputOrOutputCompleted();
        System.Threading.Tasks.Task ProcessRequestsAsync<TContext>(Microsoft.AspNetCore.Hosting.Server.IHttpApplication<TContext> application);
        void StopProcessingNextRequest();
        void Tick(System.DateTimeOffset now);
    }
    internal partial class KestrelServerOptionsSetup : Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>
    {
        public KestrelServerOptionsSetup(System.IServiceProvider services) { }
        public void Configure(Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions options) { }
    }
    internal partial class ServiceContext
    {
        public ServiceContext() { }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ConnectionManager ConnectionManager { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.DateHeaderValueManager DateHeaderValueManager { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.Heartbeat Heartbeat { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpParser<Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1ParsingHandler> HttpParser { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace Log { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.IO.Pipelines.PipeScheduler Scheduler { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions ServerOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ISystemClock SystemClock { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    internal partial class TimeoutControl : Microsoft.AspNetCore.Server.Kestrel.Core.Features.IConnectionTimeoutFeature, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ITimeoutControl
    {
        public TimeoutControl(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ITimeoutHandler timeoutHandler) { }
        internal Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IDebugger Debugger { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.TimeoutReason TimerReason { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void BytesRead(long count) { }
        public void BytesWrittenToBuffer(Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate minRate, long count) { }
        public void CancelTimeout() { }
        internal void Initialize(long nowTicks) { }
        public void InitializeHttp2(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl.InputFlowControl connectionInputFlowControl) { }
        void Microsoft.AspNetCore.Server.Kestrel.Core.Features.IConnectionTimeoutFeature.ResetTimeout(System.TimeSpan timeSpan) { }
        void Microsoft.AspNetCore.Server.Kestrel.Core.Features.IConnectionTimeoutFeature.SetTimeout(System.TimeSpan timeSpan) { }
        public void ResetTimeout(long ticks, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.TimeoutReason timeoutReason) { }
        public void SetTimeout(long ticks, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.TimeoutReason timeoutReason) { }
        public void StartRequestBody(Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate minRate) { }
        public void StartTimingRead() { }
        public void StartTimingWrite() { }
        public void StopRequestBody() { }
        public void StopTimingRead() { }
        public void StopTimingWrite() { }
        public void Tick(System.DateTimeOffset now) { }
    }
}

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal partial class DateHeaderValueManager : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IHeartbeatHandler
    {
        public DateHeaderValueManager() { }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.DateHeaderValueManager.DateHeaderValues GetDateHeaderValues() { throw null; }
        public void OnHeartbeat(System.DateTimeOffset now) { }
        public partial class DateHeaderValues
        {
            public byte[] Bytes;
            public string String;
            public DateHeaderValues() { }
        }
    }
    [System.FlagsAttribute]
    internal enum ConnectionOptions
    {
        None = 0,
        Close = 1,
        KeepAlive = 2,
        Upgrade = 4,
    }
    internal abstract partial class HttpHeaders : Microsoft.AspNetCore.Http.IHeaderDictionary, System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.Generic.IDictionary<string, Microsoft.Extensions.Primitives.StringValues>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.IEnumerable
    {
        protected System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues> MaybeUnknown;
        protected long _bits;
        protected long? _contentLength;
        protected bool _isReadOnly;
        protected HttpHeaders() { }
        public long? ContentLength { get { throw null; } set { } }
        public int Count { get { throw null; } }
        Microsoft.Extensions.Primitives.StringValues Microsoft.AspNetCore.Http.IHeaderDictionary.this[string key] { get { throw null; } set { } }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.IsReadOnly { get { throw null; } }
        Microsoft.Extensions.Primitives.StringValues System.Collections.Generic.IDictionary<System.String,Microsoft.Extensions.Primitives.StringValues>.this[string key] { get { throw null; } set { } }
        System.Collections.Generic.ICollection<string> System.Collections.Generic.IDictionary<System.String,Microsoft.Extensions.Primitives.StringValues>.Keys { get { throw null; } }
        System.Collections.Generic.ICollection<Microsoft.Extensions.Primitives.StringValues> System.Collections.Generic.IDictionary<System.String,Microsoft.Extensions.Primitives.StringValues>.Values { get { throw null; } }
        protected System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues> Unknown { get { throw null; } }
        protected virtual bool AddValueFast(string key, Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]protected static Microsoft.Extensions.Primitives.StringValues AppendValue(Microsoft.Extensions.Primitives.StringValues existing, string append) { throw null; }
        protected virtual void ClearFast() { }
        protected virtual bool CopyToFast(System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>[] array, int arrayIndex) { throw null; }
        protected virtual int GetCountFast() { throw null; }
        protected virtual System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> GetEnumeratorFast() { throw null; }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.TransferCoding GetFinalTransferCoding(Microsoft.Extensions.Primitives.StringValues transferEncoding) { throw null; }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.ConnectionOptions ParseConnection(Microsoft.Extensions.Primitives.StringValues connection) { throw null; }
        protected virtual bool RemoveFast(string key) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]protected bool RemoveUnknown(string key) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public void Reset() { }
        public void SetReadOnly() { }
        protected virtual void SetValueFast(string key, Microsoft.Extensions.Primitives.StringValues value) { }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.Add(System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> item) { }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.Clear() { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.Contains(System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> item) { throw null; }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.CopyTo(System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>[] array, int arrayIndex) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.Remove(System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> item) { throw null; }
        void System.Collections.Generic.IDictionary<System.String,Microsoft.Extensions.Primitives.StringValues>.Add(string key, Microsoft.Extensions.Primitives.StringValues value) { }
        bool System.Collections.Generic.IDictionary<System.String,Microsoft.Extensions.Primitives.StringValues>.ContainsKey(string key) { throw null; }
        bool System.Collections.Generic.IDictionary<System.String,Microsoft.Extensions.Primitives.StringValues>.Remove(string key) { throw null; }
        bool System.Collections.Generic.IDictionary<System.String,Microsoft.Extensions.Primitives.StringValues>.TryGetValue(string key, out Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        protected static void ThrowArgumentException() { }
        protected static void ThrowDuplicateKeyException() { }
        protected static void ThrowHeadersReadOnlyException() { }
        protected static void ThrowKeyNotFoundException() { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]protected bool TryGetUnknown(string key, ref Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        protected virtual bool TryGetValueFast(string key, out Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        public static void ValidateHeaderNameCharacters(string headerCharacters) { }
        public static void ValidateHeaderValueCharacters(Microsoft.Extensions.Primitives.StringValues headerValues) { }
        public static void ValidateHeaderValueCharacters(string headerCharacters) { }
    }
    internal abstract partial class HttpProtocol : Microsoft.AspNetCore.Http.Features.IEndpointFeature, Microsoft.AspNetCore.Http.Features.IFeatureCollection, Microsoft.AspNetCore.Http.Features.IHttpBodyControlFeature, Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature, Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature, Microsoft.AspNetCore.Http.Features.IHttpRequestFeature, Microsoft.AspNetCore.Http.Features.IHttpRequestIdentifierFeature, Microsoft.AspNetCore.Http.Features.IHttpRequestLifetimeFeature, Microsoft.AspNetCore.Http.Features.IHttpRequestTrailersFeature, Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature, Microsoft.AspNetCore.Http.Features.IHttpResponseFeature, Microsoft.AspNetCore.Http.Features.IHttpUpgradeFeature, Microsoft.AspNetCore.Http.Features.IRequestBodyPipeFeature, Microsoft.AspNetCore.Http.Features.IRouteValuesFeature, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpResponseControl, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Type, object>>, System.Collections.IEnumerable
    {
        protected System.Exception _applicationException;
        protected Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.BodyControl _bodyControl;
        protected Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpVersion _httpVersion;
        protected volatile bool _keepAlive;
        protected string _methodText;
        protected string _parsedPath;
        protected string _parsedQueryString;
        protected string _parsedRawTarget;
        protected Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.RequestProcessingStatus _requestProcessingStatus;
        public HttpProtocol(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.HttpConnectionContext context) { }
        public bool AllowSynchronousIO { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.Features.IFeatureCollection ConnectionFeatures { get { throw null; } }
        protected string ConnectionId { get { throw null; } }
        public string ConnectionIdFeature { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool HasFlushedHeaders { get { throw null; } }
        public bool HasResponseCompleted { get { throw null; } }
        public bool HasResponseStarted { get { throw null; } }
        public bool HasStartedConsumingRequestBody { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpRequestHeaders HttpRequestHeaders { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpResponseControl HttpResponseControl { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders HttpResponseHeaders { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string HttpVersion { get { throw null; } [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]set { } }
        public bool IsUpgradableRequest { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool IsUpgraded { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Net.IPAddress LocalIpAddress { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int LocalPort { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace Log { get { throw null; } }
        public long? MaxRequestBodySize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod Method { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        Microsoft.AspNetCore.Http.Endpoint Microsoft.AspNetCore.Http.Features.IEndpointFeature.Endpoint { get { throw null; } set { } }
        bool Microsoft.AspNetCore.Http.Features.IFeatureCollection.IsReadOnly { get { throw null; } }
        object Microsoft.AspNetCore.Http.Features.IFeatureCollection.this[System.Type key] { get { throw null; } set { } }
        int Microsoft.AspNetCore.Http.Features.IFeatureCollection.Revision { get { throw null; } }
        bool Microsoft.AspNetCore.Http.Features.IHttpBodyControlFeature.AllowSynchronousIO { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.ConnectionId { get { throw null; } set { } }
        System.Net.IPAddress Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.LocalIpAddress { get { throw null; } set { } }
        int Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.LocalPort { get { throw null; } set { } }
        System.Net.IPAddress Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.RemoteIpAddress { get { throw null; } set { } }
        int Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.RemotePort { get { throw null; } set { } }
        bool Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature.IsReadOnly { get { throw null; } }
        long? Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature.MaxRequestBodySize { get { throw null; } set { } }
        System.IO.Stream Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Body { get { throw null; } set { } }
        Microsoft.AspNetCore.Http.IHeaderDictionary Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Headers { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Method { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Path { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.PathBase { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Protocol { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.QueryString { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.RawTarget { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Scheme { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpRequestIdentifierFeature.TraceIdentifier { get { throw null; } set { } }
        System.Threading.CancellationToken Microsoft.AspNetCore.Http.Features.IHttpRequestLifetimeFeature.RequestAborted { get { throw null; } set { } }
        bool Microsoft.AspNetCore.Http.Features.IHttpRequestTrailersFeature.Available { get { throw null; } }
        Microsoft.AspNetCore.Http.IHeaderDictionary Microsoft.AspNetCore.Http.Features.IHttpRequestTrailersFeature.Trailers { get { throw null; } }
        System.IO.Stream Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature.Stream { get { throw null; } }
        System.IO.Pipelines.PipeWriter Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature.Writer { get { throw null; } }
        System.IO.Stream Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.Body { get { throw null; } set { } }
        bool Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.HasStarted { get { throw null; } }
        Microsoft.AspNetCore.Http.IHeaderDictionary Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.Headers { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.ReasonPhrase { get { throw null; } set { } }
        int Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.StatusCode { get { throw null; } set { } }
        bool Microsoft.AspNetCore.Http.Features.IHttpUpgradeFeature.IsUpgradableRequest { get { throw null; } }
        System.IO.Pipelines.PipeReader Microsoft.AspNetCore.Http.Features.IRequestBodyPipeFeature.Reader { get { throw null; } }
        Microsoft.AspNetCore.Routing.RouteValueDictionary Microsoft.AspNetCore.Http.Features.IRouteValuesFeature.RouteValues { get { throw null; } set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate MinRequestBodyDataRate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpOutputProducer Output { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]protected set { } }
        public string Path { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string PathBase { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string QueryString { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string RawTarget { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ReasonPhrase { get { throw null; } set { } }
        public System.Net.IPAddress RemoteIpAddress { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int RemotePort { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Threading.CancellationToken RequestAborted { get { throw null; } set { } }
        public System.IO.Stream RequestBody { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.IO.Pipelines.PipeReader RequestBodyPipeReader { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.IHeaderDictionary RequestHeaders { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.IHeaderDictionary RequestTrailers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool RequestTrailersAvailable { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.IO.Stream ResponseBody { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.IO.Pipelines.PipeWriter ResponseBodyPipeWriter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.IHeaderDictionary ResponseHeaders { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseTrailers ResponseTrailers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Scheme { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions ServerOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.ServiceContext ServiceContext { get { throw null; } }
        public int StatusCode { get { throw null; } set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ITimeoutControl TimeoutControl { get { throw null; } }
        public string TraceIdentifier { get { throw null; } set { } }
        protected void AbortRequest() { }
        public void Advance(int bytes) { }
        protected abstract void ApplicationAbort();
        protected virtual bool BeginRead(out System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> awaitable) { throw null; }
        protected virtual void BeginRequestProcessing() { }
        public void CancelPendingFlush() { }
        public System.Threading.Tasks.Task CompleteAsync(System.Exception exception = null) { throw null; }
        protected abstract Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.MessageBody CreateMessageBody();
        protected abstract string CreateRequestId();
        protected System.Threading.Tasks.Task FireOnCompleted() { throw null; }
        protected System.Threading.Tasks.Task FireOnStarting() { throw null; }
        public System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FlushPipeAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Memory<byte> GetMemory(int sizeHint = 0) { throw null; }
        public System.Span<byte> GetSpan(int sizeHint = 0) { throw null; }
        public void HandleNonBodyResponseWrite() { }
        public void InitializeBodyControl(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.MessageBody messageBody) { }
        public System.Threading.Tasks.Task InitializeResponseAsync(int firstWriteByteCount) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task InitializeResponseAwaited(System.Threading.Tasks.Task startingTask, int firstWriteByteCount) { throw null; }
        TFeature Microsoft.AspNetCore.Http.Features.IFeatureCollection.Get<TFeature>() { throw null; }
        void Microsoft.AspNetCore.Http.Features.IFeatureCollection.Set<TFeature>(TFeature feature) { }
        void Microsoft.AspNetCore.Http.Features.IHttpRequestLifetimeFeature.Abort() { }
        System.Threading.Tasks.Task Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature.CompleteAsync() { throw null; }
        void Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature.DisableBuffering() { }
        System.Threading.Tasks.Task Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature.SendFileAsync(string path, long offset, long? count, System.Threading.CancellationToken cancellation) { throw null; }
        System.Threading.Tasks.Task Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature.StartAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        void Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.OnCompleted(System.Func<object, System.Threading.Tasks.Task> callback, object state) { }
        void Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.OnStarting(System.Func<object, System.Threading.Tasks.Task> callback, object state) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        System.Threading.Tasks.Task<System.IO.Stream> Microsoft.AspNetCore.Http.Features.IHttpUpgradeFeature.UpgradeAsync() { throw null; }
        public void OnCompleted(System.Func<object, System.Threading.Tasks.Task> callback, object state) { }
        protected virtual void OnErrorAfterResponseStarted() { }
        public void OnHeader(System.Span<byte> name, System.Span<byte> value) { }
        public void OnHeadersComplete() { }
        protected virtual void OnRequestProcessingEnded() { }
        protected virtual void OnRequestProcessingEnding() { }
        protected abstract void OnReset();
        public void OnStarting(System.Func<object, System.Threading.Tasks.Task> callback, object state) { }
        public void OnTrailer(System.Span<byte> name, System.Span<byte> value) { }
        public void OnTrailersComplete() { }
        protected void PoisonRequestBodyStream(System.Exception abortReason) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task ProcessRequestsAsync<TContext>(Microsoft.AspNetCore.Hosting.Server.IHttpApplication<TContext> application) { throw null; }
        public void ProduceContinue() { }
        protected System.Threading.Tasks.Task ProduceEnd() { throw null; }
        public void ReportApplicationError(System.Exception ex) { }
        public void Reset() { }
        internal void ResetFeatureCollection() { }
        protected void ResetHttp1Features() { }
        protected void ResetHttp2Features() { }
        internal void ResetState() { }
        public void SetBadRequestState(Microsoft.AspNetCore.Server.Kestrel.Core.BadHttpRequestException ex) { }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<System.Type, object>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Type,System.Object>>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        [System.Diagnostics.StackTraceHiddenAttribute]
        public void ThrowRequestTargetRejected(System.Span<byte> target) { }
        protected abstract bool TryParseRequest(System.IO.Pipelines.ReadResult result, out bool endConnection);
        protected System.Threading.Tasks.Task TryProduceInvalidRequestResponse() { throw null; }
        protected bool VerifyResponseContentLength(out System.Exception ex) { throw null; }
        public System.Threading.Tasks.Task WriteAsync(System.ReadOnlyMemory<byte> data, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteAsyncAwaited(System.Threading.Tasks.Task initializeTask, System.ReadOnlyMemory<byte> data, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WritePipeAsync(System.ReadOnlyMemory<byte> data, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    internal sealed partial class HttpRequestHeaders : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpHeaders
    {
        public HttpRequestHeaders(bool reuseHeaderValues = true) { }
        public bool HasConnection { get { throw null; } }
        public bool HasTransferEncoding { get { throw null; } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAccept { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAcceptCharset { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAcceptEncoding { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAcceptLanguage { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAccessControlRequestHeaders { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAccessControlRequestMethod { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAllow { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAuthorization { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderCacheControl { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderConnection { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentEncoding { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentLanguage { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentLength { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentLocation { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentMD5 { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentRange { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentType { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderCookie { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderCorrelationContext { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderDate { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderDNT { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderExpect { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderExpires { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderFrom { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderHost { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderIfMatch { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderIfModifiedSince { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderIfNoneMatch { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderIfRange { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderIfUnmodifiedSince { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderKeepAlive { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderLastModified { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderMaxForwards { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderOrigin { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderPragma { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderProxyAuthorization { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderRange { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderReferer { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderRequestId { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderTE { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderTraceParent { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderTraceState { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderTrailer { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderTransferEncoding { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderTranslate { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderUpgrade { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderUpgradeInsecureRequests { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderUserAgent { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderVia { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderWarning { get { throw null; } set { } }
        public int HostCount { get { throw null; } }
        protected override bool AddValueFast(string key, Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        public void Append(System.Span<byte> name, System.Span<byte> value) { }
        protected override void ClearFast() { }
        protected override bool CopyToFast(System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>[] array, int arrayIndex) { throw null; }
        protected override int GetCountFast() { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpRequestHeaders.Enumerator GetEnumerator() { throw null; }
        protected override System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> GetEnumeratorFast() { throw null; }
        public void OnHeadersComplete() { }
        protected override bool RemoveFast(string key) { throw null; }
        protected override void SetValueFast(string key, Microsoft.Extensions.Primitives.StringValues value) { }
        protected override bool TryGetValueFast(string key, out Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            internal Enumerator(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpRequestHeaders collection) { throw null; }
            public System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            public void Reset() { }
        }
    }
    internal sealed partial class HttpResponseHeaders : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpHeaders
    {
        public HttpResponseHeaders() { }
        public bool HasConnection { get { throw null; } }
        public bool HasDate { get { throw null; } }
        public bool HasServer { get { throw null; } }
        public bool HasTransferEncoding { get { throw null; } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAcceptRanges { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAccessControlAllowCredentials { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAccessControlAllowHeaders { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAccessControlAllowMethods { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAccessControlAllowOrigin { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAccessControlExposeHeaders { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAccessControlMaxAge { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAge { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAllow { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderCacheControl { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderConnection { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentEncoding { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentLanguage { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentLength { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentLocation { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentMD5 { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentRange { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentType { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderDate { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderETag { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderExpires { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderKeepAlive { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderLastModified { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderLocation { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderPragma { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderProxyAuthenticate { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderRetryAfter { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderServer { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderSetCookie { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderTrailer { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderTransferEncoding { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderUpgrade { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderVary { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderVia { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderWarning { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderWWWAuthenticate { get { throw null; } set { } }
        protected override bool AddValueFast(string key, Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        protected override void ClearFast() { }
        internal void CopyTo(ref System.Buffers.BufferWriter<System.IO.Pipelines.PipeWriter> buffer) { }
        internal void CopyToFast(ref System.Buffers.BufferWriter<System.IO.Pipelines.PipeWriter> output) { }
        protected override bool CopyToFast(System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>[] array, int arrayIndex) { throw null; }
        protected override int GetCountFast() { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders.Enumerator GetEnumerator() { throw null; }
        protected override System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> GetEnumeratorFast() { throw null; }
        protected override bool RemoveFast(string key) { throw null; }
        public void SetRawConnection(Microsoft.Extensions.Primitives.StringValues value, byte[] raw) { }
        public void SetRawDate(Microsoft.Extensions.Primitives.StringValues value, byte[] raw) { }
        public void SetRawServer(Microsoft.Extensions.Primitives.StringValues value, byte[] raw) { }
        public void SetRawTransferEncoding(Microsoft.Extensions.Primitives.StringValues value, byte[] raw) { }
        protected override void SetValueFast(string key, Microsoft.Extensions.Primitives.StringValues value) { }
        protected override bool TryGetValueFast(string key, out Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            internal Enumerator(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders collection) { throw null; }
            public System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            public void Reset() { }
        }
    }
    internal partial class HttpResponseTrailers : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpHeaders
    {
        public HttpResponseTrailers() { }
        public Microsoft.Extensions.Primitives.StringValues HeaderETag { get { throw null; } set { } }
        protected override bool AddValueFast(string key, Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        protected override void ClearFast() { }
        protected override bool CopyToFast(System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>[] array, int arrayIndex) { throw null; }
        protected override int GetCountFast() { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseTrailers.Enumerator GetEnumerator() { throw null; }
        protected override System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> GetEnumeratorFast() { throw null; }
        protected override bool RemoveFast(string key) { throw null; }
        protected override void SetValueFast(string key, Microsoft.Extensions.Primitives.StringValues value) { }
        protected override bool TryGetValueFast(string key, out Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            internal Enumerator(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseTrailers collection) { throw null; }
            public System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            public void Reset() { }
        }
    }
    internal partial class Http1Connection : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol, Microsoft.AspNetCore.Server.Kestrel.Core.Features.IHttpMinRequestBodyDataRateFeature, Microsoft.AspNetCore.Server.Kestrel.Core.Features.IHttpMinResponseDataRateFeature, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.IRequestProcessor
    {
        protected readonly long _keepAliveTicks;
        public Http1Connection(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.HttpConnectionContext context) : base (default(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.HttpConnectionContext)) { }
        public System.IO.Pipelines.PipeReader Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Buffers.MemoryPool<byte> MemoryPool { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate Microsoft.AspNetCore.Server.Kestrel.Core.Features.IHttpMinRequestBodyDataRateFeature.MinDataRate { get { throw null; } set { } }
        Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate Microsoft.AspNetCore.Server.Kestrel.Core.Features.IHttpMinResponseDataRateFeature.MinDataRate { get { throw null; } set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate MinResponseDataRate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool RequestTimedOut { get { throw null; } }
        public void Abort(Microsoft.AspNetCore.Connections.ConnectionAbortedException abortReason) { }
        protected override void ApplicationAbort() { }
        protected override bool BeginRead(out System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> awaitable) { throw null; }
        protected override void BeginRequestProcessing() { }
        protected override Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.MessageBody CreateMessageBody() { throw null; }
        protected override string CreateRequestId() { throw null; }
        internal void EnsureHostHeaderExists() { }
        public void HandleReadDataRateTimeout() { }
        public void HandleRequestHeadersTimeout() { }
        void Microsoft.AspNetCore.Server.Kestrel.Core.Internal.IRequestProcessor.Tick(System.DateTimeOffset now) { }
        public void OnInputOrOutputCompleted() { }
        protected override void OnRequestProcessingEnded() { }
        protected override void OnRequestProcessingEnding() { }
        protected override void OnReset() { }
        public void OnStartLine(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod method, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpVersion version, System.Span<byte> target, System.Span<byte> path, System.Span<byte> query, System.Span<byte> customMethod, bool pathEncoded) { }
        public void ParseRequest(in System.Buffers.ReadOnlySequence<byte> buffer, out System.SequencePosition consumed, out System.SequencePosition examined) { throw null; }
        public void SendTimeoutResponse() { }
        public void StopProcessingNextRequest() { }
        public bool TakeMessageHeaders(in System.Buffers.ReadOnlySequence<byte> buffer, bool trailers, out System.SequencePosition consumed, out System.SequencePosition examined) { throw null; }
        public bool TakeStartLine(in System.Buffers.ReadOnlySequence<byte> buffer, out System.SequencePosition consumed, out System.SequencePosition examined) { throw null; }
        protected override bool TryParseRequest(System.IO.Pipelines.ReadResult result, out bool endConnection) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal readonly partial struct Http1ParsingHandler : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpHeadersHandler, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpRequestLineHandler
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public Http1ParsingHandler(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1Connection connection) { throw null; }
        public Http1ParsingHandler(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1Connection connection, bool trailers) { throw null; }
        public void OnHeader(System.Span<byte> name, System.Span<byte> value) { }
        public void OnHeadersComplete() { }
        public void OnStartLine(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod method, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpVersion version, System.Span<byte> target, System.Span<byte> path, System.Span<byte> query, System.Span<byte> customMethod, bool pathEncoded) { }
    }
    internal partial interface IHttpOutputProducer
    {
        void Advance(int bytes);
        void CancelPendingFlush();
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FirstWriteAsync(int statusCode, string reasonPhrase, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders responseHeaders, bool autoChunk, System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FirstWriteChunkedAsync(int statusCode, string reasonPhrase, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders responseHeaders, bool autoChunk, System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FlushAsync(System.Threading.CancellationToken cancellationToken);
        System.Memory<byte> GetMemory(int sizeHint = 0);
        System.Span<byte> GetSpan(int sizeHint = 0);
        void Reset();
        void Stop();
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> Write100ContinueAsync();
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteChunkAsync(System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task WriteDataAsync(System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteDataToPipeAsync(System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken);
        void WriteResponseHeaders(int statusCode, string reasonPhrase, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders responseHeaders, bool autoChunk, bool appCompleted);
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteStreamSuffixAsync();
    }
    internal partial interface IHttpResponseControl
    {
        void Advance(int bytes);
        void CancelPendingFlush();
        System.Threading.Tasks.Task CompleteAsync(System.Exception exception = null);
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FlushPipeAsync(System.Threading.CancellationToken cancellationToken);
        System.Memory<byte> GetMemory(int sizeHint = 0);
        System.Span<byte> GetSpan(int sizeHint = 0);
        void ProduceContinue();
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WritePipeAsync(System.ReadOnlyMemory<byte> source, System.Threading.CancellationToken cancellationToken);
    }
    internal abstract partial class MessageBody
    {
        protected long _alreadyTimedBytes;
        protected bool _backpressure;
        protected long _examinedUnconsumedBytes;
        protected bool _timingEnabled;
        protected MessageBody(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol context) { }
        public virtual bool IsEmpty { get { throw null; } }
        protected Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace Log { get { throw null; } }
        public bool RequestKeepAlive { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]protected set { } }
        public bool RequestUpgrade { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]protected set { } }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.MessageBody ZeroContentLengthClose { get { throw null; } }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.MessageBody ZeroContentLengthKeepAlive { get { throw null; } }
        protected void AddAndCheckConsumedBytes(long consumedBytes) { }
        public abstract void AdvanceTo(System.SequencePosition consumed);
        public abstract void AdvanceTo(System.SequencePosition consumed, System.SequencePosition examined);
        public abstract void CancelPendingRead();
        public abstract void Complete(System.Exception exception);
        public virtual System.Threading.Tasks.Task ConsumeAsync() { throw null; }
        protected void CountBytesRead(long bytesInReadResult) { }
        protected long OnAdvance(System.IO.Pipelines.ReadResult readResult, System.SequencePosition consumed, System.SequencePosition examined) { throw null; }
        protected virtual System.Threading.Tasks.Task OnConsumeAsync() { throw null; }
        protected virtual void OnDataRead(long bytesRead) { }
        protected virtual void OnReadStarted() { }
        protected virtual void OnReadStarting() { }
        protected virtual System.Threading.Tasks.Task OnStopAsync() { throw null; }
        public abstract System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> ReadAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        protected System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> StartTimingReadAsync(System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> readAwaitable, System.Threading.CancellationToken cancellationToken) { throw null; }
        public virtual System.Threading.Tasks.Task StopAsync() { throw null; }
        protected void StopTimingRead(long bytesInReadResult) { }
        protected void TryProduceContinue() { }
        public abstract bool TryRead(out System.IO.Pipelines.ReadResult readResult);
        protected void TryStart() { }
        protected void TryStop() { }
    }
    internal enum RequestProcessingStatus
    {
        RequestPending = 0,
        ParsingRequestLine = 1,
        ParsingHeaders = 2,
        AppStarted = 3,
        HeadersCommitted = 4,
        HeadersFlushed = 5,
        ResponseCompleted = 6,
    }
    [System.FlagsAttribute]
    internal enum TransferCoding
    {
        None = 0,
        Chunked = 1,
        Other = 2,
    }
}

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal sealed partial class Http2ConnectionErrorException : System.Exception
    {
        public Http2ConnectionErrorException(string message, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode errorCode) { }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode ErrorCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    [System.FlagsAttribute]
    internal enum Http2ContinuationFrameFlags : byte
    {
        NONE = (byte)0,
        END_HEADERS = (byte)4,
    }
    [System.FlagsAttribute]
    internal enum Http2DataFrameFlags : byte
    {
        NONE = (byte)0,
        END_STREAM = (byte)1,
        PADDED = (byte)8,
    }
    internal enum Http2ErrorCode : uint
    {
        NO_ERROR = (uint)0,
        PROTOCOL_ERROR = (uint)1,
        INTERNAL_ERROR = (uint)2,
        FLOW_CONTROL_ERROR = (uint)3,
        SETTINGS_TIMEOUT = (uint)4,
        STREAM_CLOSED = (uint)5,
        FRAME_SIZE_ERROR = (uint)6,
        REFUSED_STREAM = (uint)7,
        CANCEL = (uint)8,
        COMPRESSION_ERROR = (uint)9,
        CONNECT_ERROR = (uint)10,
        ENHANCE_YOUR_CALM = (uint)11,
        INADEQUATE_SECURITY = (uint)12,
        HTTP_1_1_REQUIRED = (uint)13,
    }
    internal partial class Http2Frame
    {
        public Http2Frame() { }
        public bool ContinuationEndHeaders { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ContinuationFrameFlags ContinuationFlags { get { throw null; } set { } }
        public bool DataEndStream { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2DataFrameFlags DataFlags { get { throw null; } set { } }
        public bool DataHasPadding { get { throw null; } }
        public byte DataPadLength { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int DataPayloadLength { get { throw null; } }
        public byte Flags { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode GoAwayErrorCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int GoAwayLastStreamId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool HeadersEndHeaders { get { throw null; } }
        public bool HeadersEndStream { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2HeadersFrameFlags HeadersFlags { get { throw null; } set { } }
        public bool HeadersHasPadding { get { throw null; } }
        public bool HeadersHasPriority { get { throw null; } }
        public byte HeadersPadLength { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int HeadersPayloadLength { get { throw null; } }
        public byte HeadersPriorityWeight { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int HeadersStreamDependency { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int PayloadLength { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool PingAck { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2PingFrameFlags PingFlags { get { throw null; } set { } }
        public bool PriorityIsExclusive { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int PriorityStreamDependency { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public byte PriorityWeight { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode RstStreamErrorCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool SettingsAck { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2SettingsFrameFlags SettingsFlags { get { throw null; } set { } }
        public int StreamId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2FrameType Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int WindowUpdateSizeIncrement { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public void PrepareContinuation(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ContinuationFrameFlags flags, int streamId) { }
        public void PrepareData(int streamId, byte? padLength = default(byte?)) { }
        public void PrepareGoAway(int lastStreamId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode errorCode) { }
        public void PrepareHeaders(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2HeadersFrameFlags flags, int streamId) { }
        public void PreparePing(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2PingFrameFlags flags) { }
        public void PreparePriority(int streamId, int streamDependency, bool exclusive, byte weight) { }
        public void PrepareRstStream(int streamId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode errorCode) { }
        public void PrepareSettings(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2SettingsFrameFlags flags) { }
        public void PrepareWindowUpdate(int streamId, int sizeIncrement) { }
        internal object ShowFlags() { throw null; }
        public override string ToString() { throw null; }
    }
    internal enum Http2FrameType : byte
    {
        DATA = (byte)0,
        HEADERS = (byte)1,
        PRIORITY = (byte)2,
        RST_STREAM = (byte)3,
        SETTINGS = (byte)4,
        PUSH_PROMISE = (byte)5,
        PING = (byte)6,
        GOAWAY = (byte)7,
        WINDOW_UPDATE = (byte)8,
        CONTINUATION = (byte)9,
    }
    [System.FlagsAttribute]
    internal enum Http2HeadersFrameFlags : byte
    {
        NONE = (byte)0,
        END_STREAM = (byte)1,
        END_HEADERS = (byte)4,
        PADDED = (byte)8,
        PRIORITY = (byte)32,
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal readonly partial struct Http2PeerSetting
    {
        private readonly int _dummyPrimitive;
        public Http2PeerSetting(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2SettingsParameter parameter, uint value) { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2SettingsParameter Parameter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public uint Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    internal partial class Http2PeerSettings
    {
        public const bool DefaultEnablePush = true;
        public const uint DefaultHeaderTableSize = (uint)4096;
        public const uint DefaultInitialWindowSize = (uint)65535;
        public const uint DefaultMaxConcurrentStreams = (uint)4294967295;
        public const uint DefaultMaxFrameSize = (uint)16384;
        public const uint DefaultMaxHeaderListSize = (uint)4294967295;
        internal const int MaxAllowedMaxFrameSize = 16777215;
        public const uint MaxWindowSize = (uint)2147483647;
        internal const int MinAllowedMaxFrameSize = 16384;
        public Http2PeerSettings() { }
        public bool EnablePush { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public uint HeaderTableSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public uint InitialWindowSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public uint MaxConcurrentStreams { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public uint MaxFrameSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public uint MaxHeaderListSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal System.Collections.Generic.IList<Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2PeerSetting> GetNonProtocolDefaults() { throw null; }
        public void Update(System.Collections.Generic.IList<Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2PeerSetting> settings) { }
    }
    [System.FlagsAttribute]
    internal enum Http2PingFrameFlags : byte
    {
        NONE = (byte)0,
        ACK = (byte)1,
    }
    [System.FlagsAttribute]
    internal enum Http2SettingsFrameFlags : byte
    {
        NONE = (byte)0,
        ACK = (byte)1,
    }
    internal enum Http2SettingsParameter : ushort
    {
        SETTINGS_HEADER_TABLE_SIZE = (ushort)1,
        SETTINGS_ENABLE_PUSH = (ushort)2,
        SETTINGS_MAX_CONCURRENT_STREAMS = (ushort)3,
        SETTINGS_INITIAL_WINDOW_SIZE = (ushort)4,
        SETTINGS_MAX_FRAME_SIZE = (ushort)5,
        SETTINGS_MAX_HEADER_LIST_SIZE = (ushort)6,
    }
    internal sealed partial class Http2StreamErrorException : System.Exception
    {
        public Http2StreamErrorException(int streamId, string message, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode errorCode) { }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode ErrorCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public int StreamId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
}

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal partial struct FlowControl
    {
        private int _dummyPrimitive;
        public FlowControl(uint initialWindowSize) { throw null; }
        public int Available { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool IsAborted { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void Abort() { }
        public void Advance(int bytes) { }
        public bool TryUpdateWindow(int bytes) { throw null; }
    }
    internal partial class InputFlowControl
    {
        public InputFlowControl(uint initialWindowSize, uint minWindowSizeIncrement) { }
        public bool IsAvailabilityLow { get { throw null; } }
        public int Abort() { throw null; }
        public void StopWindowUpdates() { }
        public bool TryAdvance(int bytes) { throw null; }
        public bool TryUpdateWindow(int bytes, out int updateSize) { throw null; }
    }
}

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack
{
    internal partial class DynamicTable
    {
        public DynamicTable(int maxSize) { }
        public int Count { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack.HeaderField this[int index] { get { throw null; } }
        public int MaxSize { get { throw null; } }
        public int Size { get { throw null; } }
        public void Insert(System.Span<byte> name, System.Span<byte> value) { }
        public void Resize(int maxSize) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal readonly partial struct HeaderField
    {
        private readonly object _dummy;
        public HeaderField(System.Span<byte> name, System.Span<byte> value) { throw null; }
        public int Length { get { throw null; } }
        public byte[] Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public byte[] Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public static int GetLength(int nameLength, int valueLength) { throw null; }
    }
    internal partial class HPackDecoder
    {
        public HPackDecoder(int maxDynamicTableSize, int maxRequestHeaderFieldSize) { }
        internal HPackDecoder(int maxDynamicTableSize, int maxRequestHeaderFieldSize, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack.DynamicTable dynamicTable) { }
        public void Decode(in System.Buffers.ReadOnlySequence<byte> data, bool endHeaders, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpHeadersHandler handler) { }
    }
    internal sealed partial class HPackDecodingException : System.Exception
    {
        public HPackDecodingException(string message) { }
        public HPackDecodingException(string message, System.Exception innerException) { }
    }
    internal sealed partial class HPackEncodingException : System.Exception
    {
        public HPackEncodingException(string message) { }
        public HPackEncodingException(string message, System.Exception innerException) { }
    }
}

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal partial class BodyControl
    {
        public BodyControl(Microsoft.AspNetCore.Http.Features.IHttpBodyControlFeature bodyControl, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpResponseControl responseControl) { }
        public void Abort(System.Exception error) { }
        public (System.IO.Stream request, System.IO.Stream response, System.IO.Pipelines.PipeReader reader, System.IO.Pipelines.PipeWriter writer) Start(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.MessageBody body) { throw null; }
        public System.Threading.Tasks.Task StopAsync() { throw null; }
        public System.IO.Stream Upgrade() { throw null; }
    }
    internal partial class ConnectionManager
    {
        public ConnectionManager(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace trace, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ResourceCounter upgradedConnections) { }
        public ConnectionManager(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace trace, long? upgradedConnectionLimit) { }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ResourceCounter UpgradedConnectionCount { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<bool> AbortAllConnectionsAsync() { throw null; }
        public void AddConnection(long id, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.KestrelConnection connection) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<bool> CloseAllConnectionsAsync(System.Threading.CancellationToken token) { throw null; }
        public void RemoveConnection(long id) { }
        public void Walk(System.Action<Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.KestrelConnection> callback) { }
    }
    internal partial class Heartbeat : System.IDisposable
    {
        public static readonly System.TimeSpan Interval;
        public Heartbeat(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IHeartbeatHandler[] callbacks, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ISystemClock systemClock, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IDebugger debugger, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace trace) { }
        public void Dispose() { }
        internal void OnHeartbeat() { }
        public void Start() { }
    }
    internal static partial class HttpUtilities
    {
        public const string Http10Version = "HTTP/1.0";
        public const string Http11Version = "HTTP/1.1";
        public const string Http2Version = "HTTP/2";
        public const string HttpsUriScheme = "https://";
        public const string HttpUriScheme = "http://";
        public static string GetAsciiOrUTF8StringNonNullCharacters(this System.Span<byte> span) { throw null; }
        public static string GetAsciiStringEscaped(this System.Span<byte> span, int maxChars) { throw null; }
        public static string GetAsciiStringNonNullCharacters(this System.Span<byte> span) { throw null; }
        public static string GetHeaderName(this System.Span<byte> span) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static bool GetKnownHttpScheme(this System.Span<byte> span, out Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpScheme knownScheme) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]internal unsafe static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod GetKnownMethod(byte* data, int length, out int methodLength) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static bool GetKnownMethod(this System.Span<byte> span, out Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod method, out int length) { throw null; }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod GetKnownMethod(string value) { throw null; }
        public static bool IsHostHeaderValid(string hostText) { throw null; }
        public static string MethodToString(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod method) { throw null; }
        public static string SchemeToString(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpScheme scheme) { throw null; }
        public static string VersionToString(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpVersion httpVersion) { throw null; }
    }
    internal partial interface IDebugger
    {
        bool IsAttached { get; }
    }
    internal partial interface IHeartbeatHandler
    {
        void OnHeartbeat(System.DateTimeOffset now);
    }
    internal partial interface IKestrelTrace : Microsoft.Extensions.Logging.ILogger
    {
        void ApplicationAbortedConnection(string connectionId, string traceIdentifier);
        void ApplicationError(string connectionId, string traceIdentifier, System.Exception ex);
        void ApplicationNeverCompleted(string connectionId);
        void ConnectionAccepted(string connectionId);
        void ConnectionBadRequest(string connectionId, Microsoft.AspNetCore.Server.Kestrel.Core.BadHttpRequestException ex);
        void ConnectionDisconnect(string connectionId);
        void ConnectionHeadResponseBodyWrite(string connectionId, long count);
        void ConnectionKeepAlive(string connectionId);
        void ConnectionPause(string connectionId);
        void ConnectionRejected(string connectionId);
        void ConnectionResume(string connectionId);
        void ConnectionStart(string connectionId);
        void ConnectionStop(string connectionId);
        void HeartbeatSlow(System.TimeSpan interval, System.DateTimeOffset now);
        void HPackDecodingError(string connectionId, int streamId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack.HPackDecodingException ex);
        void HPackEncodingError(string connectionId, int streamId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack.HPackEncodingException ex);
        void Http2ConnectionClosed(string connectionId, int highestOpenedStreamId);
        void Http2ConnectionClosing(string connectionId);
        void Http2ConnectionError(string connectionId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ConnectionErrorException ex);
        void Http2FrameReceived(string connectionId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2Frame frame);
        void Http2FrameSending(string connectionId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2Frame frame);
        void Http2StreamError(string connectionId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2StreamErrorException ex);
        void Http2StreamResetAbort(string traceIdentifier, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode error, Microsoft.AspNetCore.Connections.ConnectionAbortedException abortReason);
        void NotAllConnectionsAborted();
        void NotAllConnectionsClosedGracefully();
        void RequestBodyDone(string connectionId, string traceIdentifier);
        void RequestBodyDrainTimedOut(string connectionId, string traceIdentifier);
        void RequestBodyMinimumDataRateNotSatisfied(string connectionId, string traceIdentifier, double rate);
        void RequestBodyNotEntirelyRead(string connectionId, string traceIdentifier);
        void RequestBodyStart(string connectionId, string traceIdentifier);
        void RequestProcessingError(string connectionId, System.Exception ex);
        void ResponseMinimumDataRateNotSatisfied(string connectionId, string traceIdentifier);
    }
    internal partial interface ISystemClock
    {
        System.DateTimeOffset UtcNow { get; }
        long UtcNowTicks { get; }
        System.DateTimeOffset UtcNowUnsynchronized { get; }
    }
    internal partial interface ITimeoutControl
    {
        Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.TimeoutReason TimerReason { get; }
        void BytesRead(long count);
        void BytesWrittenToBuffer(Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate minRate, long count);
        void CancelTimeout();
        void InitializeHttp2(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl.InputFlowControl connectionInputFlowControl);
        void ResetTimeout(long ticks, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.TimeoutReason timeoutReason);
        void SetTimeout(long ticks, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.TimeoutReason timeoutReason);
        void StartRequestBody(Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate minRate);
        void StartTimingRead();
        void StartTimingWrite();
        void StopRequestBody();
        void StopTimingRead();
        void StopTimingWrite();
    }
    internal partial interface ITimeoutHandler
    {
        void OnTimeout(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.TimeoutReason reason);
    }
    internal partial class KestrelConnection : Microsoft.AspNetCore.Connections.Features.IConnectionCompleteFeature, Microsoft.AspNetCore.Connections.Features.IConnectionHeartbeatFeature, Microsoft.AspNetCore.Connections.Features.IConnectionLifetimeNotificationFeature, System.Threading.IThreadPoolWorkItem
    {
        public KestrelConnection(long id, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.ServiceContext serviceContext, Microsoft.AspNetCore.Connections.ConnectionDelegate connectionDelegate, Microsoft.AspNetCore.Connections.ConnectionContext connectionContext, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace logger) { }
        public System.Threading.CancellationToken ConnectionClosedRequested { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Threading.Tasks.Task ExecutionTask { get { throw null; } }
        public Microsoft.AspNetCore.Connections.ConnectionContext TransportConnection { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public void Complete() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        internal System.Threading.Tasks.Task ExecuteAsync() { throw null; }
        public System.Threading.Tasks.Task FireOnCompletedAsync() { throw null; }
        void Microsoft.AspNetCore.Connections.Features.IConnectionCompleteFeature.OnCompleted(System.Func<object, System.Threading.Tasks.Task> callback, object state) { }
        public void OnHeartbeat(System.Action<object> action, object state) { }
        public void RequestClose() { }
        void System.Threading.IThreadPoolWorkItem.Execute() { }
        public void TickHeartbeat() { }
    }
    internal abstract partial class ResourceCounter
    {
        protected ResourceCounter() { }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ResourceCounter Unlimited { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ResourceCounter Quota(long amount) { throw null; }
        public abstract void ReleaseOne();
        public abstract bool TryLockOne();
        internal partial class FiniteCounter : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ResourceCounter
        {
            public FiniteCounter(long max) { }
            internal long Count { get { throw null; } set { } }
            public override void ReleaseOne() { }
            public override bool TryLockOne() { throw null; }
        }
    }
    internal enum TimeoutReason
    {
        None = 0,
        KeepAlive = 1,
        RequestHeaders = 2,
        ReadDataRate = 3,
        WriteDataRate = 4,
        RequestBodyDrain = 5,
        TimeoutFeature = 6,
    }
}

namespace System.Buffers
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal ref partial struct BufferWriter<T> where T : System.Buffers.IBufferWriter<byte>
    {
        private T _output;
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public BufferWriter(T output) { throw null; }
        public long BytesCommitted { get { throw null; } }
        public System.Span<byte> Span { get { throw null; } }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public void Advance(int count) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public void Commit() { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public void Ensure(int count = 1) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public void Write(System.ReadOnlySpan<byte> source) { }
    }
}

namespace System.Diagnostics
{
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Constructor | System.AttributeTargets.Method | System.AttributeTargets.Struct, Inherited=false)]
    internal sealed partial class StackTraceHiddenAttribute : System.Attribute
    {
        public StackTraceHiddenAttribute() { }
    }
}
