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
        public Microsoft.Extensions.Configuration.IConfigurationSection ConfigSection { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Https.HttpsConnectionAdapterOptions HttpsOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool IsHttps { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions ListenOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class KestrelConfigurationLoader
    {
        internal KestrelConfigurationLoader() { }
        public Microsoft.Extensions.Configuration.IConfiguration Configuration { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions Options { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
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
        public int StatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
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
    public partial class Http3Limits
    {
        public Http3Limits() { }
        public int HeaderTableSize { get { throw null; } set { } }
        public int MaxRequestHeaderFieldSize { get { throw null; } set { } }
    }
    [System.FlagsAttribute]
    public enum HttpProtocols
    {
        None = 0,
        Http1 = 1,
        Http2 = 2,
        Http1AndHttp2 = 3,
        Http3 = 4,
        Http1AndHttp2AndHttp3 = 7,
    }
    public partial class KestrelServer : Microsoft.AspNetCore.Hosting.Server.IServer, System.IDisposable
    {
        public KestrelServer(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions> options, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Connections.IConnectionListenerFactory> transportFactories, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public KestrelServer(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions> options, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Connections.IConnectionListenerFactory> transportFactories, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Connections.IMultiplexedConnectionListenerFactory> multiplexedFactories, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.AspNetCore.Http.Features.IFeatureCollection Features { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
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
        public Microsoft.AspNetCore.Server.Kestrel.Core.Http2Limits Http2 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Http3Limits Http3 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.TimeSpan KeepAliveTimeout { get { throw null; } set { } }
        public long? MaxConcurrentConnections { get { throw null; } set { } }
        public long? MaxConcurrentUpgradedConnections { get { throw null; } set { } }
        public long? MaxRequestBodySize { get { throw null; } set { } }
        public long? MaxRequestBufferSize { get { throw null; } set { } }
        public int MaxRequestHeaderCount { get { throw null; } set { } }
        public int MaxRequestHeadersTotalSize { get { throw null; } set { } }
        public int MaxRequestLineSize { get { throw null; } set { } }
        public long? MaxResponseBufferSize { get { throw null; } set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate MinRequestBodyDataRate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate MinResponseDataRate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.TimeSpan RequestHeadersTimeout { get { throw null; } set { } }
    }
    public partial class KestrelServerOptions
    {
        public KestrelServerOptions() { }
        public bool AddServerHeader { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool AllowSynchronousIO { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.IServiceProvider ApplicationServices { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Server.Kestrel.KestrelConfigurationLoader ConfigurationLoader { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool DisableStringReuse { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool EnableAltSvc { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerLimits Limits { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
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
    public partial class ListenOptions : Microsoft.AspNetCore.Connections.IConnectionBuilder, Microsoft.AspNetCore.Connections.IMultiplexedConnectionBuilder
    {
        internal ListenOptions() { }
        public System.IServiceProvider ApplicationServices { get { throw null; } }
        public ulong FileHandle { get { throw null; } }
        public System.Net.IPEndPoint IPEndPoint { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions KestrelServerOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols Protocols { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string SocketPath { get { throw null; } }
        public Microsoft.AspNetCore.Connections.ConnectionDelegate Build() { throw null; }
        Microsoft.AspNetCore.Connections.MultiplexedConnectionDelegate Microsoft.AspNetCore.Connections.IMultiplexedConnectionBuilder.Build() { throw null; }
        Microsoft.AspNetCore.Connections.IMultiplexedConnectionBuilder Microsoft.AspNetCore.Connections.IMultiplexedConnectionBuilder.Use(System.Func<Microsoft.AspNetCore.Connections.MultiplexedConnectionDelegate, Microsoft.AspNetCore.Connections.MultiplexedConnectionDelegate> middleware) { throw null; }
        public override string ToString() { throw null; }
        public Microsoft.AspNetCore.Connections.IConnectionBuilder Use(System.Func<Microsoft.AspNetCore.Connections.ConnectionDelegate, Microsoft.AspNetCore.Connections.ConnectionDelegate> middleware) { throw null; }
    }
    public partial class MinDataRate
    {
        public MinDataRate(double bytesPerSecond, System.TimeSpan gracePeriod) { }
        public double BytesPerSecond { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.TimeSpan GracePeriod { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
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
namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public enum HttpMethod : byte
    {
        Get = (byte)0,
        Put = (byte)1,
        Delete = (byte)2,
        Post = (byte)3,
        Head = (byte)4,
        Trace = (byte)5,
        Patch = (byte)6,
        Connect = (byte)7,
        Options = (byte)8,
        Custom = (byte)9,
        None = (byte)255,
    }
    public partial class HttpParser<TRequestHandler> where TRequestHandler : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpHeadersHandler, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpRequestLineHandler
    {
        public HttpParser() { }
        public HttpParser(bool showErrorDetails) { }
        public bool ParseHeaders(TRequestHandler handler, ref System.Buffers.SequenceReader<byte> reader) { throw null; }
        public bool ParseRequestLine(TRequestHandler handler, in System.Buffers.ReadOnlySequence<byte> buffer, out System.SequencePosition consumed, out System.SequencePosition examined) { throw null; }
    }
    public enum HttpScheme
    {
        Unknown = -1,
        Http = 0,
        Https = 1,
    }
    public enum HttpVersion
    {
        Unknown = -1,
        Http10 = 0,
        Http11 = 1,
        Http2 = 2,
        Http3 = 3,
    }
    public partial interface IHttpHeadersHandler
    {
        void OnHeader(System.ReadOnlySpan<byte> name, System.ReadOnlySpan<byte> value);
        void OnHeadersComplete(bool endStream);
        void OnStaticIndexedHeader(int index);
        void OnStaticIndexedHeader(int index, System.ReadOnlySpan<byte> value);
    }
    public partial interface IHttpRequestLineHandler
    {
        void OnStartLine(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod method, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpVersion version, System.Span<byte> target, System.Span<byte> path, System.Span<byte> query, System.Span<byte> customMethod, bool pathEncoded);
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Https
{
    public static partial class CertificateLoader
    {
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 LoadFromStoreCert(string subject, string storeName, System.Security.Cryptography.X509Certificates.StoreLocation storeLocation, bool allowInvalid) { throw null; }
    }
    public enum ClientCertificateMode
    {
        NoCertificate = 0,
        AllowCertificate = 1,
        RequireCertificate = 2,
    }
    public partial class HttpsConnectionAdapterOptions
    {
        public HttpsConnectionAdapterOptions() { }
        public bool CheckCertificateRevocation { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Https.ClientCertificateMode ClientCertificateMode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Func<System.Security.Cryptography.X509Certificates.X509Certificate2, System.Security.Cryptography.X509Certificates.X509Chain, System.Net.Security.SslPolicyErrors, bool> ClientCertificateValidation { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.TimeSpan HandshakeTimeout { get { throw null; } set { } }
        public System.Action<Microsoft.AspNetCore.Connections.ConnectionContext, System.Net.Security.SslServerAuthenticationOptions> OnAuthenticate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 ServerCertificate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Func<Microsoft.AspNetCore.Connections.ConnectionContext, string, System.Security.Cryptography.X509Certificates.X509Certificate2> ServerCertificateSelector { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Security.Authentication.SslProtocols SslProtocols { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public void AllowAnyClientCertificate() { }
    }
}
