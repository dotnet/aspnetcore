// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http.Connections.Client
{
    public partial class HttpConnection : Microsoft.AspNetCore.Connections.ConnectionContext, Microsoft.AspNetCore.Connections.Features.IConnectionInherentKeepAliveFeature
    {
        public HttpConnection(Microsoft.AspNetCore.Http.Connections.Client.HttpConnectionOptions httpConnectionOptions, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public HttpConnection(System.Uri url) { }
        public HttpConnection(System.Uri url, Microsoft.AspNetCore.Http.Connections.HttpTransportType transports) { }
        public HttpConnection(System.Uri url, Microsoft.AspNetCore.Http.Connections.HttpTransportType transports, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public override string ConnectionId { get { throw null; } set { } }
        public override Microsoft.AspNetCore.Http.Features.IFeatureCollection Features { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public override System.Collections.Generic.IDictionary<object, object> Items { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        bool Microsoft.AspNetCore.Connections.Features.IConnectionInherentKeepAliveFeature.HasInherentKeepAlive { get { throw null; } }
        public override System.IO.Pipelines.IDuplexPipe Transport { get { throw null; } set { } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task StartAsync(Microsoft.AspNetCore.Connections.TransferFormat transferFormat, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Threading.Tasks.Task StartAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public partial class HttpConnectionOptions
    {
        public HttpConnectionOptions() { }
        public System.Func<System.Threading.Tasks.Task<string>> AccessTokenProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Security.Cryptography.X509Certificates.X509CertificateCollection ClientCertificates { get { throw null; } set { } }
        public System.TimeSpan CloseTimeout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Net.CookieContainer Cookies { get { throw null; } set { } }
        public System.Net.ICredentials Credentials { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IDictionary<string, string> Headers { get { throw null; } set { } }
        public System.Func<System.Net.Http.HttpMessageHandler, System.Net.Http.HttpMessageHandler> HttpMessageHandlerFactory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Net.IWebProxy Proxy { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool SkipNegotiation { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.Connections.HttpTransportType Transports { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Uri Url { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool? UseDefaultCredentials { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Action<System.Net.WebSockets.ClientWebSocketOptions> WebSocketConfiguration { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class NoTransportSupportedException : System.Exception
    {
        public NoTransportSupportedException(string message) { }
    }
    public partial class TransportFailedException : System.Exception
    {
        public TransportFailedException(string transportType, string message, System.Exception innerException = null) { }
        public string TransportType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
}
