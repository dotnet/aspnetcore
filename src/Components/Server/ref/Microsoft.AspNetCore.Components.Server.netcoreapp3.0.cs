// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public sealed partial class ComponentEndpointConventionBuilder : Microsoft.AspNetCore.Builder.IEndpointConventionBuilder, Microsoft.AspNetCore.Builder.IHubEndpointConventionBuilder
    {
        internal ComponentEndpointConventionBuilder() { }
        public void Add(System.Action<Microsoft.AspNetCore.Builder.EndpointBuilder> convention) { }
    }
    public static partial class ComponentEndpointConventionBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.ComponentEndpointConventionBuilder AddComponent(this Microsoft.AspNetCore.Builder.ComponentEndpointConventionBuilder builder, System.Type componentType, string selector) { throw null; }
    }
    public static partial class ComponentEndpointRouteBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.ComponentEndpointConventionBuilder MapBlazorHub(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, System.Type type, string selector) { throw null; }
        public static Microsoft.AspNetCore.Builder.ComponentEndpointConventionBuilder MapBlazorHub(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, System.Type type, string selector, System.Action<Microsoft.AspNetCore.Http.Connections.HttpConnectionDispatcherOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Builder.ComponentEndpointConventionBuilder MapBlazorHub(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, System.Type componentType, string selector, string path) { throw null; }
        public static Microsoft.AspNetCore.Builder.ComponentEndpointConventionBuilder MapBlazorHub(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, System.Type componentType, string selector, string path, System.Action<Microsoft.AspNetCore.Http.Connections.HttpConnectionDispatcherOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Builder.ComponentEndpointConventionBuilder MapBlazorHub<TComponent>(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string selector) where TComponent : Microsoft.AspNetCore.Components.IComponent { throw null; }
        public static Microsoft.AspNetCore.Builder.ComponentEndpointConventionBuilder MapBlazorHub<TComponent>(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string selector, System.Action<Microsoft.AspNetCore.Http.Connections.HttpConnectionDispatcherOptions> configureOptions) where TComponent : Microsoft.AspNetCore.Components.IComponent { throw null; }
        public static Microsoft.AspNetCore.Builder.ComponentEndpointConventionBuilder MapBlazorHub<TComponent>(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string selector, string path) where TComponent : Microsoft.AspNetCore.Components.IComponent { throw null; }
        public static Microsoft.AspNetCore.Builder.ComponentEndpointConventionBuilder MapBlazorHub<TComponent>(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string selector, string path, System.Action<Microsoft.AspNetCore.Http.Connections.HttpConnectionDispatcherOptions> configureOptions) where TComponent : Microsoft.AspNetCore.Components.IComponent { throw null; }
    }
}
namespace Microsoft.AspNetCore.Components.Server
{
    public sealed partial class CircuitOptions
    {
        public CircuitOptions() { }
        public bool DetailedErrors { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int DisconnectedCircuitMaxRetained { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.TimeSpan DisconnectedCircuitRetentionPeriod { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.TimeSpan JSInteropDefaultCallTimeout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    public sealed partial class Circuit
    {
        internal Circuit() { }
        public string Id { get { throw null; } }
    }
    public abstract partial class CircuitHandler
    {
        protected CircuitHandler() { }
        public virtual int Order { get { throw null; } }
        public virtual System.Threading.Tasks.Task OnCircuitClosedAsync(Microsoft.AspNetCore.Components.Server.Circuits.Circuit circuit, System.Threading.CancellationToken cancellationToken) { throw null; }
        public virtual System.Threading.Tasks.Task OnCircuitOpenedAsync(Microsoft.AspNetCore.Components.Server.Circuits.Circuit circuit, System.Threading.CancellationToken cancellationToken) { throw null; }
        public virtual System.Threading.Tasks.Task OnConnectionDownAsync(Microsoft.AspNetCore.Components.Server.Circuits.Circuit circuit, System.Threading.CancellationToken cancellationToken) { throw null; }
        public virtual System.Threading.Tasks.Task OnConnectionUpAsync(Microsoft.AspNetCore.Components.Server.Circuits.Circuit circuit, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public partial class RemoteNavigationManager : Microsoft.AspNetCore.Components.NavigationManager
    {
        public RemoteNavigationManager(Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Components.Server.Circuits.RemoteNavigationManager> logger) { }
        public bool HasAttachedJSRuntime { get { throw null; } }
        public override void InitializeState(string uriAbsolute, string baseUriAbsolute) { }
        protected override void NavigateToCore(string uri, bool forceLoad) { }
        [Microsoft.JSInterop.JSInvokableAttribute("NotifyLocationChanged")]
        public static void NotifyLocationChanged(string uriAbsolute, bool isInterceptedLink) { }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ComponentServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServerSideBlazorBuilder AddServerSideBlazor(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.Components.Server.CircuitOptions> configure = null) { throw null; }
    }
    public partial interface IServerSideBlazorBuilder
    {
        Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; }
    }
    public static partial class ServerSideBlazorBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServerSideBlazorBuilder AddCircuitOptions(this Microsoft.Extensions.DependencyInjection.IServerSideBlazorBuilder builder, System.Action<Microsoft.AspNetCore.Components.Server.CircuitOptions> configure) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServerSideBlazorBuilder AddHubOptions(this Microsoft.Extensions.DependencyInjection.IServerSideBlazorBuilder builder, System.Action<Microsoft.AspNetCore.SignalR.HubOptions> configure) { throw null; }
    }
}
