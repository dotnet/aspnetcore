// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public sealed partial class ConnectionEndpointRouteBuilder : Microsoft.AspNetCore.Builder.IEndpointConventionBuilder
    {
        internal ConnectionEndpointRouteBuilder() { }
        public void Add(System.Action<Microsoft.AspNetCore.Builder.EndpointBuilder> convention) { }
    }
    public static partial class ConnectionEndpointRouteBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.ConnectionEndpointRouteBuilder MapConnectionHandler<TConnectionHandler>(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern) where TConnectionHandler : Microsoft.AspNetCore.Connections.ConnectionHandler { throw null; }
        public static Microsoft.AspNetCore.Builder.ConnectionEndpointRouteBuilder MapConnectionHandler<TConnectionHandler>(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern, System.Action<Microsoft.AspNetCore.Http.Connections.HttpConnectionDispatcherOptions> configureOptions) where TConnectionHandler : Microsoft.AspNetCore.Connections.ConnectionHandler { throw null; }
        public static Microsoft.AspNetCore.Builder.ConnectionEndpointRouteBuilder MapConnections(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern, Microsoft.AspNetCore.Http.Connections.HttpConnectionDispatcherOptions options, System.Action<Microsoft.AspNetCore.Connections.IConnectionBuilder> configure) { throw null; }
        public static Microsoft.AspNetCore.Builder.ConnectionEndpointRouteBuilder MapConnections(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern, System.Action<Microsoft.AspNetCore.Connections.IConnectionBuilder> configure) { throw null; }
    }
    public static partial class ConnectionsAppBuilderExtensions
    {
        [System.ObsoleteAttribute("This method is obsolete and will be removed in a future version. The recommended alternative is to use MapConnections or MapConnectionHandler<TConnectionHandler> inside Microsoft.AspNetCore.Builder.UseEndpoints(...).")]
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseConnections(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, System.Action<Microsoft.AspNetCore.Http.Connections.ConnectionsRouteBuilder> configure) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Http.Connections
{
    public partial class ConnectionOptions
    {
        public ConnectionOptions() { }
        public System.TimeSpan? DisconnectTimeout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class ConnectionOptionsSetup : Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.Http.Connections.ConnectionOptions>
    {
        public static System.TimeSpan DefaultDisconectTimeout;
        public ConnectionOptionsSetup() { }
        public void Configure(Microsoft.AspNetCore.Http.Connections.ConnectionOptions options) { }
    }
    [System.ObsoleteAttribute("This class is obsolete and will be removed in a future version. The recommended alternative is to use MapConnection and MapConnectionHandler<TConnectionHandler> inside Microsoft.AspNetCore.Builder.UseEndpoints(...).")]
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
        public long ApplicationMaxBufferSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Authorization.IAuthorizeData> AuthorizationData { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Http.Connections.LongPollingOptions LongPolling { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public int MinimumProtocolVersion { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public long TransportMaxBufferSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Http.Connections.HttpTransportType Transports { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Http.Connections.WebSocketOptions WebSockets { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class LongPollingOptions
    {
        public LongPollingOptions() { }
        public System.TimeSpan PollTimeout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class NegotiateMetadata
    {
        public NegotiateMetadata() { }
    }
    public partial class WebSocketOptions
    {
        public WebSocketOptions() { }
        public System.TimeSpan CloseTimeout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Func<System.Collections.Generic.IList<string>, string> SubProtocolSelector { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
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
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ConnectionsDependencyInjectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddConnections(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddConnections(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.Http.Connections.ConnectionOptions> options) { throw null; }
    }
}
