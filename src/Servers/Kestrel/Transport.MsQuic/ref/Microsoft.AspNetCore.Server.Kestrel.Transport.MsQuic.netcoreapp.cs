// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Hosting
{
    public static partial class WebHostBuilderMsQuicExtensions
    {
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder UseMsQuic(this Microsoft.AspNetCore.Hosting.IWebHostBuilder hostBuilder) { throw null; }
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder UseMsQuic(this Microsoft.AspNetCore.Hosting.IWebHostBuilder hostBuilder, System.Action<Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.MsQuicTransportOptions> configureOptions) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic
{
    public partial interface IMsQuicTrace : Microsoft.Extensions.Logging.ILogger
    {
        void ConnectionError(string connectionId, System.Exception ex);
        void NewConnection(string connectionId);
        void NewStream(string streamId);
        void StreamError(string streamId, System.Exception ex);
    }
    public partial class MsQuicConnectionFactory : Microsoft.AspNetCore.Connections.IConnectionFactory
    {
        public MsQuicConnectionFactory(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.MsQuicTransportOptions> options, Microsoft.Extensions.Hosting.IHostApplicationLifetime lifetime, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.MsQuicTransportContext TransportContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.ValueTask<Microsoft.AspNetCore.Connections.ConnectionContext> ConnectAsync(System.Net.EndPoint endPoint, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public partial class MsQuicConnectionListener : Microsoft.AspNetCore.Connections.IConnectionListener, System.IAsyncDisposable, System.IDisposable
    {
        public MsQuicConnectionListener(Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.MsQuicTransportContext transportContext, System.Net.EndPoint endpoint) { }
        public Microsoft.Extensions.Hosting.IHostApplicationLifetime AppLifetime { get { throw null; } }
        public System.Net.EndPoint EndPoint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.IMsQuicTrace Log { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.MsQuicTransportContext TransportContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.ValueTask<Microsoft.AspNetCore.Connections.ConnectionContext> AcceptAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public void Dispose() { }
        public System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        ~MsQuicConnectionListener() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task StartAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected void StopAcceptingConnections() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.ValueTask UnbindAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public partial class MsQuicTransportContext
    {
        public MsQuicTransportContext(Microsoft.Extensions.Hosting.IHostApplicationLifetime appLifetime, Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.IMsQuicTrace log, Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.MsQuicTransportOptions options) { }
        public Microsoft.Extensions.Hosting.IHostApplicationLifetime AppLifetime { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.IMsQuicTrace Log { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.MsQuicTransportOptions Options { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class MsQuicTransportFactory : Microsoft.AspNetCore.Connections.IConnectionListenerFactory
    {
        public MsQuicTransportFactory(Microsoft.Extensions.Hosting.IHostApplicationLifetime applicationLifetime, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.MsQuicTransportOptions> options) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.ValueTask<Microsoft.AspNetCore.Connections.IConnectionListener> BindAsync(System.Net.EndPoint endpoint, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public partial class MsQuicTransportOptions
    {
        public MsQuicTransportOptions() { }
        public string Alpn { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 Certificate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.TimeSpan IdleTimeout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public ushort MaxBidirectionalStreamCount { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public long? MaxReadBufferSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public ushort MaxUnidirectionalStreamCount { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public long? MaxWriteBufferSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string RegistrationName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
}
