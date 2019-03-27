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
    public partial interface IHttpParser<TRequestHandler> where TRequestHandler : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpHeadersHandler, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpRequestLineHandler
    {
        bool ParseHeaders(TRequestHandler handler, in System.Buffers.ReadOnlySequence<byte> buffer, out System.SequencePosition consumed, out System.SequencePosition examined, out int consumedBytes);
        bool ParseRequestLine(TRequestHandler handler, in System.Buffers.ReadOnlySequence<byte> buffer, out System.SequencePosition consumed, out System.SequencePosition examined);
    }
    public partial interface IHttpRequestLineHandler
    {
        void OnStartLine(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod method, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpVersion version, System.Span<byte> target, System.Span<byte> path, System.Span<byte> query, System.Span<byte> customMethod, bool pathEncoded);
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
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
}
