// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public sealed partial class HubEndpointConventionBuilder : Microsoft.AspNetCore.Builder.IEndpointConventionBuilder, Microsoft.AspNetCore.Builder.IHubEndpointConventionBuilder
    {
        internal HubEndpointConventionBuilder() { }
        public void Add(System.Action<Microsoft.AspNetCore.Builder.EndpointBuilder> convention) { }
    }
    public static partial class HubEndpointRouteBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.HubEndpointConventionBuilder MapHub<THub>(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern) where THub : Microsoft.AspNetCore.SignalR.Hub { throw null; }
        public static Microsoft.AspNetCore.Builder.HubEndpointConventionBuilder MapHub<THub>(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern, System.Action<Microsoft.AspNetCore.Http.Connections.HttpConnectionDispatcherOptions> configureOptions) where THub : Microsoft.AspNetCore.SignalR.Hub { throw null; }
    }
    public partial interface IHubEndpointConventionBuilder : Microsoft.AspNetCore.Builder.IEndpointConventionBuilder
    {
    }
    public static partial class SignalRAppBuilderExtensions
    {
        [System.ObsoleteAttribute("This method is obsolete and will be removed in a future version. The recommended alternative is to use MapHub<THub> inside Microsoft.AspNetCore.Builder.UseEndpoints(...).")]
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseSignalR(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, System.Action<Microsoft.AspNetCore.SignalR.HubRouteBuilder> configure) { throw null; }
    }
}
namespace Microsoft.AspNetCore.SignalR
{
    public static partial class GetHttpContextExtensions
    {
        public static Microsoft.AspNetCore.Http.HttpContext GetHttpContext(this Microsoft.AspNetCore.SignalR.HubCallerContext connection) { throw null; }
        public static Microsoft.AspNetCore.Http.HttpContext GetHttpContext(this Microsoft.AspNetCore.SignalR.HubConnectionContext connection) { throw null; }
    }
    [System.ObsoleteAttribute("This class is obsolete and will be removed in a future version. The recommended alternative is to use MapHub<THub> inside Microsoft.AspNetCore.Builder.UseEndpoints(...).")]
    public partial class HubRouteBuilder
    {
        public HubRouteBuilder(Microsoft.AspNetCore.Http.Connections.ConnectionsRouteBuilder routes) { }
        public void MapHub<THub>(Microsoft.AspNetCore.Http.PathString path) where THub : Microsoft.AspNetCore.SignalR.Hub { }
        public void MapHub<THub>(Microsoft.AspNetCore.Http.PathString path, System.Action<Microsoft.AspNetCore.Http.Connections.HttpConnectionDispatcherOptions> configureOptions) where THub : Microsoft.AspNetCore.SignalR.Hub { }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class SignalRDependencyInjectionExtensions
    {
        public static Microsoft.AspNetCore.SignalR.ISignalRServerBuilder AddHubOptions<THub>(this Microsoft.AspNetCore.SignalR.ISignalRServerBuilder signalrBuilder, System.Action<Microsoft.AspNetCore.SignalR.HubOptions<THub>> configure) where THub : Microsoft.AspNetCore.SignalR.Hub { throw null; }
        public static Microsoft.AspNetCore.SignalR.ISignalRServerBuilder AddSignalR(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.AspNetCore.SignalR.ISignalRServerBuilder AddSignalR(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.SignalR.HubOptions> configure) { throw null; }
    }
}
