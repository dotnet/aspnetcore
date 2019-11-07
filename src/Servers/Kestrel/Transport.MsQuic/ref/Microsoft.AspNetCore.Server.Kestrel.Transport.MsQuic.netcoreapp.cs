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
    public partial class MsQuicConnectionFactory : Microsoft.AspNetCore.Connections.IConnectionFactory
    {
        public MsQuicConnectionFactory(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.MsQuicTransportOptions> options, Microsoft.Extensions.Hosting.IHostApplicationLifetime lifetime, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.ValueTask<Microsoft.AspNetCore.Connections.ConnectionContext> ConnectAsync(System.Net.EndPoint endPoint, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
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
