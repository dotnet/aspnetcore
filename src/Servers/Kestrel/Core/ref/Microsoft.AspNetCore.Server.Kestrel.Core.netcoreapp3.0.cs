// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Hosting
{
    public static partial class KestrelServerOptionsSystemdExtensions
    {
        public static Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions UseSystemd(this Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions options) { throw null; }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions UseSystemd(this Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions options, System.Action<Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions> configure) { throw null; }
    }
    public static partial class ListenOptionsConnectionLoggingExtensions
    {
        public static Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions UseConnectionLogging(this Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions listenOptions) { throw null; }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions UseConnectionLogging(this Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions listenOptions, string loggerName) { throw null; }
    }
    public static partial class ListenOptionsHttpsExtensions
    {
        public static Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions UseHttps(this Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions listenOptions) { throw null; }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions UseHttps(this Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions listenOptions, Microsoft.AspNetCore.Server.Kestrel.Https.HttpsConnectionAdapterOptions httpsOptions) { throw null; }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions UseHttps(this Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions listenOptions, System.Action<Microsoft.AspNetCore.Server.Kestrel.Https.HttpsConnectionAdapterOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions UseHttps(this Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions listenOptions, System.Security.Cryptography.X509Certificates.StoreName storeName, string subject) { throw null; }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions UseHttps(this Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions listenOptions, System.Security.Cryptography.X509Certificates.StoreName storeName, string subject, bool allowInvalid) { throw null; }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions UseHttps(this Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions listenOptions, System.Security.Cryptography.X509Certificates.StoreName storeName, string subject, bool allowInvalid, System.Security.Cryptography.X509Certificates.StoreLocation location) { throw null; }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions UseHttps(this Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions listenOptions, System.Security.Cryptography.X509Certificates.StoreName storeName, string subject, bool allowInvalid, System.Security.Cryptography.X509Certificates.StoreLocation location, System.Action<Microsoft.AspNetCore.Server.Kestrel.Https.HttpsConnectionAdapterOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions UseHttps(this Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions listenOptions, System.Security.Cryptography.X509Certificates.X509Certificate2 serverCertificate) { throw null; }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions UseHttps(this Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions listenOptions, System.Security.Cryptography.X509Certificates.X509Certificate2 serverCertificate, System.Action<Microsoft.AspNetCore.Server.Kestrel.Https.HttpsConnectionAdapterOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions UseHttps(this Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions listenOptions, string fileName) { throw null; }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions UseHttps(this Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions listenOptions, string fileName, string password) { throw null; }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions UseHttps(this Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions listenOptions, string fileName, string password, System.Action<Microsoft.AspNetCore.Server.Kestrel.Https.HttpsConnectionAdapterOptions> configureOptions) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel
{
    public partial class EndpointConfiguration
    {
        internal EndpointConfiguration() { }
        public Microsoft.Extensions.Configuration.IConfigurationSection ConfigSection { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Https.HttpsConnectionAdapterOptions HttpsOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool IsHttps { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions ListenOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class KestrelConfigurationLoader
    {
        internal KestrelConfigurationLoader() { }
        public Microsoft.Extensions.Configuration.IConfiguration Configuration { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions Options { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.KestrelConfigurationLoader AnyIPEndpoint(int port) { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.KestrelConfigurationLoader AnyIPEndpoint(int port, System.Action<Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions> configure) { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.KestrelConfigurationLoader Endpoint(System.Net.IPAddress address, int port) { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.KestrelConfigurationLoader Endpoint(System.Net.IPAddress address, int port, System.Action<Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions> configure) { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.KestrelConfigurationLoader Endpoint(System.Net.IPEndPoint endPoint) { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.KestrelConfigurationLoader Endpoint(System.Net.IPEndPoint endPoint, System.Action<Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions> configure) { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.KestrelConfigurationLoader Endpoint(string name, System.Action<Microsoft.AspNetCore.Server.Kestrel.EndpointConfiguration> configureOptions) { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.KestrelConfigurationLoader HandleEndpoint(ulong handle) { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.KestrelConfigurationLoader HandleEndpoint(ulong handle, System.Action<Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions> configure) { throw null; }
        public void Load() { }
        public Microsoft.AspNetCore.Server.Kestrel.KestrelConfigurationLoader LocalhostEndpoint(int port) { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.KestrelConfigurationLoader LocalhostEndpoint(int port, System.Action<Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions> configure) { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.KestrelConfigurationLoader UnixSocketEndpoint(string socketPath) { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.KestrelConfigurationLoader UnixSocketEndpoint(string socketPath, System.Action<Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions> configure) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Core
{
    public sealed partial class BadHttpRequestException : System.IO.IOException
    {
        internal BadHttpRequestException() { }
        public int StatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public static void Throw(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.RequestRejectionReason reason, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod method) { }
    }
    public partial class Http2Limits
    {
        public Http2Limits() { }
        public int HeaderTableSize { get { throw null; } set { } }
        public int InitialConnectionWindowSize { get { throw null; } set { } }
        public int InitialStreamWindowSize { get { throw null; } set { } }
        public int MaxFrameSize { get { throw null; } set { } }
        public int MaxRequestHeaderFieldSize { get { throw null; } set { } }
        public int MaxStreamsPerConnection { get { throw null; } set { } }
    }
    [System.FlagsAttribute]
    public enum HttpProtocols
    {
        Http1 = 1,
        Http1AndHttp2 = 3,
        Http2 = 2,
        None = 0,
    }
    public partial class KestrelServer : Microsoft.AspNetCore.Hosting.Server.IServer, System.IDisposable
    {
        public KestrelServer(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions> options, Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.ITransportFactory transportFactory, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.AspNetCore.Http.Features.IFeatureCollection Features { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions Options { get { throw null; } }
        public void Dispose() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task StartAsync<TContext>(Microsoft.AspNetCore.Hosting.Server.IHttpApplication<TContext> application, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public partial class KestrelServerLimits
    {
        public KestrelServerLimits() { }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Http2Limits Http2 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.TimeSpan KeepAliveTimeout { get { throw null; } set { } }
        public long? MaxConcurrentConnections { get { throw null; } set { } }
        public long? MaxConcurrentUpgradedConnections { get { throw null; } set { } }
        public long? MaxRequestBodySize { get { throw null; } set { } }
        public long? MaxRequestBufferSize { get { throw null; } set { } }
        public int MaxRequestHeaderCount { get { throw null; } set { } }
        public int MaxRequestHeadersTotalSize { get { throw null; } set { } }
        public int MaxRequestLineSize { get { throw null; } set { } }
        public long? MaxResponseBufferSize { get { throw null; } set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate MinRequestBodyDataRate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate MinResponseDataRate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.TimeSpan RequestHeadersTimeout { get { throw null; } set { } }
    }
    public partial class KestrelServerOptions
    {
        public KestrelServerOptions() { }
        public bool AddServerHeader { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool AllowSynchronousIO { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.SchedulingMode ApplicationSchedulingMode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.IServiceProvider ApplicationServices { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.KestrelConfigurationLoader ConfigurationLoader { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerLimits Limits { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.KestrelConfigurationLoader Configure() { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.KestrelConfigurationLoader Configure(Microsoft.Extensions.Configuration.IConfiguration config) { throw null; }
        public void ConfigureEndpointDefaults(System.Action<Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions> configureOptions) { }
        public void ConfigureHttpsDefaults(System.Action<Microsoft.AspNetCore.Server.Kestrel.Https.HttpsConnectionAdapterOptions> configureOptions) { }
        public void Listen(System.Net.IPAddress address, int port) { }
        public void Listen(System.Net.IPAddress address, int port, System.Action<Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions> configure) { }
        public void Listen(System.Net.IPEndPoint endPoint) { }
        public void Listen(System.Net.IPEndPoint endPoint, System.Action<Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions> configure) { }
        public void ListenAnyIP(int port) { }
        public void ListenAnyIP(int port, System.Action<Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions> configure) { }
        public void ListenHandle(ulong handle) { }
        public void ListenHandle(ulong handle, System.Action<Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions> configure) { }
        public void ListenLocalhost(int port) { }
        public void ListenLocalhost(int port, System.Action<Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions> configure) { }
        public void ListenUnixSocket(string socketPath) { }
        public void ListenUnixSocket(string socketPath, System.Action<Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions> configure) { }
    }
    public partial class ListenOptions : Microsoft.AspNetCore.Connections.IConnectionBuilder, Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.IEndPointInformation
    {
        internal ListenOptions() { }
        public System.IServiceProvider ApplicationServices { get { throw null; } }
        public System.Collections.Generic.List<Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal.IConnectionAdapter> ConnectionAdapters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public ulong FileHandle { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.FileHandleType HandleType { get { throw null; } set { } }
        public System.Net.IPEndPoint IPEndPoint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions KestrelServerOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool NoDelay { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols Protocols { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string SocketPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.ListenType Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Connections.ConnectionDelegate Build() { throw null; }
        public override string ToString() { throw null; }
        public Microsoft.AspNetCore.Connections.IConnectionBuilder Use(System.Func<Microsoft.AspNetCore.Connections.ConnectionDelegate, Microsoft.AspNetCore.Connections.ConnectionDelegate> middleware) { throw null; }
    }
    public partial class MinDataRate
    {
        public MinDataRate(double bytesPerSecond, System.TimeSpan gracePeriod) { }
        public double BytesPerSecond { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.TimeSpan GracePeriod { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal
{
    public partial class AdaptedPipeline : System.IO.Pipelines.IDuplexPipe
    {
        public AdaptedPipeline(System.IO.Pipelines.IDuplexPipe transport, System.IO.Pipelines.Pipe inputPipe, System.IO.Pipelines.Pipe outputPipe, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace log) { }
        public System.IO.Pipelines.Pipe Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace Log { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.IO.Pipelines.Pipe Output { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        System.IO.Pipelines.PipeReader System.IO.Pipelines.IDuplexPipe.Input { get { throw null; } }
        System.IO.Pipelines.PipeWriter System.IO.Pipelines.IDuplexPipe.Output { get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task RunAsync(System.IO.Stream stream) { throw null; }
    }
    public partial class ConnectionAdapterContext
    {
        internal ConnectionAdapterContext() { }
        public System.IO.Stream ConnectionStream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Http.Features.IFeatureCollection Features { get { throw null; } }
    }
    public partial interface IAdaptedConnection : System.IDisposable
    {
        System.IO.Stream ConnectionStream { get; }
    }
    public partial interface IConnectionAdapter
    {
        bool IsHttps { get; }
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal.IAdaptedConnection> OnConnectionAsync(Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal.ConnectionAdapterContext context);
    }
    public partial class LoggingConnectionAdapter : Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal.IConnectionAdapter
    {
        public LoggingConnectionAdapter(Microsoft.Extensions.Logging.ILogger logger) { }
        public bool IsHttps { get { throw null; } }
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal.IAdaptedConnection> OnConnectionAsync(Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal.ConnectionAdapterContext context) { throw null; }
    }
    public partial class RawStream : System.IO.Stream
    {
        public RawStream(System.IO.Pipelines.PipeReader input, System.IO.Pipelines.PipeWriter output) { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public override System.IAsyncResult BeginRead(byte[] buffer, int offset, int count, System.AsyncCallback callback, object state) { throw null; }
        public override System.IAsyncResult BeginWrite(byte[] buffer, int offset, int count, System.AsyncCallback callback, object state) { throw null; }
        public override int EndRead(System.IAsyncResult asyncResult) { throw null; }
        public override void EndWrite(System.IAsyncResult asyncResult) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask<int> ReadAsync(System.Memory<byte> destination, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.ValueTask WriteAsync(System.ReadOnlyMemory<byte> source, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Core.Features
{
    public partial interface IConnectionTimeoutFeature
    {
        void CancelTimeout();
        void ResetTimeout(System.TimeSpan timeSpan);
        void SetTimeout(System.TimeSpan timeSpan);
    }
    public partial interface IDecrementConcurrentConnectionCountFeature
    {
        void ReleaseConnection();
    }
    public partial interface IHttp2StreamIdFeature
    {
        int StreamId { get; }
    }
    public partial interface IHttpMinRequestBodyDataRateFeature
    {
        Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate MinDataRate { get; set; }
    }
    public partial interface IHttpMinResponseDataRateFeature
    {
        Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate MinDataRate { get; set; }
    }
    public partial interface ITlsApplicationProtocolFeature
    {
        System.ReadOnlyMemory<byte> ApplicationProtocol { get; }
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    public partial class ConnectionDispatcher : Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.IConnectionDispatcher
    {
        public ConnectionDispatcher(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.ServiceContext serviceContext, Microsoft.AspNetCore.Connections.ConnectionDelegate connectionDelegate) { }
        public System.Threading.Tasks.Task OnConnection(Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.TransportConnection connection) { throw null; }
    }
    public partial class ConnectionLimitMiddleware
    {
        public ConnectionLimitMiddleware(Microsoft.AspNetCore.Connections.ConnectionDelegate next, long connectionLimit, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace trace) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task OnConnectionAsync(Microsoft.AspNetCore.Connections.ConnectionContext connection) { throw null; }
    }
    public partial class ConnectionLogScope : System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.Generic.IReadOnlyList<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.IEnumerable
    {
        public ConnectionLogScope(string connectionId) { }
        public int Count { get { throw null; } }
        public System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object>> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class HttpConnection : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ITimeoutHandler
    {
        public HttpConnection(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.HttpConnectionContext context) { }
        public string ConnectionId { get { throw null; } }
        public System.Net.IPEndPoint LocalEndPoint { get { throw null; } }
        public System.Net.IPEndPoint RemoteEndPoint { get { throw null; } }
        public void OnTimeout(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.TimeoutReason reason) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task ProcessRequestsAsync<TContext>(Microsoft.AspNetCore.Hosting.Server.IHttpApplication<TContext> httpApplication) { throw null; }
    }
    public static partial class HttpConnectionBuilderExtensions
    {
        public static Microsoft.AspNetCore.Connections.IConnectionBuilder UseHttpServer<TContext>(this Microsoft.AspNetCore.Connections.IConnectionBuilder builder, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.ServiceContext serviceContext, Microsoft.AspNetCore.Hosting.Server.IHttpApplication<TContext> application, Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols protocols) { throw null; }
        public static Microsoft.AspNetCore.Connections.IConnectionBuilder UseHttpServer<TContext>(this Microsoft.AspNetCore.Connections.IConnectionBuilder builder, System.Collections.Generic.IList<Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal.IConnectionAdapter> adapters, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.ServiceContext serviceContext, Microsoft.AspNetCore.Hosting.Server.IHttpApplication<TContext> application, Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols protocols) { throw null; }
    }
    public partial class HttpConnectionContext
    {
        public HttpConnectionContext() { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal.IConnectionAdapter> ConnectionAdapters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
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
    public partial class HttpConnectionMiddleware<TContext>
    {
        public HttpConnectionMiddleware(System.Collections.Generic.IList<Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal.IConnectionAdapter> adapters, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.ServiceContext serviceContext, Microsoft.AspNetCore.Hosting.Server.IHttpApplication<TContext> application, Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols protocols) { }
        public System.Threading.Tasks.Task OnConnectionAsync(Microsoft.AspNetCore.Connections.ConnectionContext connectionContext) { throw null; }
    }
    public partial interface IRequestProcessor
    {
        void Abort(Microsoft.AspNetCore.Connections.ConnectionAbortedException ex);
        void HandleReadDataRateTimeout();
        void HandleRequestHeadersTimeout();
        void OnInputOrOutputCompleted();
        System.Threading.Tasks.Task ProcessRequestsAsync<TContext>(Microsoft.AspNetCore.Hosting.Server.IHttpApplication<TContext> application);
        void StopProcessingNextRequest();
        void Tick(System.DateTimeOffset now);
    }
    public partial class KestrelServerOptionsSetup : Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>
    {
        public KestrelServerOptionsSetup(System.IServiceProvider services) { }
        public void Configure(Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions options) { }
    }
    public partial class KestrelTrace : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace, Microsoft.Extensions.Logging.ILogger
    {
        protected readonly Microsoft.Extensions.Logging.ILogger _logger;
        public KestrelTrace(Microsoft.Extensions.Logging.ILogger logger) { }
        public virtual void ApplicationAbortedConnection(string connectionId, string traceIdentifier) { }
        public virtual void ApplicationError(string connectionId, string traceIdentifier, System.Exception ex) { }
        public virtual void ApplicationNeverCompleted(string connectionId) { }
        public virtual System.IDisposable BeginScope<TState>(TState state) { throw null; }
        public virtual void ConnectionBadRequest(string connectionId, Microsoft.AspNetCore.Server.Kestrel.Core.BadHttpRequestException ex) { }
        public virtual void ConnectionDisconnect(string connectionId) { }
        public virtual void ConnectionHeadResponseBodyWrite(string connectionId, long count) { }
        public virtual void ConnectionKeepAlive(string connectionId) { }
        public virtual void ConnectionPause(string connectionId) { }
        public virtual void ConnectionRejected(string connectionId) { }
        public virtual void ConnectionResume(string connectionId) { }
        public virtual void ConnectionStart(string connectionId) { }
        public virtual void ConnectionStop(string connectionId) { }
        public virtual void HeartbeatSlow(System.TimeSpan interval, System.DateTimeOffset now) { }
        public virtual void HPackDecodingError(string connectionId, int streamId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack.HPackDecodingException ex) { }
        public virtual void HPackEncodingError(string connectionId, int streamId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack.HPackEncodingException ex) { }
        public virtual void Http2ConnectionClosed(string connectionId, int highestOpenedStreamId) { }
        public virtual void Http2ConnectionClosing(string connectionId) { }
        public virtual void Http2ConnectionError(string connectionId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ConnectionErrorException ex) { }
        public void Http2FrameReceived(string connectionId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2Frame frame) { }
        public void Http2FrameSending(string connectionId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2Frame frame) { }
        public virtual void Http2StreamError(string connectionId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2StreamErrorException ex) { }
        public void Http2StreamResetAbort(string traceIdentifier, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode error, Microsoft.AspNetCore.Connections.ConnectionAbortedException abortReason) { }
        public virtual bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) { throw null; }
        public virtual void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, System.Exception exception, System.Func<TState, System.Exception, string> formatter) { }
        public virtual void NotAllConnectionsAborted() { }
        public virtual void NotAllConnectionsClosedGracefully() { }
        public virtual void RequestBodyDone(string connectionId, string traceIdentifier) { }
        public virtual void RequestBodyDrainTimedOut(string connectionId, string traceIdentifier) { }
        public virtual void RequestBodyMinimumDataRateNotSatisfied(string connectionId, string traceIdentifier, double rate) { }
        public virtual void RequestBodyNotEntirelyRead(string connectionId, string traceIdentifier) { }
        public virtual void RequestBodyStart(string connectionId, string traceIdentifier) { }
        public virtual void RequestProcessingError(string connectionId, System.Exception ex) { }
        public virtual void ResponseMinimumDataRateNotSatisfied(string connectionId, string traceIdentifier) { }
    }
    public partial class ServiceContext
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
}
namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    [System.FlagsAttribute]
    public enum ConnectionOptions
    {
        Close = 1,
        KeepAlive = 2,
        None = 0,
        Upgrade = 4,
    }
    public partial class DateHeaderValueManager : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IHeartbeatHandler
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
    public partial class Http1ChunkedEncodingMessageBody : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1MessageBody
    {
        public Http1ChunkedEncodingMessageBody(bool keepAlive, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1Connection context) : base (default(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1Connection)) { }
        public override void AdvanceTo(System.SequencePosition consumed) { }
        public override void AdvanceTo(System.SequencePosition consumed, System.SequencePosition examined) { }
        public override void CancelPendingRead() { }
        public override void Complete(System.Exception exception) { }
        protected void Copy(System.Buffers.ReadOnlySequence<byte> readableBuffer, System.IO.Pipelines.PipeWriter writableBuffer) { }
        protected override void OnReadStarted() { }
        protected override System.Threading.Tasks.Task OnStopAsync() { throw null; }
        public override void OnWriterCompleted(System.Action<System.Exception, object> callback, object state) { }
        protected bool Read(System.Buffers.ReadOnlySequence<byte> readableBuffer, System.IO.Pipelines.PipeWriter writableBuffer, out System.SequencePosition consumed, out System.SequencePosition examined) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> ReadAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override bool TryRead(out System.IO.Pipelines.ReadResult readResult) { throw null; }
    }
    public partial class Http1Connection : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol, Microsoft.AspNetCore.Server.Kestrel.Core.Features.IHttpMinRequestBodyDataRateFeature, Microsoft.AspNetCore.Server.Kestrel.Core.Features.IHttpMinResponseDataRateFeature, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.IRequestProcessor
    {
        protected readonly long _keepAliveTicks;
        public Http1Connection(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.HttpConnectionContext context) : base (default(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.HttpConnectionContext)) { }
        public System.IO.Pipelines.PipeReader Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Buffers.MemoryPool<byte> MemoryPool { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate Microsoft.AspNetCore.Server.Kestrel.Core.Features.IHttpMinRequestBodyDataRateFeature.MinDataRate { get { throw null; } set { } }
        Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate Microsoft.AspNetCore.Server.Kestrel.Core.Features.IHttpMinResponseDataRateFeature.MinDataRate { get { throw null; } set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate MinRequestBodyDataRate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate MinResponseDataRate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool RequestTimedOut { get { throw null; } }
        public void Abort(Microsoft.AspNetCore.Connections.ConnectionAbortedException abortReason) { }
        protected override void ApplicationAbort() { }
        protected override bool BeginRead(out System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> awaitable) { throw null; }
        protected override void BeginRequestProcessing() { }
        protected override Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.MessageBody CreateMessageBody() { throw null; }
        protected override string CreateRequestId() { throw null; }
        public void HandleReadDataRateTimeout() { }
        public void HandleRequestHeadersTimeout() { }
        void Microsoft.AspNetCore.Server.Kestrel.Core.Internal.IRequestProcessor.Tick(System.DateTimeOffset now) { }
        public void OnInputOrOutputCompleted() { }
        protected override void OnRequestProcessingEnded() { }
        protected override void OnRequestProcessingEnding() { }
        protected override void OnReset() { }
        public void OnStartLine(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod method, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpVersion version, System.Span<byte> target, System.Span<byte> path, System.Span<byte> query, System.Span<byte> customMethod, bool pathEncoded) { }
        public void ParseRequest(System.Buffers.ReadOnlySequence<byte> buffer, out System.SequencePosition consumed, out System.SequencePosition examined) { throw null; }
        public void SendTimeoutResponse() { }
        public void StopProcessingNextRequest() { }
        public bool TakeMessageHeaders(System.Buffers.ReadOnlySequence<byte> buffer, out System.SequencePosition consumed, out System.SequencePosition examined) { throw null; }
        public bool TakeStartLine(System.Buffers.ReadOnlySequence<byte> buffer, out System.SequencePosition consumed, out System.SequencePosition examined) { throw null; }
        protected override bool TryParseRequest(System.IO.Pipelines.ReadResult result, out bool endConnection) { throw null; }
    }
    public partial class Http1ContentLengthMessageBody : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1MessageBody
    {
        public Http1ContentLengthMessageBody(bool keepAlive, long contentLength, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1Connection context) : base (default(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1Connection)) { }
        public override void AdvanceTo(System.SequencePosition consumed) { }
        public override void AdvanceTo(System.SequencePosition consumed, System.SequencePosition examined) { }
        public override void CancelPendingRead() { }
        public override void Complete(System.Exception exception) { }
        protected override void OnReadStarting() { }
        protected override System.Threading.Tasks.Task OnStopAsync() { throw null; }
        public override void OnWriterCompleted(System.Action<System.Exception, object> callback, object state) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> ReadAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override bool TryRead(out System.IO.Pipelines.ReadResult readResult) { throw null; }
    }
    public abstract partial class Http1MessageBody : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.MessageBody
    {
        protected readonly Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1Connection _context;
        protected Http1MessageBody(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1Connection context) : base (default(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol), default(Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate)) { }
        protected void CheckCompletedReadResult(System.IO.Pipelines.ReadResult result) { }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.MessageBody For(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpVersion httpVersion, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpRequestHeaders headers, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1Connection context) { throw null; }
        protected override System.Threading.Tasks.Task OnConsumeAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected System.Threading.Tasks.Task OnConsumeAsyncAwaited() { throw null; }
    }
    public partial class Http1OutputProducer : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpOutputAborter, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpOutputProducer, System.IDisposable
    {
        public Http1OutputProducer(System.IO.Pipelines.PipeWriter pipeWriter, string connectionId, Microsoft.AspNetCore.Connections.ConnectionContext connectionContext, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace log, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ITimeoutControl timeoutControl, Microsoft.AspNetCore.Server.Kestrel.Core.Features.IHttpMinResponseDataRateFeature minResponseDataRateFeature, System.Buffers.MemoryPool<byte> memoryPool) { }
        public void Abort(Microsoft.AspNetCore.Connections.ConnectionAbortedException error) { }
        public void Advance(int bytes) { }
        public void CancelPendingFlush() { }
        public void Complete() { }
        public void Dispose() { }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FirstWriteAsync(int statusCode, string reasonPhrase, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders responseHeaders, bool autoChunk, System.ReadOnlySpan<byte> buffer, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FirstWriteChunkedAsync(int statusCode, string reasonPhrase, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders responseHeaders, bool autoChunk, System.ReadOnlySpan<byte> buffer, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FlushAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Memory<byte> GetMemory(int sizeHint = 0) { throw null; }
        public System.Span<byte> GetSpan(int sizeHint = 0) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> Write100ContinueAsync() { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteChunkAsync(System.ReadOnlySpan<byte> buffer, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task WriteDataAsync(System.ReadOnlySpan<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteDataToPipeAsync(System.ReadOnlySpan<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public void WriteResponseHeaders(int statusCode, string reasonPhrase, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders responseHeaders, bool autoChunk) { }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteStreamSuffixAsync() { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct Http1ParsingHandler : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpHeadersHandler, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpRequestLineHandler
    {
        public readonly Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1Connection Connection;
        public Http1ParsingHandler(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1Connection connection) { throw null; }
        public void OnHeader(System.Span<byte> name, System.Span<byte> value) { }
        public void OnStartLine(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod method, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpVersion version, System.Span<byte> target, System.Span<byte> path, System.Span<byte> query, System.Span<byte> customMethod, bool pathEncoded) { }
    }
    public partial class Http1UpgradeMessageBody : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1MessageBody
    {
        public bool _completed;
        public Http1UpgradeMessageBody(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1Connection context) : base (default(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1Connection)) { }
        public override bool IsEmpty { get { throw null; } }
        public override void AdvanceTo(System.SequencePosition consumed) { }
        public override void AdvanceTo(System.SequencePosition consumed, System.SequencePosition examined) { }
        public override void CancelPendingRead() { }
        public override void Complete(System.Exception exception) { }
        public override System.Threading.Tasks.Task ConsumeAsync() { throw null; }
        public override void OnWriterCompleted(System.Action<System.Exception, object> callback, object state) { }
        public override System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> ReadAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.Task StopAsync() { throw null; }
        public override bool TryRead(out System.IO.Pipelines.ReadResult result) { throw null; }
    }
    public abstract partial class HttpHeaders : Microsoft.AspNetCore.Http.IHeaderDictionary, System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.Generic.IDictionary<string, Microsoft.Extensions.Primitives.StringValues>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.IEnumerable
    {
        protected System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues> MaybeUnknown;
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
        protected virtual bool AddValueFast(string key, in Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]protected static Microsoft.Extensions.Primitives.StringValues AppendValue(in Microsoft.Extensions.Primitives.StringValues existing, string append) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]protected static int BitCount(long value) { throw null; }
        protected virtual void ClearFast() { }
        protected virtual bool CopyToFast(System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>[] array, int arrayIndex) { throw null; }
        protected virtual int GetCountFast() { throw null; }
        protected virtual System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> GetEnumeratorFast() { throw null; }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.TransferCoding GetFinalTransferCoding(in Microsoft.Extensions.Primitives.StringValues transferEncoding) { throw null; }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.ConnectionOptions ParseConnection(in Microsoft.Extensions.Primitives.StringValues connection) { throw null; }
        protected virtual bool RemoveFast(string key) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public void Reset() { }
        public void SetReadOnly() { }
        protected virtual void SetValueFast(string key, in Microsoft.Extensions.Primitives.StringValues value) { }
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
        protected virtual bool TryGetValueFast(string key, out Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        public static void ValidateHeaderNameCharacters(string headerCharacters) { }
        public static void ValidateHeaderValueCharacters(in Microsoft.Extensions.Primitives.StringValues headerValues) { }
        public static void ValidateHeaderValueCharacters(string headerCharacters) { }
    }
    public enum HttpMethod : byte
    {
        Connect = (byte)7,
        Custom = (byte)9,
        Delete = (byte)2,
        Get = (byte)0,
        Head = (byte)4,
        None = (byte)255,
        Options = (byte)8,
        Patch = (byte)6,
        Post = (byte)3,
        Put = (byte)1,
        Trace = (byte)5,
    }
    public partial class HttpParser<TRequestHandler> : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpParser<TRequestHandler> where TRequestHandler : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpHeadersHandler, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpRequestLineHandler
    {
        public HttpParser() { }
        public HttpParser(bool showErrorDetails) { }
        bool Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpParser<TRequestHandler>.ParseHeaders(TRequestHandler handler, in System.Buffers.ReadOnlySequence<byte> buffer, out System.SequencePosition consumed, out System.SequencePosition examined, out int consumedBytes) { throw null; }
        bool Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpParser<TRequestHandler>.ParseRequestLine(TRequestHandler handler, in System.Buffers.ReadOnlySequence<byte> buffer, out System.SequencePosition consumed, out System.SequencePosition examined) { throw null; }
        public bool ParseHeaders(TRequestHandler handler, in System.Buffers.ReadOnlySequence<byte> buffer, out System.SequencePosition consumed, out System.SequencePosition examined, out int consumedBytes) { throw null; }
        public bool ParseRequestLine(TRequestHandler handler, in System.Buffers.ReadOnlySequence<byte> buffer, out System.SequencePosition consumed, out System.SequencePosition examined) { throw null; }
    }
    public abstract partial class HttpProtocol : Microsoft.AspNetCore.Http.Features.IFeatureCollection, Microsoft.AspNetCore.Http.Features.IHttpBodyControlFeature, Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature, Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature, Microsoft.AspNetCore.Http.Features.IHttpRequestFeature, Microsoft.AspNetCore.Http.Features.IHttpRequestIdentifierFeature, Microsoft.AspNetCore.Http.Features.IHttpRequestLifetimeFeature, Microsoft.AspNetCore.Http.Features.IHttpResponseFeature, Microsoft.AspNetCore.Http.Features.IHttpResponseStartFeature, Microsoft.AspNetCore.Http.Features.IHttpUpgradeFeature, Microsoft.AspNetCore.Http.Features.IRequestBodyPipeFeature, Microsoft.AspNetCore.Http.Features.IResponseBodyPipeFeature, Microsoft.AspNetCore.Http.IDefaultHttpContextContainer, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpResponseControl, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Type, object>>, System.Collections.IEnumerable
    {
        protected Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.BodyControl bodyControl;
        protected System.Exception _applicationException;
        protected Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpVersion _httpVersion;
        protected volatile bool _keepAlive;
        protected string _methodText;
        protected Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.RequestProcessingStatus _requestProcessingStatus;
        public HttpProtocol(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.HttpConnectionContext context) { }
        public bool AllowSynchronousIO { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.Features.IFeatureCollection ConnectionFeatures { get { throw null; } }
        protected string ConnectionId { get { throw null; } }
        public string ConnectionIdFeature { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool HasFlushedHeaders { get { throw null; } }
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
        System.IO.Stream Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.Body { get { throw null; } set { } }
        bool Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.HasStarted { get { throw null; } }
        Microsoft.AspNetCore.Http.IHeaderDictionary Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.Headers { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.ReasonPhrase { get { throw null; } set { } }
        int Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.StatusCode { get { throw null; } set { } }
        bool Microsoft.AspNetCore.Http.Features.IHttpUpgradeFeature.IsUpgradableRequest { get { throw null; } }
        System.IO.Pipelines.PipeReader Microsoft.AspNetCore.Http.Features.IRequestBodyPipeFeature.RequestBodyPipe { get { throw null; } set { } }
        System.IO.Pipelines.PipeWriter Microsoft.AspNetCore.Http.Features.IResponseBodyPipeFeature.ResponseBodyPipe { get { throw null; } set { } }
        Microsoft.AspNetCore.Http.DefaultHttpContext Microsoft.AspNetCore.Http.IDefaultHttpContextContainer.HttpContext { get { throw null; } }
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
        public System.IO.Stream ResponseBody { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.IHeaderDictionary ResponseHeaders { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.IO.Pipelines.PipeWriter ResponsePipeWriter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
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
        public void Complete(System.Exception ex) { }
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
        void Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.OnCompleted(System.Func<object, System.Threading.Tasks.Task> callback, object state) { }
        void Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.OnStarting(System.Func<object, System.Threading.Tasks.Task> callback, object state) { }
        System.Threading.Tasks.Task Microsoft.AspNetCore.Http.Features.IHttpResponseStartFeature.StartAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        System.Threading.Tasks.Task<System.IO.Stream> Microsoft.AspNetCore.Http.Features.IHttpUpgradeFeature.UpgradeAsync() { throw null; }
        public void OnCompleted(System.Func<object, System.Threading.Tasks.Task> callback, object state) { }
        protected virtual void OnErrorAfterResponseStarted() { }
        public void OnHeader(System.Span<byte> name, System.Span<byte> value) { }
        protected virtual void OnRequestProcessingEnded() { }
        protected virtual void OnRequestProcessingEnding() { }
        protected abstract void OnReset();
        public void OnStarting(System.Func<object, System.Threading.Tasks.Task> callback, object state) { }
        protected void PoisonRequestBodyStream(System.Exception abortReason) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task ProcessRequestsAsync<TContext>(Microsoft.AspNetCore.Hosting.Server.IHttpApplication<TContext> application) { throw null; }
        public void ProduceContinue() { }
        protected System.Threading.Tasks.Task ProduceEnd() { throw null; }
        public void ReportApplicationError(System.Exception ex) { }
        public void Reset() { }
        protected void ResetHttp1Features() { }
        protected void ResetHttp2Features() { }
        public void SetBadRequestState(Microsoft.AspNetCore.Server.Kestrel.Core.BadHttpRequestException ex) { }
        public void StopBodies() { }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<System.Type, object>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Type,System.Object>>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public void ThrowRequestTargetRejected(System.Span<byte> target) { }
        protected abstract bool TryParseRequest(System.IO.Pipelines.ReadResult result, out bool endConnection);
        protected System.Threading.Tasks.Task TryProduceInvalidRequestResponse() { throw null; }
        protected void VerifyResponseContentLength() { }
        public System.Threading.Tasks.Task WriteAsync(System.ReadOnlyMemory<byte> data, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteAsyncAwaited(System.Threading.Tasks.Task initializeTask, System.ReadOnlyMemory<byte> data, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WritePipeAsync(System.ReadOnlyMemory<byte> data, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public sealed partial class HttpRequestHeaders : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpHeaders
    {
        public HttpRequestHeaders() { }
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
        public Microsoft.Extensions.Primitives.StringValues HeaderDate { get { throw null; } set { } }
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
        public Microsoft.Extensions.Primitives.StringValues HeaderTE { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderTrailer { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderTransferEncoding { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderTranslate { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderUpgrade { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderUserAgent { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderVia { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderWarning { get { throw null; } set { } }
        public int HostCount { get { throw null; } }
        protected override bool AddValueFast(string key, in Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        public unsafe void Append(byte* pKeyBytes, int keyLength, string value) { }
        public void Append(System.Span<byte> name, string value) { }
        protected override void ClearFast() { }
        protected override bool CopyToFast(System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>[] array, int arrayIndex) { throw null; }
        protected override int GetCountFast() { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpRequestHeaders.Enumerator GetEnumerator() { throw null; }
        protected override System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> GetEnumeratorFast() { throw null; }
        protected override bool RemoveFast(string key) { throw null; }
        protected override void SetValueFast(string key, in Microsoft.Extensions.Primitives.StringValues value) { }
        protected override bool TryGetValueFast(string key, out Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            public void Reset() { }
        }
    }
    public partial class HttpRequestPipeReader : System.IO.Pipelines.PipeReader
    {
        public HttpRequestPipeReader() { }
        public void Abort(System.Exception error = null) { }
        public override void AdvanceTo(System.SequencePosition consumed) { }
        public override void AdvanceTo(System.SequencePosition consumed, System.SequencePosition examined) { }
        public override void CancelPendingRead() { }
        public override void Complete(System.Exception exception = null) { }
        public override void OnWriterCompleted(System.Action<System.Exception, object> callback, object state) { }
        public override System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> ReadAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public void StartAcceptingReads(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.MessageBody body) { }
        public void StopAcceptingReads() { }
        public override bool TryRead(out System.IO.Pipelines.ReadResult result) { throw null; }
    }
    public enum HttpRequestTarget
    {
        AbsoluteForm = 1,
        AsteriskForm = 3,
        AuthorityForm = 2,
        OriginForm = 0,
        Unknown = -1,
    }
    public sealed partial class HttpResponseHeaders : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpHeaders
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
        protected override bool AddValueFast(string key, in Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        protected override void ClearFast() { }
        protected override bool CopyToFast(System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>[] array, int arrayIndex) { throw null; }
        protected override int GetCountFast() { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders.Enumerator GetEnumerator() { throw null; }
        protected override System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> GetEnumeratorFast() { throw null; }
        protected override bool RemoveFast(string key) { throw null; }
        public void SetRawConnection(in Microsoft.Extensions.Primitives.StringValues value, byte[] raw) { }
        public void SetRawDate(in Microsoft.Extensions.Primitives.StringValues value, byte[] raw) { }
        public void SetRawServer(in Microsoft.Extensions.Primitives.StringValues value, byte[] raw) { }
        public void SetRawTransferEncoding(in Microsoft.Extensions.Primitives.StringValues value, byte[] raw) { }
        protected override void SetValueFast(string key, in Microsoft.Extensions.Primitives.StringValues value) { }
        protected override bool TryGetValueFast(string key, out Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            public void Reset() { }
        }
    }
    public partial class HttpResponsePipeWriter : System.IO.Pipelines.PipeWriter
    {
        public HttpResponsePipeWriter(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpResponseControl pipeControl) { }
        public void Abort() { }
        public override void Advance(int bytes) { }
        public override void CancelPendingFlush() { }
        public override void Complete(System.Exception exception = null) { }
        public override System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FlushAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Memory<byte> GetMemory(int sizeHint = 0) { throw null; }
        public override System.Span<byte> GetSpan(int sizeHint = 0) { throw null; }
        public override void OnReaderCompleted(System.Action<System.Exception, object> callback, object state) { }
        public void StartAcceptingWrites() { }
        public void StopAcceptingWrites() { }
        public override System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteAsync(System.ReadOnlyMemory<byte> source, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public partial class HttpResponseTrailers : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpHeaders
    {
        public HttpResponseTrailers() { }
        public Microsoft.Extensions.Primitives.StringValues HeaderETag { get { throw null; } set { } }
        protected override bool AddValueFast(string key, in Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        protected override void ClearFast() { }
        protected override bool CopyToFast(System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>[] array, int arrayIndex) { throw null; }
        protected override int GetCountFast() { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseTrailers.Enumerator GetEnumerator() { throw null; }
        protected override System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> GetEnumeratorFast() { throw null; }
        protected override bool RemoveFast(string key) { throw null; }
        protected override void SetValueFast(string key, in Microsoft.Extensions.Primitives.StringValues value) { }
        protected override bool TryGetValueFast(string key, out Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            public void Reset() { }
        }
    }
    public enum HttpScheme
    {
        Http = 0,
        Https = 1,
        Unknown = -1,
    }
    public enum HttpVersion
    {
        Http10 = 0,
        Http11 = 1,
        Http2 = 2,
        Unknown = -1,
    }
    public partial interface IHttpHeadersHandler
    {
        void OnHeader(System.Span<byte> name, System.Span<byte> value);
    }
    public partial interface IHttpOutputAborter
    {
        void Abort(Microsoft.AspNetCore.Connections.ConnectionAbortedException abortReason);
    }
    public partial interface IHttpOutputProducer
    {
        void Advance(int bytes);
        void CancelPendingFlush();
        void Complete();
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FirstWriteAsync(int statusCode, string reasonPhrase, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders responseHeaders, bool autoChunk, System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FirstWriteChunkedAsync(int statusCode, string reasonPhrase, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders responseHeaders, bool autoChunk, System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FlushAsync(System.Threading.CancellationToken cancellationToken);
        System.Memory<byte> GetMemory(int sizeHint = 0);
        System.Span<byte> GetSpan(int sizeHint = 0);
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> Write100ContinueAsync();
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteChunkAsync(System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task WriteDataAsync(System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteDataToPipeAsync(System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken);
        void WriteResponseHeaders(int statusCode, string reasonPhrase, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders responseHeaders, bool autoChunk);
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteStreamSuffixAsync();
    }
    public partial interface IHttpParser<TRequestHandler> where TRequestHandler : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpHeadersHandler, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpRequestLineHandler
    {
        bool ParseHeaders(TRequestHandler handler, in System.Buffers.ReadOnlySequence<byte> buffer, out System.SequencePosition consumed, out System.SequencePosition examined, out int consumedBytes);
        bool ParseRequestLine(TRequestHandler handler, in System.Buffers.ReadOnlySequence<byte> buffer, out System.SequencePosition consumed, out System.SequencePosition examined);
    }
    public partial interface IHttpRequestLineHandler
    {
        void OnStartLine(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod method, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpVersion version, System.Span<byte> target, System.Span<byte> path, System.Span<byte> query, System.Span<byte> customMethod, bool pathEncoded);
    }
    public partial interface IHttpResponseControl
    {
        void Advance(int bytes);
        void CancelPendingFlush();
        void Complete(System.Exception exception = null);
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FlushPipeAsync(System.Threading.CancellationToken cancellationToken);
        System.Memory<byte> GetMemory(int sizeHint = 0);
        System.Span<byte> GetSpan(int sizeHint = 0);
        void ProduceContinue();
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WritePipeAsync(System.ReadOnlyMemory<byte> source, System.Threading.CancellationToken cancellationToken);
    }
    public partial interface IHttpResponsePipeWriterControl
    {
        void Advance(int bytes);
        void CancelPendingFlush();
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FlushPipeAsync(System.Threading.CancellationToken cancellationToken);
        System.Memory<byte> GetMemory(int sizeHint = 0);
        System.Span<byte> GetSpan(int sizeHint = 0);
        void ProduceContinue();
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WritePipeAsync(System.ReadOnlyMemory<byte> source, System.Threading.CancellationToken cancellationToken);
    }
    public abstract partial class MessageBody
    {
        protected long _alreadyTimedBytes;
        protected bool _backpressure;
        protected bool _timingEnabled;
        protected MessageBody(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol context, Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate minRequestBodyDataRate) { }
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
        protected virtual System.Threading.Tasks.Task OnConsumeAsync() { throw null; }
        protected virtual void OnDataRead(long bytesRead) { }
        protected virtual void OnReadStarted() { }
        protected virtual void OnReadStarting() { }
        protected virtual System.Threading.Tasks.Task OnStopAsync() { throw null; }
        public abstract void OnWriterCompleted(System.Action<System.Exception, object> callback, object state);
        public abstract System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> ReadAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        protected System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> StartTimingReadAsync(System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> readAwaitable, System.Threading.CancellationToken cancellationToken) { throw null; }
        public virtual System.Threading.Tasks.Task StopAsync() { throw null; }
        protected void StopTimingRead(long bytesRead) { }
        protected void TryProduceContinue() { }
        public abstract bool TryRead(out System.IO.Pipelines.ReadResult readResult);
        protected void TryStart() { }
        protected void TryStop() { }
    }
    public static partial class PathNormalizer
    {
        public unsafe static bool ContainsDotSegments(byte* start, byte* end) { throw null; }
        public static string DecodePath(System.Span<byte> path, bool pathEncoded, string rawTarget, int queryLength) { throw null; }
        public unsafe static int RemoveDotSegments(byte* start, byte* end) { throw null; }
        public static int RemoveDotSegments(System.Span<byte> input) { throw null; }
    }
    public static partial class PipelineExtensions
    {
        public static System.ArraySegment<byte> GetArray(this System.Memory<byte> buffer) { throw null; }
        public static System.ArraySegment<byte> GetArray(this System.ReadOnlyMemory<byte> memory) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static System.ReadOnlySpan<byte> ToSpan(this System.Buffers.ReadOnlySequence<byte> buffer) { throw null; }
    }
    public enum ProduceEndType
    {
        ConnectionKeepAlive = 2,
        SocketDisconnect = 1,
        SocketShutdown = 0,
    }
    public static partial class ReasonPhrases
    {
        public static byte[] ToStatusBytes(int statusCode, string reasonPhrase = null) { throw null; }
    }
    public enum RequestProcessingStatus
    {
        AppStarted = 3,
        HeadersCommitted = 4,
        HeadersFlushed = 5,
        ParsingHeaders = 2,
        ParsingRequestLine = 1,
        RequestPending = 0,
    }
    public enum RequestRejectionReason
    {
        BadChunkSizeData = 9,
        BadChunkSuffix = 8,
        ChunkedRequestIncomplete = 10,
        ConnectMethodRequired = 23,
        FinalTransferCodingNotChunked = 19,
        HeadersExceedMaxTotalSize = 14,
        InvalidCharactersInHeaderName = 12,
        InvalidContentLength = 5,
        InvalidHostHeader = 26,
        InvalidRequestHeader = 2,
        InvalidRequestHeadersNoCRLF = 3,
        InvalidRequestLine = 1,
        InvalidRequestTarget = 11,
        LengthRequired = 20,
        LengthRequiredHttp10 = 21,
        MalformedRequestInvalidHeaders = 4,
        MissingHostHeader = 24,
        MultipleContentLengths = 6,
        MultipleHostHeaders = 25,
        OptionsMethodRequired = 22,
        RequestBodyExceedsContentLength = 28,
        RequestBodyTimeout = 18,
        RequestBodyTooLarge = 16,
        RequestHeadersTimeout = 17,
        RequestLineTooLong = 13,
        TooManyHeaders = 15,
        UnexpectedEndOfRequestContent = 7,
        UnrecognizedHTTPVersion = 0,
        UpgradeRequestCannotHavePayload = 27,
    }
    [System.FlagsAttribute]
    public enum TransferCoding
    {
        Chunked = 1,
        None = 0,
        Other = 2,
    }
    public partial class ZeroContentLengthMessageBody : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.MessageBody
    {
        public ZeroContentLengthMessageBody(bool keepAlive) : base (default(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol), default(Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate)) { }
        public override bool IsEmpty { get { throw null; } }
        public override void AdvanceTo(System.SequencePosition consumed) { }
        public override void AdvanceTo(System.SequencePosition consumed, System.SequencePosition examined) { }
        public override void CancelPendingRead() { }
        public override void Complete(System.Exception ex) { }
        public override System.Threading.Tasks.Task ConsumeAsync() { throw null; }
        public override void OnWriterCompleted(System.Action<System.Exception, object> callback, object state) { }
        public override System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> ReadAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.Task StopAsync() { throw null; }
        public override bool TryRead(out System.IO.Pipelines.ReadResult result) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public partial class Http2Connection : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpHeadersHandler, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.IHttp2StreamLifetimeHandler, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.IRequestProcessor
    {
        public Http2Connection(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.HttpConnectionContext context) { }
        public static byte[] ClientPreface { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Http.Features.IFeatureCollection ConnectionFeatures { get { throw null; } }
        public string ConnectionId { get { throw null; } }
        public System.IO.Pipelines.PipeReader Input { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerLimits Limits { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace Log { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ISystemClock SystemClock { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ITimeoutControl TimeoutControl { get { throw null; } }
        public void Abort(Microsoft.AspNetCore.Connections.ConnectionAbortedException ex) { }
        public void HandleReadDataRateTimeout() { }
        public void HandleRequestHeadersTimeout() { }
        void Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.IHttp2StreamLifetimeHandler.OnStreamCompleted(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2Stream stream) { }
        void Microsoft.AspNetCore.Server.Kestrel.Core.Internal.IRequestProcessor.Tick(System.DateTimeOffset now) { }
        public void OnHeader(System.Span<byte> name, System.Span<byte> value) { }
        public void OnInputOrOutputCompleted() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task ProcessRequestsAsync<TContext>(Microsoft.AspNetCore.Hosting.Server.IHttpApplication<TContext> application) { throw null; }
        public void StopProcessingNextRequest() { }
        public void StopProcessingNextRequest(bool serverInitiated) { }
    }
    public partial class Http2ConnectionErrorException : System.Exception
    {
        public Http2ConnectionErrorException(string message, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode errorCode) { }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode ErrorCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    [System.FlagsAttribute]
    public enum Http2ContinuationFrameFlags : byte
    {
        END_HEADERS = (byte)4,
        NONE = (byte)0,
    }
    [System.FlagsAttribute]
    public enum Http2DataFrameFlags : byte
    {
        END_STREAM = (byte)1,
        NONE = (byte)0,
        PADDED = (byte)8,
    }
    public enum Http2ErrorCode : uint
    {
        CANCEL = (uint)8,
        COMPRESSION_ERROR = (uint)9,
        CONNECT_ERROR = (uint)10,
        ENHANCE_YOUR_CALM = (uint)11,
        FLOW_CONTROL_ERROR = (uint)3,
        FRAME_SIZE_ERROR = (uint)6,
        HTTP_1_1_REQUIRED = (uint)13,
        INADEQUATE_SECURITY = (uint)12,
        INTERNAL_ERROR = (uint)2,
        NO_ERROR = (uint)0,
        PROTOCOL_ERROR = (uint)1,
        REFUSED_STREAM = (uint)7,
        SETTINGS_TIMEOUT = (uint)4,
        STREAM_CLOSED = (uint)5,
    }
    public partial class Http2Frame
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
        public override string ToString() { throw null; }
    }
    public static partial class Http2FrameReader
    {
        public const int HeaderLength = 9;
        public const int SettingSize = 6;
        public static int GetPayloadFieldsLength(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2Frame frame) { throw null; }
        public static bool ReadFrame(System.Buffers.ReadOnlySequence<byte> readableBuffer, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2Frame frame, uint maxFrameSize, out System.Buffers.ReadOnlySequence<byte> framePayload) { throw null; }
        public static System.Collections.Generic.IList<Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2PeerSetting> ReadSettings(System.Buffers.ReadOnlySequence<byte> payload) { throw null; }
    }
    public enum Http2FrameType : byte
    {
        CONTINUATION = (byte)9,
        DATA = (byte)0,
        GOAWAY = (byte)7,
        HEADERS = (byte)1,
        PING = (byte)6,
        PRIORITY = (byte)2,
        PUSH_PROMISE = (byte)5,
        RST_STREAM = (byte)3,
        SETTINGS = (byte)4,
        WINDOW_UPDATE = (byte)8,
    }
    public partial class Http2FrameWriter
    {
        public Http2FrameWriter(System.IO.Pipelines.PipeWriter outputPipeWriter, Microsoft.AspNetCore.Connections.ConnectionContext connectionContext, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2Connection http2Connection, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl.OutputFlowControl connectionOutputFlowControl, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ITimeoutControl timeoutControl, Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate minResponseDataRate, string connectionId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace log) { }
        public void Abort(Microsoft.AspNetCore.Connections.ConnectionAbortedException error) { }
        public void AbortPendingStreamDataWrites(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl.StreamOutputFlowControl flowControl) { }
        public void Complete() { }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FlushAsync(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpOutputAborter outputAborter, System.Threading.CancellationToken cancellationToken) { throw null; }
        public bool TryUpdateConnectionWindow(int bytes) { throw null; }
        public bool TryUpdateStreamWindow(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl.StreamOutputFlowControl flowControl, int bytes) { throw null; }
        public void UpdateMaxFrameSize(uint maxFrameSize) { }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> Write100ContinueAsync(int streamId) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteDataAsync(int streamId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl.StreamOutputFlowControl flowControl, System.Buffers.ReadOnlySequence<byte> data, bool endStream) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteGoAwayAsync(int lastStreamId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode errorCode) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WritePingAsync(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2PingFrameFlags flags, System.Buffers.ReadOnlySequence<byte> payload) { throw null; }
        public void WriteResponseHeaders(int streamId, int statusCode, Microsoft.AspNetCore.Http.IHeaderDictionary headers) { }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteResponseTrailers(int streamId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseTrailers headers) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteRstStreamAsync(int streamId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode errorCode) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteSettingsAckAsync() { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteSettingsAsync(System.Collections.Generic.IList<Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2PeerSetting> settings) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteWindowUpdateAsync(int streamId, int sizeIncrement) { throw null; }
    }
    [System.FlagsAttribute]
    public enum Http2HeadersFrameFlags : byte
    {
        END_HEADERS = (byte)4,
        END_STREAM = (byte)1,
        NONE = (byte)0,
        PADDED = (byte)8,
        PRIORITY = (byte)32,
    }
    public partial class Http2MessageBody : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.MessageBody
    {
        internal Http2MessageBody() : base (default(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol), default(Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate)) { }
        public override void AdvanceTo(System.SequencePosition consumed) { }
        public override void AdvanceTo(System.SequencePosition consumed, System.SequencePosition examined) { }
        public override void CancelPendingRead() { }
        public override void Complete(System.Exception exception) { }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.MessageBody For(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2Stream context, Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate minRequestBodyDataRate) { throw null; }
        protected override void OnDataRead(long bytesRead) { }
        protected override void OnReadStarted() { }
        protected override void OnReadStarting() { }
        protected override System.Threading.Tasks.Task OnStopAsync() { throw null; }
        public override void OnWriterCompleted(System.Action<System.Exception, object> callback, object state) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> ReadAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override bool TryRead(out System.IO.Pipelines.ReadResult readResult) { throw null; }
    }
    public partial class Http2OutputProducer : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpOutputAborter, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpOutputProducer
    {
        public Http2OutputProducer(int streamId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2FrameWriter frameWriter, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl.StreamOutputFlowControl flowControl, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ITimeoutControl timeoutControl, System.Buffers.MemoryPool<byte> pool, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2Stream stream, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace log) { }
        public void Advance(int bytes) { }
        public void CancelPendingFlush() { }
        public void Complete() { }
        public void Dispose() { }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FirstWriteAsync(int statusCode, string reasonPhrase, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders responseHeaders, bool autoChunk, System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FirstWriteChunkedAsync(int statusCode, string reasonPhrase, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders responseHeaders, bool autoChunk, System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Memory<byte> GetMemory(int sizeHint = 0) { throw null; }
        public System.Span<byte> GetSpan(int sizeHint = 0) { throw null; }
        void Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpOutputAborter.Abort(Microsoft.AspNetCore.Connections.ConnectionAbortedException abortReason) { }
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpOutputProducer.WriteChunkAsync(System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> Write100ContinueAsync() { throw null; }
        public System.Threading.Tasks.Task WriteChunkAsync(System.ReadOnlySpan<byte> span, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task WriteDataAsync(System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteDataToPipeAsync(System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken) { throw null; }
        public void WriteResponseHeaders(int statusCode, string ReasonPhrase, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders responseHeaders, bool autoChunk) { }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteRstStreamAsync(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode error) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteStreamSuffixAsync() { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct Http2PeerSetting
    {
        private readonly int _dummyPrimitive;
        public Http2PeerSetting(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2SettingsParameter parameter, uint value) { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2SettingsParameter Parameter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public uint Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class Http2PeerSettings
    {
        public const bool DefaultEnablePush = true;
        public const uint DefaultHeaderTableSize = (uint)4096;
        public const uint DefaultInitialWindowSize = (uint)65535;
        public const uint DefaultMaxConcurrentStreams = (uint)4294967295;
        public const uint DefaultMaxFrameSize = (uint)16384;
        public const uint DefaultMaxHeaderListSize = (uint)4294967295;
        public const uint MaxWindowSize = (uint)2147483647;
        public Http2PeerSettings() { }
        public bool EnablePush { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public uint HeaderTableSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public uint InitialWindowSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public uint MaxConcurrentStreams { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public uint MaxFrameSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public uint MaxHeaderListSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public void Update(System.Collections.Generic.IList<Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2PeerSetting> settings) { }
    }
    [System.FlagsAttribute]
    public enum Http2PingFrameFlags : byte
    {
        ACK = (byte)1,
        NONE = (byte)0,
    }
    [System.FlagsAttribute]
    public enum Http2SettingsFrameFlags : byte
    {
        ACK = (byte)1,
        NONE = (byte)0,
    }
    public enum Http2SettingsParameter : ushort
    {
        SETTINGS_ENABLE_PUSH = (ushort)2,
        SETTINGS_HEADER_TABLE_SIZE = (ushort)1,
        SETTINGS_INITIAL_WINDOW_SIZE = (ushort)4,
        SETTINGS_MAX_CONCURRENT_STREAMS = (ushort)3,
        SETTINGS_MAX_FRAME_SIZE = (ushort)5,
        SETTINGS_MAX_HEADER_LIST_SIZE = (ushort)6,
    }
    public partial class Http2SettingsParameterOutOfRangeException : System.Exception
    {
        public Http2SettingsParameterOutOfRangeException(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2SettingsParameter parameter, long lowerBound, long upperBound) { }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2SettingsParameter Parameter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public abstract partial class Http2Stream : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol, Microsoft.AspNetCore.Http.Features.IHttpResponseTrailersFeature, Microsoft.AspNetCore.Server.Kestrel.Core.Features.IHttp2StreamIdFeature, System.Threading.IThreadPoolWorkItem
    {
        public Http2Stream(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2StreamContext context) : base (default(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.HttpConnectionContext)) { }
        public bool EndStreamReceived { get { throw null; } }
        public long? InputRemaining { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        Microsoft.AspNetCore.Http.IHeaderDictionary Microsoft.AspNetCore.Http.Features.IHttpResponseTrailersFeature.Trailers { get { throw null; } set { } }
        int Microsoft.AspNetCore.Server.Kestrel.Core.Features.IHttp2StreamIdFeature.StreamId { get { throw null; } }
        public bool ReceivedEmptyRequestBody { get { throw null; } }
        public System.IO.Pipelines.Pipe RequestBodyPipe { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool RequestBodyStarted { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public int StreamId { get { throw null; } }
        public void Abort(System.IO.IOException abortReason) { }
        public void AbortRstStreamReceived() { }
        protected override void ApplicationAbort() { }
        protected override Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.MessageBody CreateMessageBody() { throw null; }
        protected override string CreateRequestId() { throw null; }
        public abstract void Execute();
        public System.Threading.Tasks.Task OnDataAsync(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2Frame dataFrame, System.Buffers.ReadOnlySequence<byte> payload) { throw null; }
        public void OnDataRead(int bytesRead) { }
        public void OnEndStreamReceived() { }
        protected override void OnErrorAfterResponseStarted() { }
        protected override void OnRequestProcessingEnded() { }
        protected override void OnReset() { }
        protected override bool TryParseRequest(System.IO.Pipelines.ReadResult result, out bool endConnection) { throw null; }
        public bool TryUpdateOutputWindow(int bytes) { throw null; }
    }
    public partial class Http2StreamContext : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.HttpConnectionContext
    {
        public Http2StreamContext() { }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2PeerSettings ClientPeerSettings { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl.InputFlowControl ConnectionInputFlowControl { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl.OutputFlowControl ConnectionOutputFlowControl { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2FrameWriter FrameWriter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2PeerSettings ServerPeerSettings { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int StreamId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.IHttp2StreamLifetimeHandler StreamLifetimeHandler { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class Http2StreamErrorException : System.Exception
    {
        public Http2StreamErrorException(int streamId, string message, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode errorCode) { }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode ErrorCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public int StreamId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class Http2Stream<TContext> : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2Stream
    {
        public Http2Stream(Microsoft.AspNetCore.Hosting.Server.IHttpApplication<TContext> application, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2StreamContext context) : base (default(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2StreamContext)) { }
        public override void Execute() { }
    }
    public partial interface IHttp2StreamLifetimeHandler
    {
        void OnStreamCompleted(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2Stream stream);
    }
    public partial class ThreadPoolAwaitable : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
    {
        internal ThreadPoolAwaitable() { }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.ThreadPoolAwaitable Instance;
        public bool IsCompleted { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.ThreadPoolAwaitable GetAwaiter() { throw null; }
        public void GetResult() { }
        public void OnCompleted(System.Action continuation) { }
        public void UnsafeOnCompleted(System.Action continuation) { }
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct FlowControl
    {
        private int _dummyPrimitive;
        public FlowControl(uint initialWindowSize) { throw null; }
        public int Available { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool IsAborted { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void Abort() { }
        public void Advance(int bytes) { }
        public bool TryUpdateWindow(int bytes) { throw null; }
    }
    public partial class InputFlowControl
    {
        public InputFlowControl(uint initialWindowSize, uint minWindowSizeIncrement) { }
        public bool IsAvailabilityLow { get { throw null; } }
        public int Abort() { throw null; }
        public void StopWindowUpdates() { }
        public bool TryAdvance(int bytes) { throw null; }
        public bool TryUpdateWindow(int bytes, out int updateSize) { throw null; }
    }
    public partial class OutputFlowControl
    {
        public OutputFlowControl(uint initialWindowSize) { }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl.OutputFlowControlAwaitable AvailabilityAwaitable { get { throw null; } }
        public int Available { get { throw null; } }
        public bool IsAborted { get { throw null; } }
        public void Abort() { }
        public void Advance(int bytes) { }
        public bool TryUpdateWindow(int bytes) { throw null; }
    }
    public partial class OutputFlowControlAwaitable : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
    {
        public OutputFlowControlAwaitable() { }
        public bool IsCompleted { get { throw null; } }
        public void Complete() { }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl.OutputFlowControlAwaitable GetAwaiter() { throw null; }
        public void GetResult() { }
        public void OnCompleted(System.Action continuation) { }
        public void UnsafeOnCompleted(System.Action continuation) { }
    }
    public partial class StreamInputFlowControl
    {
        public StreamInputFlowControl(int streamId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2FrameWriter frameWriter, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl.InputFlowControl connectionLevelFlowControl, uint initialWindowSize, uint minWindowSizeIncrement) { }
        public void Abort() { }
        public void Advance(int bytes) { }
        public void StopWindowUpdates() { }
        public void UpdateWindows(int bytes) { }
    }
    public partial class StreamOutputFlowControl
    {
        public StreamOutputFlowControl(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl.OutputFlowControl connectionLevelFlowControl, uint initialWindowSize) { }
        public int Available { get { throw null; } }
        public bool IsAborted { get { throw null; } }
        public void Abort() { }
        public void Advance(int bytes) { }
        public int AdvanceUpToAndWait(long bytes, out Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl.OutputFlowControlAwaitable awaitable) { throw null; }
        public bool TryUpdateWindow(int bytes) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack
{
    public partial class DynamicTable
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
    public readonly partial struct HeaderField
    {
        private readonly object _dummy;
        public const int RfcOverhead = 32;
        public HeaderField(System.Span<byte> name, System.Span<byte> value) { throw null; }
        public int Length { get { throw null; } }
        public byte[] Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public byte[] Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public static int GetLength(int nameLength, int valueLength) { throw null; }
    }
    public partial class HPackDecoder
    {
        public HPackDecoder(int maxDynamicTableSize, int maxRequestHeaderFieldSize) { }
        public void Decode(System.Buffers.ReadOnlySequence<byte> data, bool endHeaders, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpHeadersHandler handler) { }
    }
    public partial class HPackDecodingException : System.Exception
    {
        public HPackDecodingException(string message) { }
        public HPackDecodingException(string message, System.Exception innerException) { }
    }
    public partial class HPackEncoder
    {
        public HPackEncoder() { }
        public bool BeginEncode(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>> headers, System.Span<byte> buffer, out int length) { throw null; }
        public bool BeginEncode(int statusCode, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>> headers, System.Span<byte> buffer, out int length) { throw null; }
        public bool Encode(System.Span<byte> buffer, out int length) { throw null; }
    }
    public partial class HPackEncodingException : System.Exception
    {
        public HPackEncodingException(string message) { }
        public HPackEncodingException(string message, System.Exception innerException) { }
    }
    public partial class Huffman
    {
        public Huffman() { }
        public static int Decode(System.ReadOnlySpan<byte> src, System.Span<byte> dst) { throw null; }
        public static (uint encoded, int bitLength) Encode(int data) { throw null; }
    }
    public partial class HuffmanDecodingException : System.Exception
    {
        public HuffmanDecodingException(string message) { }
    }
    public partial class IntegerDecoder
    {
        public IntegerDecoder() { }
        public bool BeginTryDecode(byte b, int prefixLength, out int result) { throw null; }
        public static void ThrowIntegerTooBigException() { }
        public bool TryDecode(byte b, out int result) { throw null; }
    }
    public static partial class IntegerEncoder
    {
        public static bool Encode(int i, int n, System.Span<byte> buffer, out int length) { throw null; }
    }
    public partial class StaticTable
    {
        internal StaticTable() { }
        public int Count { get { throw null; } }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack.StaticTable Instance { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack.HeaderField this[int index] { get { throw null; } }
        public System.Collections.Generic.IReadOnlyDictionary<int, int> StatusIndex { get { throw null; } }
    }
    public static partial class StatusCodes
    {
        public static byte[] ToStatusBytes(int statusCode) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    public partial class BodyControl
    {
        public BodyControl(Microsoft.AspNetCore.Http.Features.IHttpBodyControlFeature bodyControl, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpResponseControl responseControl) { }
        public void Abort(System.Exception error) { }
        public (System.IO.Stream request, System.IO.Stream response, System.IO.Pipelines.PipeReader reader, System.IO.Pipelines.PipeWriter writer) Start(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.MessageBody body) { throw null; }
        public void Stop() { }
        public System.IO.Stream Upgrade() { throw null; }
    }
    public partial class ConnectionManager
    {
        public ConnectionManager(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace trace, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ResourceCounter upgradedConnections) { }
        public ConnectionManager(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace trace, long? upgradedConnectionLimit) { }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ResourceCounter UpgradedConnectionCount { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void AddConnection(long id, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.KestrelConnection connection) { }
        public void RemoveConnection(long id) { }
        public void Walk(System.Action<Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.KestrelConnection> callback) { }
    }
    public static partial class ConnectionManagerShutdownExtensions
    {
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.Task<bool> AbortAllConnectionsAsync(this Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ConnectionManager connectionManager) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.Task<bool> CloseAllConnectionsAsync(this Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ConnectionManager connectionManager, System.Threading.CancellationToken token) { throw null; }
    }
    public partial class ConnectionReference
    {
        public ConnectionReference(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.KestrelConnection connection) { }
        public string ConnectionId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool TryGetConnection(out Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.KestrelConnection connection) { throw null; }
    }
    public partial class Disposable : System.IDisposable
    {
        public Disposable(System.Action dispose) { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
    }
    public partial class Heartbeat : System.IDisposable
    {
        public static readonly System.TimeSpan Interval;
        public Heartbeat(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IHeartbeatHandler[] callbacks, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ISystemClock systemClock, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IDebugger debugger, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace trace) { }
        public void Dispose() { }
        public void Start() { }
    }
    public partial class HeartbeatManager : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IHeartbeatHandler, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ISystemClock
    {
        public HeartbeatManager(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ConnectionManager connectionManager) { }
        public System.DateTimeOffset UtcNow { get { throw null; } }
        public long UtcNowTicks { get { throw null; } }
        public System.DateTimeOffset UtcNowUnsynchronized { get { throw null; } }
        public void OnHeartbeat(System.DateTimeOffset now) { }
    }
    public static partial class HttpUtilities
    {
        public const string Http10Version = "HTTP/1.0";
        public const string Http11Version = "HTTP/1.1";
        public const string Http2Version = "HTTP/2";
        public const string HttpsUriScheme = "https://";
        public const string HttpUriScheme = "http://";
        public static string GetAsciiOrUTF8StringNonNullCharacters(this System.Span<byte> span) { throw null; }
        public static string GetAsciiStringEscaped(this System.Span<byte> span, int maxChars) { throw null; }
        public static string GetAsciiStringNonNullCharacters(this System.Span<byte> span) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static bool GetKnownHttpScheme(this System.Span<byte> span, out Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpScheme knownScheme) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static bool GetKnownMethod(this System.Span<byte> span, out Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod method, out int length) { throw null; }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod GetKnownMethod(string value) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static bool GetKnownVersion(this System.Span<byte> span, out Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpVersion knownVersion, out byte length) { throw null; }
        public static bool IsHostHeaderValid(string hostText) { throw null; }
        public static string MethodToString(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod method) { throw null; }
        public static string SchemeToString(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpScheme scheme) { throw null; }
        public static string VersionToString(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpVersion httpVersion) { throw null; }
    }
    public partial interface IDebugger
    {
        bool IsAttached { get; }
    }
    public partial interface IHeartbeatHandler
    {
        void OnHeartbeat(System.DateTimeOffset now);
    }
    public partial interface IKestrelTrace : Microsoft.Extensions.Logging.ILogger
    {
        void ApplicationAbortedConnection(string connectionId, string traceIdentifier);
        void ApplicationError(string connectionId, string traceIdentifier, System.Exception ex);
        void ApplicationNeverCompleted(string connectionId);
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
    public partial interface ISystemClock
    {
        System.DateTimeOffset UtcNow { get; }
        long UtcNowTicks { get; }
        System.DateTimeOffset UtcNowUnsynchronized { get; }
    }
    public partial interface ITimeoutControl
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
    public partial interface ITimeoutHandler
    {
        void OnTimeout(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.TimeoutReason reason);
    }
    public partial class KestrelConnection
    {
        public KestrelConnection(Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.TransportConnection transportConnection) { }
        public System.Threading.Tasks.Task ExecutionTask { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.TransportConnection TransportConnection { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    [System.Diagnostics.Tracing.EventSourceAttribute(Name="Microsoft-AspNetCore-Server-Kestrel")]
    public sealed partial class KestrelEventSource : System.Diagnostics.Tracing.EventSource
    {
        internal KestrelEventSource() { }
        public static readonly Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.KestrelEventSource Log;
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Diagnostics.Tracing.EventAttribute(5, Level=System.Diagnostics.Tracing.EventLevel.Verbose)]
        public void ConnectionRejected(string connectionId) { }
        [System.Diagnostics.Tracing.NonEventAttribute]
        public void ConnectionStart(Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.TransportConnection connection) { }
        [System.Diagnostics.Tracing.NonEventAttribute]
        public void ConnectionStop(Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.TransportConnection connection) { }
        [System.Diagnostics.Tracing.NonEventAttribute]
        public void RequestStart(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol httpProtocol) { }
        [System.Diagnostics.Tracing.NonEventAttribute]
        public void RequestStop(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol httpProtocol) { }
    }
    public abstract partial class ReadOnlyStream : System.IO.Stream
    {
        protected ReadOnlyStream() { }
        public override bool CanRead { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override int WriteTimeout { get { throw null; } set { } }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public abstract partial class ResourceCounter
    {
        protected ResourceCounter() { }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ResourceCounter Unlimited { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ResourceCounter Quota(long amount) { throw null; }
        public abstract void ReleaseOne();
        public abstract bool TryLockOne();
    }
    public partial class ThrowingWasUpgradedWriteOnlyStream : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.WriteOnlyStream
    {
        public ThrowingWasUpgradedWriteOnlyStream() { }
        public override bool CanSeek { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public override void Flush() { }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public partial class TimeoutControl : Microsoft.AspNetCore.Server.Kestrel.Core.Features.IConnectionTimeoutFeature, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ITimeoutControl
    {
        public TimeoutControl(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ITimeoutHandler timeoutHandler) { }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.TimeoutReason TimerReason { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void BytesRead(long count) { }
        public void BytesWrittenToBuffer(Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate minRate, long count) { }
        public void CancelTimeout() { }
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
    public static partial class TimeoutControlExtensions
    {
        public static void StartDrainTimeout(this Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ITimeoutControl timeoutControl, Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate minDataRate, long? maxResponseBufferSize) { }
    }
    public enum TimeoutReason
    {
        KeepAlive = 1,
        None = 0,
        ReadDataRate = 3,
        RequestBodyDrain = 5,
        RequestHeaders = 2,
        TimeoutFeature = 6,
        WriteDataRate = 4,
    }
    public partial class TimingPipeFlusher
    {
        public TimingPipeFlusher(System.IO.Pipelines.PipeWriter writer, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ITimeoutControl timeoutControl, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace log) { }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FlushAsync() { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FlushAsync(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpOutputAborter outputAborter, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FlushAsync(Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate minRate, long count) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FlushAsync(Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate minRate, long count, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpOutputAborter outputAborter, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public abstract partial class WriteOnlyStream : System.IO.Stream
    {
        protected WriteOnlyStream() { }
        public override bool CanRead { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override int ReadTimeout { get { throw null; } set { } }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Https
{
    public enum ClientCertificateMode
    {
        AllowCertificate = 1,
        NoCertificate = 0,
        RequireCertificate = 2,
    }
    public partial class HttpsConnectionAdapterOptions
    {
        public HttpsConnectionAdapterOptions() { }
        public bool CheckCertificateRevocation { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Https.ClientCertificateMode ClientCertificateMode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<System.Security.Cryptography.X509Certificates.X509Certificate2, System.Security.Cryptography.X509Certificates.X509Chain, System.Net.Security.SslPolicyErrors, bool> ClientCertificateValidation { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.TimeSpan HandshakeTimeout { get { throw null; } set { } }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 ServerCertificate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.AspNetCore.Connections.ConnectionContext, string, System.Security.Cryptography.X509Certificates.X509Certificate2> ServerCertificateSelector { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Security.Authentication.SslProtocols SslProtocols { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Https.Internal
{
    public static partial class CertificateLoader
    {
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 LoadFromStoreCert(string subject, string storeName, System.Security.Cryptography.X509Certificates.StoreLocation storeLocation, bool allowInvalid) { throw null; }
    }
    public partial class HttpsConnectionAdapter : Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal.IConnectionAdapter
    {
        public HttpsConnectionAdapter(Microsoft.AspNetCore.Server.Kestrel.Https.HttpsConnectionAdapterOptions options) { }
        public HttpsConnectionAdapter(Microsoft.AspNetCore.Server.Kestrel.Https.HttpsConnectionAdapterOptions options, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public bool IsHttps { get { throw null; } }
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal.IAdaptedConnection> OnConnectionAsync(Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal.ConnectionAdapterContext context) { throw null; }
    }
}
