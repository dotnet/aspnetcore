// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Hosting
{
    public static partial class WebHostBuilderLibuvExtensions
    {
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder UseLibuv(this Microsoft.AspNetCore.Hosting.IWebHostBuilder hostBuilder) { throw null; }
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder UseLibuv(this Microsoft.AspNetCore.Hosting.IWebHostBuilder hostBuilder, System.Action<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.LibuvTransportOptions> configureOptions) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv
{
    public partial class LibuvTransportOptions
    {
        public LibuvTransportOptions() { }
        public int ThreadCount { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    public partial interface ILibuvTrace : Microsoft.Extensions.Logging.ILogger
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
    public partial class LibuvAwaitable<TRequest> : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion where TRequest : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvRequest
    {
        public static readonly System.Action<TRequest, int, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvException, object> Callback;
        public LibuvAwaitable() { }
        public bool IsCompleted { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvAwaitable<TRequest> GetAwaiter() { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.UvWriteResult GetResult() { throw null; }
        public void OnCompleted(System.Action continuation) { }
        public void UnsafeOnCompleted(System.Action continuation) { }
    }
    public partial class LibuvConnection : Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.TransportConnection, System.IDisposable
    {
        public LibuvConnection(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle socket, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace log, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvThread thread, System.Net.IPEndPoint remoteEndPoint, System.Net.IPEndPoint localEndPoint) { }
        public override System.IO.Pipelines.PipeScheduler InputWriterScheduler { get { throw null; } }
        public override System.Buffers.MemoryPool<byte> MemoryPool { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvOutputConsumer OutputConsumer { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override System.IO.Pipelines.PipeScheduler OutputReaderScheduler { get { throw null; } }
        public override void Abort(Microsoft.AspNetCore.Connections.ConnectionAbortedException abortReason) { }
        public void Dispose() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task Start() { throw null; }
    }
    public partial class LibuvOutputConsumer
    {
        public LibuvOutputConsumer(System.IO.Pipelines.PipeReader pipe, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvThread thread, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle socket, string connectionId, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace log) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task WriteOutputAsync() { throw null; }
    }
    public partial class LibuvThread : System.IO.Pipelines.PipeScheduler
    {
        public LibuvThread(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvTransport transport) { }
        public LibuvThread(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvTransport transport, int maxLoops) { }
        public System.Exception FatalError { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle Loop { get { throw null; } }
        public System.Buffers.MemoryPool<byte> MemoryPool { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Action<System.Action<System.IntPtr>, System.IntPtr> QueueCloseHandle { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Collections.Generic.List<System.WeakReference> Requests { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.WriteReqPool WriteReqPool { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Threading.Tasks.Task PostAsync<T>(System.Action<T> callback, T state) { throw null; }
        public void Post<T>(System.Action<T> callback, T state) { }
        public override void Schedule(System.Action<object> action, object state) { }
        public System.Threading.Tasks.Task StartAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task StopAsync(System.TimeSpan timeout) { throw null; }
        public void Walk(System.Action<System.IntPtr> callback) { }
    }
    public partial class LibuvTrace : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace, Microsoft.Extensions.Logging.ILogger
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
    public partial class LibuvTransport : Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.ITransport
    {
        public LibuvTransport(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvTransportContext context, Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.IEndPointInformation endPointInformation) { }
        public LibuvTransport(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions uv, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvTransportContext context, Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.IEndPointInformation endPointInformation) { }
        public Microsoft.Extensions.Hosting.IHostApplicationLifetime AppLifetime { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions Libuv { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace Log { get { throw null; } }
        public System.Collections.Generic.List<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvThread> Threads { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvTransportContext TransportContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.LibuvTransportOptions TransportOptions { get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task BindAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task StopAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task UnbindAsync() { throw null; }
    }
    public partial class LibuvTransportContext
    {
        public LibuvTransportContext() { }
        public Microsoft.Extensions.Hosting.IHostApplicationLifetime AppLifetime { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.IConnectionDispatcher ConnectionDispatcher { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace Log { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.LibuvTransportOptions Options { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class LibuvTransportFactory : Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.ITransportFactory
    {
        public LibuvTransportFactory(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.LibuvTransportOptions> options, Microsoft.Extensions.Hosting.IHostApplicationLifetime applicationLifetime, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.ITransport Create(Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.IEndPointInformation endPointInformation, Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.IConnectionDispatcher dispatcher) { throw null; }
    }
    public partial class Listener : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ListenerContext
    {
        public Listener(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvTransportContext transportContext) : base (default(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvTransportContext)) { }
        protected Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle ListenSocket { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace Log { get { throw null; } }
        protected virtual void DispatchConnection(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle socket) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task DisposeAsync() { throw null; }
        public System.Threading.Tasks.Task StartAsync(Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.IEndPointInformation endPointInformation, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvThread thread) { throw null; }
    }
    public partial class ListenerContext
    {
        public ListenerContext(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvTransportContext transportContext) { }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.IEndPointInformation EndPointInformation { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvThread Thread { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvTransportContext TransportContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle CreateAcceptSocket() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected System.Threading.Tasks.Task HandleConnectionAsync(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle socket) { throw null; }
    }
    public partial class ListenerPrimary : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Listener
    {
        public ListenerPrimary(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvTransportContext transportContext) : base (default(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvTransportContext)) { }
        public int UvPipeCount { get { throw null; } }
        protected override void DispatchConnection(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle socket) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task DisposeAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task StartAsync(string pipeName, byte[] pipeMessage, Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.IEndPointInformation endPointInformation, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvThread thread) { throw null; }
    }
    public partial class ListenerSecondary : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ListenerContext
    {
        public ListenerSecondary(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvTransportContext transportContext) : base (default(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvTransportContext)) { }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace Log { get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task DisposeAsync() { throw null; }
        public System.Threading.Tasks.Task StartAsync(string pipeName, byte[] pipeMessage, Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.IEndPointInformation endPointInformation, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvThread thread) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct UvWriteResult
    {
        private object _dummy;
        private int _dummyPrimitive;
        public UvWriteResult(int status, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvException error) { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvException Error { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public int Status { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class WriteReqPool
    {
        public WriteReqPool(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvThread thread, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace log) { }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvWriteReq Allocate() { throw null; }
        public void Dispose() { }
        public void Return(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvWriteReq req) { }
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking
{
    public partial class LibuvFunctions
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
            SIGNAL = 16,
            STREAM = 11,
            TCP = 12,
            TIMER = 13,
            TTY = 14,
            UDP = 15,
            Unknown = 0,
        }
        public enum RequestType
        {
            CONNECT = 2,
            FS = 6,
            GETADDRINFO = 8,
            GETNAMEINFO = 9,
            REQ = 1,
            SHUTDOWN = 4,
            UDP_SEND = 5,
            Unknown = 0,
            WORK = 7,
            WRITE = 3,
        }
        [System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public delegate void uv_alloc_cb(System.IntPtr server, int suggested_size, out Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_buf_t buf);
        [System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public delegate void uv_async_cb(System.IntPtr handle);
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct uv_buf_t
        {
            private int _dummyPrimitive;
            public uv_buf_t(System.IntPtr memory, int len, bool IsWindows) { throw null; }
        }
        [System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public delegate void uv_close_cb(System.IntPtr handle);
        [System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public delegate void uv_connection_cb(System.IntPtr server, int status);
        [System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public delegate void uv_connect_cb(System.IntPtr req, int status);
        [System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(System.Runtime.InteropServices.CallingConvention.Cdecl)]
        protected delegate int uv_fileno_func(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvHandle handle, ref System.IntPtr socket);
        protected delegate int uv_ip4_addr_func(string ip, int port, out Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.SockAddr addr);
        protected delegate int uv_ip6_addr_func(string ip, int port, out Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.SockAddr addr);
        [System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public delegate void uv_read_cb(System.IntPtr server, int nread, ref Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_buf_t buf);
        protected delegate int uv_tcp_bind_func(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvTcpHandle handle, ref Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.SockAddr addr, int flags);
        public delegate int uv_tcp_getpeername_func(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvTcpHandle handle, out Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.SockAddr addr, ref int namelen);
        public delegate int uv_tcp_getsockname_func(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvTcpHandle handle, out Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.SockAddr addr, ref int namelen);
        [System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public delegate void uv_timer_cb(System.IntPtr handle);
        [System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public delegate void uv_walk_cb(System.IntPtr handle, System.IntPtr arg);
        protected unsafe delegate int uv_write2_func(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvRequest req, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle handle, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_buf_t* bufs, int nbufs, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle sendHandle, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_write_cb cb);
        [System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public delegate void uv_write_cb(System.IntPtr req, int status);
        protected unsafe delegate int uv_write_func(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvRequest req, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle handle, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_buf_t* bufs, int nbufs, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_write_cb cb);
    }
    public static partial class PlatformApis
    {
        public static bool IsDarwin { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public static bool IsWindows { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static long VolatileRead(ref long value) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct SockAddr
    {
        private int _dummyPrimitive;
        public SockAddr(long ignored) { throw null; }
        public uint ScopeId { get { throw null; } set { } }
        public System.Net.IPEndPoint GetIPEndPoint() { throw null; }
    }
    public partial class UvAsyncHandle : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvHandle
    {
        public UvAsyncHandle(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace logger) : base (default(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace)) { }
        public void Init(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle loop, System.Action callback, System.Action<System.Action<System.IntPtr>, System.IntPtr> queueCloseHandle) { }
        protected override bool ReleaseHandle() { throw null; }
        public void Send() { }
    }
    public partial class UvConnectRequest : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvRequest
    {
        public UvConnectRequest(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace logger) : base (default(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace)) { }
        public void Connect(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvPipeHandle pipe, string name, System.Action<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvConnectRequest, int, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvException, object> callback, object state) { }
        public void DangerousInit(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle loop) { }
        public override void Init(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvThread thread) { }
    }
    public partial class UvException : System.Exception
    {
        public UvException(string message, int statusCode) { }
        public int StatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public abstract partial class UvHandle : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvMemory
    {
        protected UvHandle(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace logger) : base (default(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace), default(System.Runtime.InteropServices.GCHandleType)) { }
        protected void CreateHandle(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions uv, int threadId, int size, System.Action<System.Action<System.IntPtr>, System.IntPtr> queueCloseHandle) { }
        public void Reference() { }
        protected override bool ReleaseHandle() { throw null; }
        public void Unreference() { }
    }
    public partial class UvLoopHandle : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvMemory
    {
        public UvLoopHandle(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace logger) : base (default(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace), default(System.Runtime.InteropServices.GCHandleType)) { }
        public void Init(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions uv) { }
        public long Now() { throw null; }
        protected override bool ReleaseHandle() { throw null; }
        public void Run(int mode = 0) { }
        public void Stop() { }
    }
    public abstract partial class UvMemory : System.Runtime.InteropServices.SafeHandle
    {
        protected readonly Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace _log;
        protected int _threadId;
        protected Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions _uv;
        protected UvMemory(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace logger, System.Runtime.InteropServices.GCHandleType handleType = System.Runtime.InteropServices.GCHandleType.Weak) : base (default(System.IntPtr), default(bool)) { }
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
    public partial class UvPipeHandle : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle
    {
        public UvPipeHandle(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace logger) : base (default(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace)) { }
        public void Bind(string name) { }
        public void Init(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle loop, System.Action<System.Action<System.IntPtr>, System.IntPtr> queueCloseHandle, bool ipc = false) { }
        public void Open(System.IntPtr fileDescriptor) { }
        public int PendingCount() { throw null; }
    }
    public partial class UvRequest : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvMemory
    {
        protected UvRequest(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace logger) : base (default(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace), default(System.Runtime.InteropServices.GCHandleType)) { }
        public virtual void Init(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvThread thread) { }
        protected override bool ReleaseHandle() { throw null; }
    }
    public abstract partial class UvStreamHandle : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvHandle
    {
        protected UvStreamHandle(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace logger) : base (default(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace)) { }
        public void Accept(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle handle) { }
        public void Listen(int backlog, System.Action<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle, int, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvException, object> callback, object state) { }
        public void ReadStart(System.Func<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle, int, object, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_buf_t> allocCallback, System.Action<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle, int, object> readCallback, object state) { }
        public void ReadStop() { }
        protected override bool ReleaseHandle() { throw null; }
        public int TryWrite(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.LibuvFunctions.uv_buf_t buf) { throw null; }
    }
    public partial class UvTcpHandle : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle
    {
        public UvTcpHandle(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace logger) : base (default(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace)) { }
        public void Bind(System.Net.IPEndPoint endPoint) { }
        public System.Net.IPEndPoint GetPeerIPEndPoint() { throw null; }
        public System.Net.IPEndPoint GetSockIPEndPoint() { throw null; }
        public void Init(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle loop, System.Action<System.Action<System.IntPtr>, System.IntPtr> queueCloseHandle) { }
        public void NoDelay(bool enable) { }
        public void Open(System.IntPtr fileDescriptor) { }
    }
    public partial class UvTimerHandle : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvHandle
    {
        public UvTimerHandle(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace logger) : base (default(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace)) { }
        public void Init(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle loop, System.Action<System.Action<System.IntPtr>, System.IntPtr> queueCloseHandle) { }
        public void Start(System.Action<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvTimerHandle> callback, long timeout, long repeat) { }
        public void Stop() { }
    }
    public partial class UvWriteReq : Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvRequest
    {
        public UvWriteReq(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace logger) : base (default(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.ILibuvTrace)) { }
        public void DangerousInit(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvLoopHandle loop) { }
        public override void Init(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvThread thread) { }
        public void Write2(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle handle, System.ArraySegment<System.ArraySegment<byte>> bufs, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle sendHandle, System.Action<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvWriteReq, int, Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvException, object> callback, object state) { }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvAwaitable<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvWriteReq> WriteAsync(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle handle, System.ArraySegment<System.ArraySegment<byte>> bufs) { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.LibuvAwaitable<Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvWriteReq> WriteAsync(Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking.UvStreamHandle handle, System.Buffers.ReadOnlySequence<byte> buffer) { throw null; }
    }
}
