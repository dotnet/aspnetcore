// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR.Client
{
    public partial class HttpConnectionFactory : Microsoft.AspNetCore.SignalR.Client.IConnectionFactory
    {
        public HttpConnectionFactory(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Http.Connections.Client.HttpConnectionOptions> options, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Connections.ConnectionContext> ConnectAsync(Microsoft.AspNetCore.Connections.TransferFormat transferFormat, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Threading.Tasks.Task DisposeAsync(Microsoft.AspNetCore.Connections.ConnectionContext connection) { throw null; }
    }
    public static partial class HubConnectionBuilderHttpExtensions
    {
        public static Microsoft.AspNetCore.SignalR.Client.IHubConnectionBuilder WithUrl(this Microsoft.AspNetCore.SignalR.Client.IHubConnectionBuilder hubConnectionBuilder, string url) { throw null; }
        public static Microsoft.AspNetCore.SignalR.Client.IHubConnectionBuilder WithUrl(this Microsoft.AspNetCore.SignalR.Client.IHubConnectionBuilder hubConnectionBuilder, string url, Microsoft.AspNetCore.Http.Connections.HttpTransportType transports) { throw null; }
        public static Microsoft.AspNetCore.SignalR.Client.IHubConnectionBuilder WithUrl(this Microsoft.AspNetCore.SignalR.Client.IHubConnectionBuilder hubConnectionBuilder, string url, Microsoft.AspNetCore.Http.Connections.HttpTransportType transports, System.Action<Microsoft.AspNetCore.Http.Connections.Client.HttpConnectionOptions> configureHttpConnection) { throw null; }
        public static Microsoft.AspNetCore.SignalR.Client.IHubConnectionBuilder WithUrl(this Microsoft.AspNetCore.SignalR.Client.IHubConnectionBuilder hubConnectionBuilder, string url, System.Action<Microsoft.AspNetCore.Http.Connections.Client.HttpConnectionOptions> configureHttpConnection) { throw null; }
        public static Microsoft.AspNetCore.SignalR.Client.IHubConnectionBuilder WithUrl(this Microsoft.AspNetCore.SignalR.Client.IHubConnectionBuilder hubConnectionBuilder, System.Uri url) { throw null; }
        public static Microsoft.AspNetCore.SignalR.Client.IHubConnectionBuilder WithUrl(this Microsoft.AspNetCore.SignalR.Client.IHubConnectionBuilder hubConnectionBuilder, System.Uri url, Microsoft.AspNetCore.Http.Connections.HttpTransportType transports) { throw null; }
        public static Microsoft.AspNetCore.SignalR.Client.IHubConnectionBuilder WithUrl(this Microsoft.AspNetCore.SignalR.Client.IHubConnectionBuilder hubConnectionBuilder, System.Uri url, Microsoft.AspNetCore.Http.Connections.HttpTransportType transports, System.Action<Microsoft.AspNetCore.Http.Connections.Client.HttpConnectionOptions> configureHttpConnection) { throw null; }
        public static Microsoft.AspNetCore.SignalR.Client.IHubConnectionBuilder WithUrl(this Microsoft.AspNetCore.SignalR.Client.IHubConnectionBuilder hubConnectionBuilder, System.Uri url, System.Action<Microsoft.AspNetCore.Http.Connections.Client.HttpConnectionOptions> configureHttpConnection) { throw null; }
    }
}
