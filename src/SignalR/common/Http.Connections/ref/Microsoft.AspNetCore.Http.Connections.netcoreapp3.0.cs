// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class ConnectionEndpointRouteBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapConnectionHandler<TConnectionHandler>(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder builder, string pattern) where TConnectionHandler : Microsoft.AspNetCore.Connections.ConnectionHandler { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapConnectionHandler<TConnectionHandler>(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder builder, string pattern, System.Action<Microsoft.AspNetCore.Http.Connections.HttpConnectionDispatcherOptions> configureOptions) where TConnectionHandler : Microsoft.AspNetCore.Connections.ConnectionHandler { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapConnections(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder builder, string pattern, Microsoft.AspNetCore.Http.Connections.HttpConnectionDispatcherOptions options, System.Action<Microsoft.AspNetCore.Connections.IConnectionBuilder> configure) { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapConnections(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder builder, string pattern, System.Action<Microsoft.AspNetCore.Connections.IConnectionBuilder> configure) { throw null; }
    }
    public static partial class ConnectionsAppBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseConnections(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, System.Action<Microsoft.AspNetCore.Http.Connections.ConnectionsRouteBuilder> configure) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Http.Connections
{
    public partial class ConnectionOptions
    {
        public ConnectionOptions() { }
        public System.TimeSpan? DisconnectTimeout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class ConnectionOptionsSetup : Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.Http.Connections.ConnectionOptions>
    {
        public static System.TimeSpan DefaultDisconectTimeout;
        public ConnectionOptionsSetup() { }
        public void Configure(Microsoft.AspNetCore.Http.Connections.ConnectionOptions options) { }
    }
    public partial class ConnectionsRouteBuilder
    {
        internal ConnectionsRouteBuilder() { }
        public void MapConnectionHandler<TConnectionHandler>(Microsoft.AspNetCore.Http.PathString path) where TConnectionHandler : Microsoft.AspNetCore.Connections.ConnectionHandler { }
        public void MapConnectionHandler<TConnectionHandler>(Microsoft.AspNetCore.Http.PathString path, System.Action<Microsoft.AspNetCore.Http.Connections.HttpConnectionDispatcherOptions> configureOptions) where TConnectionHandler : Microsoft.AspNetCore.Connections.ConnectionHandler { }
        public void MapConnections(Microsoft.AspNetCore.Http.PathString path, Microsoft.AspNetCore.Http.Connections.HttpConnectionDispatcherOptions options, System.Action<Microsoft.AspNetCore.Connections.IConnectionBuilder> configure) { }
        public void MapConnections(Microsoft.AspNetCore.Http.PathString path, System.Action<Microsoft.AspNetCore.Connections.IConnectionBuilder> configure) { }
    }
    public static partial class HttpConnectionContextExtensions
    {
        public static Microsoft.AspNetCore.Http.HttpContext GetHttpContext(this Microsoft.AspNetCore.Connections.ConnectionContext connection) { throw null; }
    }
    public partial class HttpConnectionDispatcherOptions
    {
        public HttpConnectionDispatcherOptions() { }
        public long ApplicationMaxBufferSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Authorization.IAuthorizeData> AuthorizationData { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Http.Connections.LongPollingOptions LongPolling { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public long TransportMaxBufferSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.Connections.HttpTransportType Transports { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.Connections.WebSocketOptions WebSockets { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class LongPollingOptions
    {
        public LongPollingOptions() { }
        public System.TimeSpan PollTimeout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class WebSocketOptions
    {
        public WebSocketOptions() { }
        public System.TimeSpan CloseTimeout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<System.Collections.Generic.IList<string>, string> SubProtocolSelector { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace Microsoft.AspNetCore.Http.Connections.Features
{
    public partial interface IHttpContextFeature
    {
        Microsoft.AspNetCore.Http.HttpContext HttpContext { get; set; }
    }
    public partial interface IHttpTransportFeature
    {
        Microsoft.AspNetCore.Http.Connections.HttpTransportType TransportType { get; }
    }
}
namespace Microsoft.AspNetCore.Http.Connections.Internal
{
    public partial class HttpConnectionContext : Microsoft.AspNetCore.Connections.ConnectionContext, Microsoft.AspNetCore.Connections.Features.IConnectionHeartbeatFeature, Microsoft.AspNetCore.Connections.Features.IConnectionIdFeature, Microsoft.AspNetCore.Connections.Features.IConnectionInherentKeepAliveFeature, Microsoft.AspNetCore.Connections.Features.IConnectionItemsFeature, Microsoft.AspNetCore.Connections.Features.IConnectionTransportFeature, Microsoft.AspNetCore.Connections.Features.IConnectionUserFeature, Microsoft.AspNetCore.Connections.Features.ITransferFormatFeature, Microsoft.AspNetCore.Http.Connections.Features.IHttpContextFeature, Microsoft.AspNetCore.Http.Connections.Features.IHttpTransportFeature
    {
        public HttpConnectionContext(string id, Microsoft.Extensions.Logging.ILogger logger) { }
        public HttpConnectionContext(string id, System.IO.Pipelines.IDuplexPipe transport, System.IO.Pipelines.IDuplexPipe application, Microsoft.Extensions.Logging.ILogger logger = null) { }
        public Microsoft.AspNetCore.Connections.TransferFormat ActiveFormat { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.IO.Pipelines.IDuplexPipe Application { get { throw null; } set { } }
        public System.Threading.Tasks.Task ApplicationTask { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Threading.CancellationTokenSource Cancellation { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override string ConnectionId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override Microsoft.AspNetCore.Http.Features.IFeatureCollection Features { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool HasInherentKeepAlive { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.HttpContext HttpContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override System.Collections.Generic.IDictionary<object, object> Items { get { throw null; } set { } }
        public System.DateTime LastSeenUtc { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Threading.Tasks.Task PreviousPollTask { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Threading.SemaphoreSlim StateLock { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Http.Connections.Internal.HttpConnectionStatus Status { get { throw null; } set { } }
        public Microsoft.AspNetCore.Connections.TransferFormat SupportedFormats { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override System.IO.Pipelines.IDuplexPipe Transport { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Threading.Tasks.Task TransportTask { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.Connections.HttpTransportType TransportType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Security.Claims.ClaimsPrincipal User { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Threading.SemaphoreSlim WriteLock { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task DisposeAsync(bool closeGracefully = false) { throw null; }
        public void OnHeartbeat(System.Action<object> action, object state) { }
        public void TickHeartbeat() { }
        public bool TryChangeState(Microsoft.AspNetCore.Http.Connections.Internal.HttpConnectionStatus from, Microsoft.AspNetCore.Http.Connections.Internal.HttpConnectionStatus to) { throw null; }
    }
    public partial class HttpConnectionDispatcher
    {
        public HttpConnectionDispatcher(Microsoft.AspNetCore.Http.Connections.Internal.HttpConnectionManager manager, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Http.Connections.HttpConnectionDispatcherOptions options, Microsoft.AspNetCore.Connections.ConnectionDelegate connectionDelegate) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task ExecuteNegotiateAsync(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Http.Connections.HttpConnectionDispatcherOptions options) { throw null; }
    }
    public partial class HttpConnectionManager
    {
        public HttpConnectionManager(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.Hosting.IHostApplicationLifetime appLifetime) { }
        public HttpConnectionManager(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.Hosting.IHostApplicationLifetime appLifetime, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Http.Connections.ConnectionOptions> connectionOptions) { }
        public void CloseConnections() { }
        public Microsoft.AspNetCore.Http.Connections.Internal.HttpConnectionContext CreateConnection() { throw null; }
        public Microsoft.AspNetCore.Http.Connections.Internal.HttpConnectionContext CreateConnection(System.IO.Pipelines.PipeOptions transportPipeOptions, System.IO.Pipelines.PipeOptions appPipeOptions) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task DisposeAndRemoveAsync(Microsoft.AspNetCore.Http.Connections.Internal.HttpConnectionContext connection, bool closeGracefully) { throw null; }
        public void RemoveConnection(string id) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task ScanAsync() { throw null; }
        public void Start() { }
        public bool TryGetConnection(string id, out Microsoft.AspNetCore.Http.Connections.Internal.HttpConnectionContext connection) { throw null; }
    }
    public enum HttpConnectionStatus
    {
        Active = 1,
        Disposed = 2,
        Inactive = 0,
    }
    public static partial class ServerSentEventsMessageFormatter
    {
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.Task WriteMessageAsync(System.Buffers.ReadOnlySequence<byte> payload, System.IO.Stream output) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Http.Connections.Internal.Transports
{
    public partial interface IHttpTransport
    {
        System.Threading.Tasks.Task ProcessRequestAsync(Microsoft.AspNetCore.Http.HttpContext context, System.Threading.CancellationToken token);
    }
    public partial class LongPollingTransport : Microsoft.AspNetCore.Http.Connections.Internal.Transports.IHttpTransport
    {
        public LongPollingTransport(System.Threading.CancellationToken timeoutToken, System.IO.Pipelines.PipeReader application, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task ProcessRequestAsync(Microsoft.AspNetCore.Http.HttpContext context, System.Threading.CancellationToken token) { throw null; }
    }
    public partial class ServerSentEventsTransport : Microsoft.AspNetCore.Http.Connections.Internal.Transports.IHttpTransport
    {
        public ServerSentEventsTransport(System.IO.Pipelines.PipeReader application, string connectionId, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task ProcessRequestAsync(Microsoft.AspNetCore.Http.HttpContext context, System.Threading.CancellationToken token) { throw null; }
    }
    public partial class WebSocketsTransport : Microsoft.AspNetCore.Http.Connections.Internal.Transports.IHttpTransport
    {
        public WebSocketsTransport(Microsoft.AspNetCore.Http.Connections.WebSocketOptions options, System.IO.Pipelines.IDuplexPipe application, Microsoft.AspNetCore.Http.Connections.Internal.HttpConnectionContext connection, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task ProcessRequestAsync(Microsoft.AspNetCore.Http.HttpContext context, System.Threading.CancellationToken token) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task ProcessSocketAsync(System.Net.WebSockets.WebSocket socket) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Internal
{
    public static partial class AwaitableThreadPool
    {
        public static Microsoft.AspNetCore.Internal.AwaitableThreadPool.Awaitable Yield() { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential, Size=1)]
        public readonly partial struct Awaitable : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
        {
            public bool IsCompleted { get { throw null; } }
            public Microsoft.AspNetCore.Internal.AwaitableThreadPool.Awaitable GetAwaiter() { throw null; }
            public void GetResult() { }
            public void OnCompleted(System.Action continuation) { }
            public void UnsafeOnCompleted(System.Action continuation) { }
        }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ConnectionsDependencyInjectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddConnections(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddConnections(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.Http.Connections.ConnectionOptions> options) { throw null; }
    }
}
