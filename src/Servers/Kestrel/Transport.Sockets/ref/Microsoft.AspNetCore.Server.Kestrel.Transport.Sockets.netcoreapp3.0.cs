// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Hosting
{
    public static partial class WebHostBuilderSocketExtensions
    {
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder UseSockets(this Microsoft.AspNetCore.Hosting.IWebHostBuilder hostBuilder) { throw null; }
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder UseSockets(this Microsoft.AspNetCore.Hosting.IWebHostBuilder hostBuilder, System.Action<Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.SocketTransportOptions> configureOptions) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets
{
    public sealed partial class SocketTransportFactory : Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.ITransportFactory
    {
        public SocketTransportFactory(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.SocketTransportOptions> options, Microsoft.Extensions.Hosting.IHostApplicationLifetime applicationLifetime, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.ITransport Create(Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.IEndPointInformation endPointInformation, Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.IConnectionDispatcher dispatcher) { throw null; }
    }
    public partial class SocketTransportOptions
    {
        public SocketTransportOptions() { }
        public int IOQueueCount { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal
{
    public static partial class BufferExtensions
    {
        public static System.ArraySegment<byte> GetArray(this System.Memory<byte> memory) { throw null; }
        public static System.ArraySegment<byte> GetArray(this System.ReadOnlyMemory<byte> memory) { throw null; }
    }
    public partial class IOQueue : System.IO.Pipelines.PipeScheduler, System.Threading.IThreadPoolWorkItem
    {
        public IOQueue() { }
        public override void Schedule(System.Action<object> action, object state) { }
        void System.Threading.IThreadPoolWorkItem.Execute() { }
    }
    public partial interface ISocketsTrace : Microsoft.Extensions.Logging.ILogger
    {
        void ConnectionError(string connectionId, System.Exception ex);
        void ConnectionPause(string connectionId);
        void ConnectionReadFin(string connectionId);
        void ConnectionReset(string connectionId);
        void ConnectionResume(string connectionId);
        void ConnectionWriteFin(string connectionId, string reason);
    }
    public partial class SocketAwaitableEventArgs : System.Net.Sockets.SocketAsyncEventArgs, System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
    {
        public SocketAwaitableEventArgs(System.IO.Pipelines.PipeScheduler ioScheduler) { }
        public bool IsCompleted { get { throw null; } }
        public void Complete() { }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal.SocketAwaitableEventArgs GetAwaiter() { throw null; }
        public int GetResult() { throw null; }
        public void OnCompleted(System.Action continuation) { }
        protected override void OnCompleted(System.Net.Sockets.SocketAsyncEventArgs _) { }
        public void UnsafeOnCompleted(System.Action continuation) { }
    }
    public sealed partial class SocketReceiver : Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal.SocketSenderReceiverBase
    {
        public SocketReceiver(System.Net.Sockets.Socket socket, System.IO.Pipelines.PipeScheduler scheduler) : base (default(System.Net.Sockets.Socket), default(System.IO.Pipelines.PipeScheduler)) { }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal.SocketAwaitableEventArgs ReceiveAsync(System.Memory<byte> buffer) { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal.SocketAwaitableEventArgs WaitForDataAsync() { throw null; }
    }
    public sealed partial class SocketSender : Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal.SocketSenderReceiverBase
    {
        public SocketSender(System.Net.Sockets.Socket socket, System.IO.Pipelines.PipeScheduler scheduler) : base (default(System.Net.Sockets.Socket), default(System.IO.Pipelines.PipeScheduler)) { }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal.SocketAwaitableEventArgs SendAsync(System.Buffers.ReadOnlySequence<byte> buffers) { throw null; }
    }
    public abstract partial class SocketSenderReceiverBase : System.IDisposable
    {
        protected readonly Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal.SocketAwaitableEventArgs _awaitableEventArgs;
        protected readonly System.Net.Sockets.Socket _socket;
        protected SocketSenderReceiverBase(System.Net.Sockets.Socket socket, System.IO.Pipelines.PipeScheduler scheduler) { }
        public void Dispose() { }
    }
    public partial class SocketsTrace : Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal.ISocketsTrace, Microsoft.Extensions.Logging.ILogger
    {
        public SocketsTrace(Microsoft.Extensions.Logging.ILogger logger) { }
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
}
