// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class ComponentEndpointConventionBuilderExtensions
    {
        public static Microsoft.AspNetCore.Components.Server.ComponentEndpointConventionBuilder AddComponent(this Microsoft.AspNetCore.Components.Server.ComponentEndpointConventionBuilder builder, System.Type componentType, string selector) { throw null; }
    }
    public static partial class ComponentEndpointRouteBuilderExtensions
    {
        public static Microsoft.AspNetCore.Components.Server.ComponentEndpointConventionBuilder MapBlazorHub(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) { throw null; }
        public static Microsoft.AspNetCore.Components.Server.ComponentEndpointConventionBuilder MapBlazorHub(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, System.Action<Microsoft.AspNetCore.Http.Connections.HttpConnectionDispatcherOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Components.Server.ComponentEndpointConventionBuilder MapBlazorHub(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, System.Type type, string selector) { throw null; }
        public static Microsoft.AspNetCore.Components.Server.ComponentEndpointConventionBuilder MapBlazorHub(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, System.Type type, string selector, System.Action<Microsoft.AspNetCore.Http.Connections.HttpConnectionDispatcherOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Components.Server.ComponentEndpointConventionBuilder MapBlazorHub(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, System.Type componentType, string selector, string path) { throw null; }
        public static Microsoft.AspNetCore.Components.Server.ComponentEndpointConventionBuilder MapBlazorHub(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, System.Type componentType, string selector, string path, System.Action<Microsoft.AspNetCore.Http.Connections.HttpConnectionDispatcherOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Components.Server.ComponentEndpointConventionBuilder MapBlazorHub<TComponent>(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string selector) where TComponent : Microsoft.AspNetCore.Components.IComponent { throw null; }
        public static Microsoft.AspNetCore.Components.Server.ComponentEndpointConventionBuilder MapBlazorHub<TComponent>(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string selector, System.Action<Microsoft.AspNetCore.Http.Connections.HttpConnectionDispatcherOptions> configureOptions) where TComponent : Microsoft.AspNetCore.Components.IComponent { throw null; }
        public static Microsoft.AspNetCore.Components.Server.ComponentEndpointConventionBuilder MapBlazorHub<TComponent>(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string selector, string path) where TComponent : Microsoft.AspNetCore.Components.IComponent { throw null; }
        public static Microsoft.AspNetCore.Components.Server.ComponentEndpointConventionBuilder MapBlazorHub<TComponent>(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string selector, string path, System.Action<Microsoft.AspNetCore.Http.Connections.HttpConnectionDispatcherOptions> configureOptions) where TComponent : Microsoft.AspNetCore.Components.IComponent { throw null; }
    }
}
namespace Microsoft.AspNetCore.Components.Server
{
    public partial class CircuitOptions
    {
        public CircuitOptions() { }
        public System.TimeSpan DisconnectedCircuitRetentionPeriod { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.TimeSpan JSInteropDefaultCallTimeout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool JSInteropDetailedErrors { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int MaxRetainedDisconnectedCircuits { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public sealed partial class ComponentEndpointConventionBuilder : Microsoft.AspNetCore.Builder.IEndpointConventionBuilder, Microsoft.AspNetCore.SignalR.IHubEndpointConventionBuilder
    {
        internal ComponentEndpointConventionBuilder() { }
        public void Add(System.Action<Microsoft.AspNetCore.Builder.EndpointBuilder> convention) { }
    }
    public partial class ComponentPrerenderingContext
    {
        public ComponentPrerenderingContext() { }
        public System.Type ComponentType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.HttpContext Context { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Components.ParameterCollection Parameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public sealed partial class ComponentPrerenderResult
    {
        internal ComponentPrerenderResult() { }
        public void WriteTo(System.IO.TextWriter writer) { }
    }
    public partial interface IComponentPrerenderer
    {
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Components.Server.ComponentPrerenderResult> PrerenderComponentAsync(Microsoft.AspNetCore.Components.Server.ComponentPrerenderingContext context);
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
}
namespace Microsoft.AspNetCore.Components.Web.Rendering
{
    public partial class RemoteRendererException : System.Exception
    {
        public RemoteRendererException(string message) { }
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
