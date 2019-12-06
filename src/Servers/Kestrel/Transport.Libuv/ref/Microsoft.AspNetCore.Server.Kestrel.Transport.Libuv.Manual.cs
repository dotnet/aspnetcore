// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Connections
{
    internal abstract partial class TransportConnection : Microsoft.AspNetCore.Connections.ConnectionContext
    {
        public TransportConnection() { }
        public System.IO.Pipelines.IDuplexPipe Application { get { throw null; } set { } }
        public override System.Threading.CancellationToken ConnectionClosed { get { throw null; } set { } }
        public override string ConnectionId { get { throw null; } set { } }
        public override Microsoft.AspNetCore.Http.Features.IFeatureCollection Features { get { throw null; } }
        public override System.Collections.Generic.IDictionary<object, object> Items { get { throw null; } set { } }
        public override System.Net.EndPoint LocalEndPoint { get { throw null; } set { } }
        public virtual System.Buffers.MemoryPool<byte> MemoryPool { get { throw null; } }
        public override System.Net.EndPoint RemoteEndPoint { get { throw null; } set { } }
        public override System.IO.Pipelines.IDuplexPipe Transport { get { throw null; } set { } }
        internal void ResetFeatureCollection() { }
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv
{
    public partial class LibuvTransportOptions
    {
        internal System.Func<System.Buffers.MemoryPool<byte>> MemoryPoolFactory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    internal partial class LibuvConnectionListener : Microsoft.AspNetCore.Connections.IConnectionListener
    {
        public LibuvConnectionListener(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvTransportContext context, System.Net.EndPoint endPoint) { }
        public LibuvConnectionListener(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions uv, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvTransportContext context, System.Net.EndPoint endPoint) { }
        public Microsoft.Extensions.Hosting.IHostApplicationLifetime AppLifetime { get { throw null; } }
        public System.Net.EndPoint EndPoint { get { throw null; } set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions Libuv { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace Log { get { throw null; } }
        public System.Collections.Generic.List<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvThread> Threads { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvTransportContext TransportContext { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.LibuvTransportOptions TransportOptions { get { throw null; } }
        public System.Threading.Tasks.ValueTask<Microsoft.AspNetCore.Connections.ConnectionContext> AcceptAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        internal System.Threading.Tasks.Task BindAsync() { throw null; }
        public System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        internal System.Threading.Tasks.Task StopThreadsAsync() { throw null; }
        public System.Threading.Tasks.ValueTask UnbindAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    internal static partial class LibuvConstants
    {
        public static readonly int? EADDRINUSE;
        public static readonly int? ECANCELED;
        public static readonly int? ECONNRESET;
        public static readonly int? EINVAL;
        public static readonly int? ENOTCONN;
        public static readonly int? ENOTSUP;
        public const int EOF = -4095;
        public static readonly int? EPIPE;
        public const int ListenBacklog = 128;
        public static bool IsConnectionReset(int errno) { throw null; }
    }
    internal partial class LibuvTrace : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace
    {
        public LibuvTrace(Microsoft.Extensions.Logging.ILogger logger) { }
        public System.IDisposable BeginScope<TState>(TState state) { throw null; }
        public void ConnectionError(string connectionId, System.Exception ex) { }
        public void ConnectionPause(string connectionId) { }
        public void ConnectionRead(string connectionId, int count) { }
        public void ConnectionReadFin(string connectionId) { }
        public void ConnectionReset(string connectionId) { }
        public void ConnectionResume(string connectionId) { }
        public void ConnectionWrite(string connectionId, int count) { }
        public void ConnectionWriteCallback(string connectionId, int status) { }
        public void ConnectionWriteFin(string connectionId, string reason) { }
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) { throw null; }
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, System.Exception exception, System.Func<TState, System.Exception, string> formatter) { }
    }

    internal partial class LibuvTransportFactory
    {
        public LibuvTransportFactory(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.LibuvTransportOptions> options, Microsoft.Extensions.Hosting.IHostApplicationLifetime applicationLifetime, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public System.Threading.Tasks.ValueTask<Microsoft.AspNetCore.Connections.IConnectionListener> BindAsync(System.Net.EndPoint endpoint, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    internal partial struct UvWriteResult
    {
        public UvWriteResult(int status, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvException error) { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvException Error { get { throw null; } }
        public int Status { get { throw null; } }
    }
    internal partial class LibuvAwaitable<TRequest> : System.Runtime.CompilerServices.ICriticalNotifyCompletion where TRequest : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvRequest
    {
        public static readonly System.Action<TRequest, int, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvException, object> Callback;
        public LibuvAwaitable() { }
        public bool IsCompleted { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvAwaitable<TRequest> GetAwaiter() { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.UvWriteResult GetResult() { throw null; }
        public void OnCompleted(System.Action continuation) { }
        public void UnsafeOnCompleted(System.Action continuation) { }
    }
    internal partial class LibuvConnection : Microsoft.AspNetCore.Connections.TransportConnection
    {
        public LibuvConnection(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle socket, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace log, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvThread thread, System.Net.IPEndPoint remoteEndPoint, System.Net.IPEndPoint localEndPoint, System.IO.Pipelines.PipeOptions inputOptions = null, System.IO.Pipelines.PipeOptions outputOptions = null, long? maxReadBufferSize = default(long?), long? maxWriteBufferSize = default(long?)) { }
        public System.IO.Pipelines.PipeWriter Input { get { throw null; } }
        public override System.Buffers.MemoryPool<byte> MemoryPool { get { throw null; } }
        public System.IO.Pipelines.PipeReader Output { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvOutputConsumer OutputConsumer { get { throw null; } set { } }
        public override void Abort(Microsoft.AspNetCore.Connections.ConnectionAbortedException abortReason) { }
        public override System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        public void Start() { }
    }
    internal partial class LibuvOutputConsumer
    {
        public LibuvOutputConsumer(System.IO.Pipelines.PipeReader pipe, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvThread thread, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle socket, string connectionId, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace log) { }
        public System.Threading.Tasks.Task WriteOutputAsync() { throw null; }
    }
    internal partial class WriteReqPool
    {
        public WriteReqPool(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvThread thread, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace log) { }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvWriteReq Allocate() { throw null; }
        public void Dispose() { }
        public void Return(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvWriteReq req) { }
    }
    internal partial class LibuvTransportContext
    {
        public LibuvTransportContext() { }
        public Microsoft.Extensions.Hosting.IHostApplicationLifetime AppLifetime { get { throw null; } set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace Log { get { throw null; } set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.LibuvTransportOptions Options { get { throw null; } set { } }
    }
    internal partial class LibuvThread : System.IO.Pipelines.PipeScheduler
    {
        public LibuvThread(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions libuv, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvTransportContext libuvTransportContext, int maxLoops = 8) { }
        public LibuvThread(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions libuv, Microsoft.Extensions.Hosting.IHostApplicationLifetime appLifetime, System.Buffers.MemoryPool<byte> pool, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace log, int maxLoops = 8) { }
        public System.Exception FatalError { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle Loop { get { throw null; } }
        public System.Buffers.MemoryPool<byte> MemoryPool { get { throw null; } }
        public System.Action<System.Action<System.IntPtr>, System.IntPtr> QueueCloseHandle { get { throw null; } }
        public System.Collections.Generic.List<System.WeakReference> Requests { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.WriteReqPool WriteReqPool { get { throw null; } }
        public System.Threading.Tasks.Task PostAsync<T>(System.Action<T> callback, T state) { throw null; }
        public void Post<T>(System.Action<T> callback, T state) { }
        public override void Schedule(System.Action<object> action, object state) { }
        public System.Threading.Tasks.Task StartAsync() { throw null; }
        public System.Threading.Tasks.Task StopAsync(System.TimeSpan timeout) { throw null; }
        public void Walk(System.Action<System.IntPtr> callback) { }
    }
    internal partial interface IAsyncDisposable
    {
        System.Threading.Tasks.Task DisposeAsync();
    }
    internal partial interface ILibuvTrace
    {
        void ConnectionError(string connectionId, System.Exception ex);
        void ConnectionPause(string connectionId);
        void ConnectionRead(string connectionId, int count);
        void ConnectionReadFin(string connectionId);
        void ConnectionReset(string connectionId);
        void ConnectionResume(string connectionId);
        void ConnectionWrite(string connectionId, int count);
        void ConnectionWriteCallback(string connectionId, int status);
        void ConnectionWriteFin(string connectionId, string reason);
    }
    internal partial class Listener : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ListenerContext, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.IAsyncDisposable
    {
        public Listener(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvTransportContext transportContext) : base (default(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvTransportContext)) { }
        protected Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle ListenSocket { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace Log { get { throw null; } }
        protected virtual void DispatchConnection(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle socket) { }
        public virtual System.Threading.Tasks.Task DisposeAsync() { throw null; }
        public System.Threading.Tasks.Task StartAsync(System.Net.EndPoint endPoint, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvThread thread) { throw null; }
    }
    internal partial class ListenerContext
    {
        public ListenerContext(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvTransportContext transportContext) { }
        public System.Net.EndPoint EndPoint { get { throw null; } set { } }
        public System.IO.Pipelines.PipeOptions InputOptions { get { throw null; } set { } }
        public System.IO.Pipelines.PipeOptions OutputOptions { get { throw null; } set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvThread Thread { get { throw null; } set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvTransportContext TransportContext { get { throw null; } set { } }
        public System.Threading.Tasks.Task AbortQueuedConnectionAsync() { throw null; }
        public System.Threading.Tasks.ValueTask<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvConnection> AcceptAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle CreateAcceptSocket() { throw null; }
        protected internal void HandleConnection(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle socket) { }
        protected void StopAcceptingConnections() { }
    }

    internal partial class ListenerPrimary : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Listener
    {
        public ListenerPrimary(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvTransportContext transportContext) : base (default(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvTransportContext)) { }
        public int UvPipeCount { get { throw null; } }
        protected override void DispatchConnection(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle socket) { }
        public override System.Threading.Tasks.Task DisposeAsync() { throw null; }
        public System.Threading.Tasks.Task StartAsync(string pipeName, byte[] pipeMessage, System.Net.EndPoint endPoint, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvThread thread) { throw null; }
    }
    internal partial class ListenerSecondary : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ListenerContext, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.IAsyncDisposable
    {
        public ListenerSecondary(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvTransportContext transportContext) : base (default(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvTransportContext)) { }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace Log { get { throw null; } }
        public System.Threading.Tasks.Task DisposeAsync() { throw null; }
        public System.Threading.Tasks.Task StartAsync(string pipeName, byte[] pipeMessage, System.Net.EndPoint endPoint, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvThread thread) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking
{
    internal partial class UvTcpHandle : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle
    {
        public UvTcpHandle(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace logger) : base (default(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace)) { }
        public void Bind(System.Net.IPEndPoint endPoint) { }
        public System.Net.IPEndPoint GetPeerIPEndPoint() { throw null; }
        public System.Net.IPEndPoint GetSockIPEndPoint() { throw null; }
        public void Init(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle loop, System.Action<System.Action<System.IntPtr>, System.IntPtr> queueCloseHandle) { }
        public void NoDelay(bool enable) { }
        public void Open(System.IntPtr fileDescriptor) { }
    }
    internal partial struct SockAddr
    {
        public SockAddr(long ignored) { throw null; }
        public uint ScopeId { get { throw null; } set { } }
        public System.Net.IPEndPoint GetIPEndPoint() { throw null; }
    }
    internal partial class UvPipeHandle : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle
    {
        public UvPipeHandle(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace logger) : base (default(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace)) { }
        public void Bind(string name) { }
        public void Init(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle loop, System.Action<System.Action<System.IntPtr>, System.IntPtr> queueCloseHandle, bool ipc = false) { }
        public void Open(System.IntPtr fileDescriptor) { }
        public int PendingCount() { throw null; }
    }
    internal partial class UvConnectRequest : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvRequest
    {
        public UvConnectRequest(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace logger) : base (default(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace)) { }
        public void Connect(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvPipeHandle pipe, string name, System.Action<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvConnectRequest, int, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvException, object> callback, object state) { }
        public void DangerousInit(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle loop) { }
        public override void Init(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvThread thread) { }
    }
    internal partial class UvTimerHandle : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvHandle
    {
        public UvTimerHandle(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace logger) : base (default(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace)) { }
        public void Init(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle loop, System.Action<System.Action<System.IntPtr>, System.IntPtr> queueCloseHandle) { }
        public void Start(System.Action<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvTimerHandle> callback, long timeout, long repeat) { }
        public void Stop() { }
    }
    internal partial class UvAsyncHandle : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvHandle
    {
        public UvAsyncHandle(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace logger) : base (default(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace)) { }
        public void Init(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle loop, System.Action callback, System.Action<System.Action<System.IntPtr>, System.IntPtr> queueCloseHandle) { }
        protected override bool ReleaseHandle() { throw null; }
        public void Send() { }
    }
    internal partial class UvRequest : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvMemory
    {
        protected UvRequest(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace logger) : base (default(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace), default(System.Runtime.InteropServices.GCHandleType)) { }
        public virtual void Init(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvThread thread) { }
        protected override bool ReleaseHandle() { throw null; }
    }
    internal partial class UvWriteReq : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvRequest
    {
        public UvWriteReq(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace logger) : base (default(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace)) { }
        public void DangerousInit(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle loop) { }
        public override void Init(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvThread thread) { }
        public void Write2(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle handle, System.ArraySegment<System.ArraySegment<byte>> bufs, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle sendHandle, System.Action<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvWriteReq, int, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvException, object> callback, object state) { }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvAwaitable<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvWriteReq> WriteAsync(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle handle, System.ArraySegment<System.ArraySegment<byte>> bufs) { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvAwaitable<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvWriteReq> WriteAsync(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle handle, in System.Buffers.ReadOnlySequence<byte> buffer) { throw null; }
    }
    internal partial class UvException : System.Exception
    {
        public UvException(string message, int statusCode) { }
        public int StatusCode { get { throw null; } }
    }
    internal abstract partial class UvMemory : System.Runtime.InteropServices.SafeHandle
    {
        protected readonly Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace _log;
        protected int _threadId;
        protected Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions _uv;
        protected UvMemory(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace logger, System.Runtime.InteropServices.GCHandleType handleType) : base(System.IntPtr.Zero, true) { }
        public override bool IsInvalid { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions Libuv { get { throw null; } }
        public int ThreadId { get { throw null; } }
        protected void CreateMemory(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions uv, int threadId, int size) { }
        protected static void DestroyMemory(System.IntPtr memory) { }
        protected static void DestroyMemory(System.IntPtr memory, System.IntPtr gcHandlePtr) { }
        public static THandle FromIntPtr<THandle>(System.IntPtr handle) { throw null; }
        public System.IntPtr InternalGetHandle() { throw null; }
        public void Validate(bool closed = false) { }
    }
    internal partial class UvLoopHandle : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvMemory
    {
        public UvLoopHandle(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace logger) : base (default(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace), default(System.Runtime.InteropServices.GCHandleType)) { }
        public void Init(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions uv) { }
        public long Now() { throw null; }
        protected override bool ReleaseHandle() { throw null; }
        public void Run(int mode = 0) { }
        public void Stop() { }
    }
    internal abstract partial class UvHandle : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvMemory
    {
        protected UvHandle(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace logger) : base (default(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace), default(System.Runtime.InteropServices.GCHandleType)) { }
        protected void CreateHandle(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions uv, int threadId, int size, System.Action<System.Action<System.IntPtr>, System.IntPtr> queueCloseHandle) { }
        public void Reference() { }
        protected override bool ReleaseHandle() { throw null; }
        public void Unreference() { }
    }
    internal abstract partial class UvStreamHandle : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvHandle
    {
        protected UvStreamHandle(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace logger) : base (default(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace)) { }
        public void Accept(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle handle) { }
        public void Listen(int backlog, System.Action<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle, int, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvException, object> callback, object state) { }
        public void ReadStart(System.Func<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle, int, object, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_buf_t> allocCallback, System.Action<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle, int, object> readCallback, object state) { }
        public void ReadStop() { }
        protected override bool ReleaseHandle() { throw null; }
        public int TryWrite(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_buf_t buf) { throw null; }
    }
    internal partial class LibuvFunctions
    {
        public readonly bool IsWindows;
        protected System.Func<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle, int> _uv_accept;
        protected System.Func<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvAsyncHandle, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_async_cb, int> _uv_async_init;
        protected System.Func<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvAsyncHandle, int> _uv_async_send;
        protected System.Action<System.IntPtr, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_close_cb> _uv_close;
        protected System.Func<int, System.IntPtr> _uv_err_name;
        protected Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_fileno_func _uv_fileno;
        protected System.Func<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.HandleType, int> _uv_handle_size;
        protected Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_ip4_addr_func _uv_ip4_addr;
        protected Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_ip6_addr_func _uv_ip6_addr;
        protected System.Func<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle, int, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_connection_cb, int> _uv_listen;
        protected System.Func<System.IntPtr, int> _uv_loop_close;
        protected System.Func<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle, int> _uv_loop_init;
        protected System.Func<int> _uv_loop_size;
        protected System.Func<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle, long> _uv_now;
        protected System.Func<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvPipeHandle, string, int> _uv_pipe_bind;
        protected System.Action<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvConnectRequest, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvPipeHandle, string, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_connect_cb> _uv_pipe_connect;
        protected System.Func<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvPipeHandle, int, int> _uv_pipe_init;
        protected System.Func<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvPipeHandle, System.IntPtr, int> _uv_pipe_open;
        protected System.Func<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvPipeHandle, int> _uv_pipe_pending_count;
        protected System.Func<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_alloc_cb, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_read_cb, int> _uv_read_start;
        protected System.Func<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle, int> _uv_read_stop;
        protected System.Action<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvHandle> _uv_ref;
        protected System.Func<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.RequestType, int> _uv_req_size;
        protected System.Func<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle, int, int> _uv_run;
        protected System.Action<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle> _uv_stop;
        protected System.Func<int, System.IntPtr> _uv_strerror;
        protected Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_tcp_bind_func _uv_tcp_bind;
        protected Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_tcp_getpeername_func _uv_tcp_getpeername;
        protected Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_tcp_getsockname_func _uv_tcp_getsockname;
        protected System.Func<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvTcpHandle, int> _uv_tcp_init;
        protected System.Func<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvTcpHandle, int, int> _uv_tcp_nodelay;
        protected System.Func<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvTcpHandle, System.IntPtr, int> _uv_tcp_open;
        protected System.Func<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvTimerHandle, int> _uv_timer_init;
        protected System.Func<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvTimerHandle, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_timer_cb, long, long, int> _uv_timer_start;
        protected System.Func<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvTimerHandle, int> _uv_timer_stop;
        protected System.Func<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_buf_t[], int, int> _uv_try_write;
        protected System.Action<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvHandle> _uv_unref;
        protected System.Func<System.IntPtr, int> _uv_unsafe_async_send;
        protected System.Func<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_walk_cb, System.IntPtr, int> _uv_walk;
        protected Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_write_func _uv_write;
        protected Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_write2_func _uv_write2;
        public LibuvFunctions() { }
        public LibuvFunctions(bool onlyForTesting) { }
        public void accept(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle server, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle client) { }
        public void async_init(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle loop, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvAsyncHandle handle, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_async_cb cb) { }
        public void async_send(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvAsyncHandle handle) { }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_buf_t buf_init(System.IntPtr memory, int len) { throw null; }
        public void Check(int statusCode, out Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvException error) { throw null; }
        public void close(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvHandle handle, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_close_cb close_cb) { }
        public void close(System.IntPtr handle, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_close_cb close_cb) { }
        public string err_name(int err) { throw null; }
        public int handle_size(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.HandleType handleType) { throw null; }
        public void ip4_addr(string ip, int port, out Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.SockAddr addr, out Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvException error) { throw null; }
        public void ip6_addr(string ip, int port, out Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.SockAddr addr, out Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvException error) { throw null; }
        public void listen(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle handle, int backlog, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_connection_cb cb) { }
        public void loop_close(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle handle) { }
        public void loop_init(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle handle) { }
        public int loop_size() { throw null; }
        public long now(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle loop) { throw null; }
        public void pipe_bind(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvPipeHandle handle, string name) { }
        public void pipe_connect(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvConnectRequest req, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvPipeHandle handle, string name, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_connect_cb cb) { }
        public void pipe_init(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle loop, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvPipeHandle handle, bool ipc) { }
        public void pipe_open(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvPipeHandle handle, System.IntPtr hSocket) { }
        public int pipe_pending_count(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvPipeHandle handle) { throw null; }
        public void read_start(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle handle, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_alloc_cb alloc_cb, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_read_cb read_cb) { }
        public void read_stop(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle handle) { }
        public void @ref(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvHandle handle) { }
        public int req_size(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.RequestType reqType) { throw null; }
        public void run(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle handle, int mode) { }
        public void stop(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle handle) { }
        public string strerror(int err) { throw null; }
        public void tcp_bind(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvTcpHandle handle, ref Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.SockAddr addr, int flags) { }
        public void tcp_getpeername(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvTcpHandle handle, out Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.SockAddr addr, ref int namelen) { throw null; }
        public void tcp_getsockname(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvTcpHandle handle, out Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.SockAddr addr, ref int namelen) { throw null; }
        public void tcp_init(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle loop, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvTcpHandle handle) { }
        public void tcp_nodelay(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvTcpHandle handle, bool enable) { }
        public void tcp_open(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvTcpHandle handle, System.IntPtr hSocket) { }
        public void ThrowIfErrored(int statusCode) { }
        public void timer_init(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle loop, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvTimerHandle handle) { }
        public void timer_start(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvTimerHandle handle, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_timer_cb cb, long timeout, long repeat) { }
        public void timer_stop(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvTimerHandle handle) { }
        public int try_write(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle handle, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_buf_t[] bufs, int nbufs) { throw null; }
        public void unref(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvHandle handle) { }
        public void unsafe_async_send(System.IntPtr handle) { }
        public void uv_fileno(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvHandle handle, ref System.IntPtr socket) { }
        public void walk(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle loop, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_walk_cb walk_cb, System.IntPtr arg) { }
        public unsafe void write(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvRequest req, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle handle, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_buf_t* bufs, int nbufs, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_write_cb cb) { }
        public unsafe void write2(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvRequest req, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle handle, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_buf_t* bufs, int nbufs, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle sendHandle, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_write_cb cb) { }
        public enum HandleType
        {
            Unknown = 0,
            ASYNC = 1,
            CHECK = 2,
            FS_EVENT = 3,
            FS_POLL = 4,
            HANDLE = 5,
            IDLE = 6,
            NAMED_PIPE = 7,
            POLL = 8,
            PREPARE = 9,
            PROCESS = 10,
            STREAM = 11,
            TCP = 12,
            TIMER = 13,
            TTY = 14,
            UDP = 15,
            SIGNAL = 16,
        }
        public enum RequestType
        {
            Unknown = 0,
            REQ = 1,
            CONNECT = 2,
            WRITE = 3,
            SHUTDOWN = 4,
            UDP_SEND = 5,
            FS = 6,
            WORK = 7,
            GETADDRINFO = 8,
            GETNAMEINFO = 9,
        }
        public delegate void uv_alloc_cb(System.IntPtr server, int suggested_size, out Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_buf_t buf);
        public delegate void uv_async_cb(System.IntPtr handle);
        public partial struct uv_buf_t
        {
            private readonly System.IntPtr _field0;
            private readonly System.IntPtr _field1;
            public uv_buf_t(System.IntPtr memory, int len, bool IsWindows) {
                if (IsWindows) 
                { 
                    _field0 = (System.IntPtr)len; 
                    _field1 = memory; 
                } 
                else 
                { 
                    _field0 = memory; 
                    _field1 = (System.IntPtr)len; 
                } 
            } 
        }
        public delegate void uv_close_cb(System.IntPtr handle);
        public delegate void uv_connection_cb(System.IntPtr server, int status);
        public delegate void uv_connect_cb(System.IntPtr req, int status);
        protected delegate int uv_fileno_func(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvHandle handle, ref System.IntPtr socket);
        protected delegate int uv_ip4_addr_func(string ip, int port, out Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.SockAddr addr);
        protected delegate int uv_ip6_addr_func(string ip, int port, out Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.SockAddr addr);
        public delegate void uv_read_cb(System.IntPtr server, int nread, ref Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_buf_t buf);
        protected delegate int uv_tcp_bind_func(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvTcpHandle handle, ref Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.SockAddr addr, int flags);
        public delegate int uv_tcp_getpeername_func(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvTcpHandle handle, out Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.SockAddr addr, ref int namelen);
        public delegate int uv_tcp_getsockname_func(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvTcpHandle handle, out Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.SockAddr addr, ref int namelen);
        public delegate void uv_timer_cb(System.IntPtr handle);
        public delegate void uv_walk_cb(System.IntPtr handle, System.IntPtr arg);
        protected unsafe delegate int uv_write2_func(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvRequest req, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle handle, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_buf_t* bufs, int nbufs, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle sendHandle, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_write_cb cb);
        public delegate void uv_write_cb(System.IntPtr req, int status);
        protected unsafe delegate int uv_write_func(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvRequest req, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle handle, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_buf_t* bufs, int nbufs, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_write_cb cb);
    }
}
namespace System.Buffers
{
    internal partial class DiagnosticMemoryPool : System.Buffers.MemoryPool<byte>
    {        public DiagnosticMemoryPool(System.Buffers.MemoryPool<byte> pool, bool allowLateReturn = false, bool rentTracking = false) { }
        public bool IsDisposed { get { throw null; } }
        public override int MaxBufferSize { get { throw null; } }
        protected override void Dispose(bool disposing) { }
        public override System.Buffers.IMemoryOwner<byte> Rent(int size = -1) { throw null; }
        internal void ReportException(System.Exception exception) { }
        internal void Return(System.Buffers.DiagnosticPoolBlock block) { }
        public System.Threading.Tasks.Task WhenAllBlocksReturnedAsync(System.TimeSpan timeout) { throw null; }
    }
    internal sealed partial class DiagnosticPoolBlock : System.Buffers.MemoryManager<byte>
    {
        internal DiagnosticPoolBlock(System.Buffers.DiagnosticMemoryPool pool, System.Buffers.IMemoryOwner<byte> memoryOwner) { }
        public System.Diagnostics.StackTrace Leaser { get { throw null; } set { } }
        public override System.Memory<byte> Memory { get { throw null; } }
        protected override void Dispose(bool disposing) { }
        public override System.Span<byte> GetSpan() { throw null; }
        public override System.Buffers.MemoryHandle Pin(int byteOffset = 0) { throw null; }
        public void Track() { }
        protected override bool TryGetArray(out System.ArraySegment<byte> segment) { throw null; }
        public override void Unpin() { }
    }
    internal sealed partial class MemoryPoolBlock : System.Buffers.IMemoryOwner<byte>
    {
        internal MemoryPoolBlock(System.Buffers.SlabMemoryPool pool, System.Buffers.MemoryPoolSlab slab, int offset, int length) { }
        public System.Memory<byte> Memory { get { throw null; } }
        public System.Buffers.SlabMemoryPool Pool { get { throw null; } }
        public System.Buffers.MemoryPoolSlab Slab { get { throw null; } }
        public void Dispose() { }
        ~MemoryPoolBlock() { }
        public void Lease() { }
    }
    internal partial class MemoryPoolSlab : System.IDisposable
    {
        public MemoryPoolSlab(byte[] data) { }
        public byte[] Array { get { throw null; } }
        public bool IsActive { get { throw null; } }
        public System.IntPtr NativePointer { get { throw null; } }
        public static System.Buffers.MemoryPoolSlab Create(int length) { throw null; }
        public void Dispose() { }
        protected void Dispose(bool disposing) { }
        ~MemoryPoolSlab() { }
    }
    internal partial class MemoryPoolThrowHelper
    {
        public MemoryPoolThrowHelper() { }
        public static void ThrowArgumentOutOfRangeException(int sourceLength, int offset) { }
        public static void ThrowArgumentOutOfRangeException_BufferRequestTooLarge(int maxSize) { }
        public static void ThrowInvalidOperationException_BlockDoubleDispose(System.Buffers.DiagnosticPoolBlock block) { }
        public static void ThrowInvalidOperationException_BlockIsBackedByDisposedSlab(System.Buffers.DiagnosticPoolBlock block) { }
        public static void ThrowInvalidOperationException_BlockReturnedToDisposedPool(System.Buffers.DiagnosticPoolBlock block) { }
        public static void ThrowInvalidOperationException_BlocksWereNotReturnedInTime(int returned, int total, System.Buffers.DiagnosticPoolBlock[] blocks) { }
        public static void ThrowInvalidOperationException_DisposingPoolWithActiveBlocks(int returned, int total, System.Buffers.DiagnosticPoolBlock[] blocks) { }
        public static void ThrowInvalidOperationException_DoubleDispose() { }
        public static void ThrowInvalidOperationException_PinCountZero(System.Buffers.DiagnosticPoolBlock block) { }
        public static void ThrowInvalidOperationException_ReturningPinnedBlock(System.Buffers.DiagnosticPoolBlock block) { }
        public static void ThrowObjectDisposedException(System.Buffers.MemoryPoolThrowHelper.ExceptionArgument argument) { }
        internal enum ExceptionArgument
        {
            size = 0,
            offset = 1,
            length = 2,
            MemoryPoolBlock = 3,
            MemoryPool = 4,
        }
    }
    internal sealed partial class SlabMemoryPool : System.Buffers.MemoryPool<byte>
    {
        public SlabMemoryPool() { }
        public static int BlockSize { get { throw null; } }
        public override int MaxBufferSize { get { throw null; } }
        protected override void Dispose(bool disposing) { }
        internal void RefreshBlock(System.Buffers.MemoryPoolSlab slab, int offset, int length) { }
        public override System.Buffers.IMemoryOwner<byte> Rent(int size = -1) { throw null; }
        internal void Return(System.Buffers.MemoryPoolBlock block) { }
    }
    internal static partial class SlabMemoryPoolFactory
    {
        public static System.Buffers.MemoryPool<byte> Create() { throw null; }
        public static System.Buffers.MemoryPool<byte> CreateSlabMemoryPool() { throw null; }
    }
}
