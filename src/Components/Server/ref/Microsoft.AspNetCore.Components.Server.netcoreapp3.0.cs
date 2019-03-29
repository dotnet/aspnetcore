// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class ComponentEndpointConventionBuilderExtensions
    {
        public static TBuilder AddComponent<TBuilder>(this TBuilder builder, System.Type componentType, string selector) where TBuilder : Microsoft.AspNetCore.SignalR.HubEndpointConventionBuilder { throw null; }
    }
    public static partial class ComponentEndpointRouteBuilderExtensions
    {
        public static Microsoft.AspNetCore.SignalR.ComponentEndpointConventionBuilder MapBlazorHub(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) { throw null; }
        public static Microsoft.AspNetCore.SignalR.ComponentEndpointConventionBuilder MapBlazorHub(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, System.Type componentType, string selector, string path) { throw null; }
        public static Microsoft.AspNetCore.SignalR.ComponentEndpointConventionBuilder MapBlazorHub<TComponent>(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string selector) where TComponent : Microsoft.AspNetCore.Components.IComponent { throw null; }
        public static Microsoft.AspNetCore.SignalR.ComponentEndpointConventionBuilder MapBlazorHub<TComponent>(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string selector, string path) where TComponent : Microsoft.AspNetCore.Components.IComponent { throw null; }
    }
}
namespace Microsoft.AspNetCore.Components.Browser.Rendering
{
    public partial class RemoteRendererException : System.Exception
    {
        public RemoteRendererException(string message) { }
    }
}
namespace Microsoft.AspNetCore.Components.Server
{
    public partial class CircuitOptions
    {
        public CircuitOptions() { }
        public System.TimeSpan DisconnectedCircuitRetentionPeriod { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int MaxRetainedDisconnectedCircuits { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public sealed partial class ComponentHub : Microsoft.AspNetCore.SignalR.Hub
    {
        public ComponentHub(System.IServiceProvider services, Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Components.Server.ComponentHub> logger) { }
        public static Microsoft.AspNetCore.Http.PathString DefaultPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void BeginInvokeDotNetFromJS(string callId, string assemblyName, string methodIdentifier, long dotNetObjectId, string argsJson) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<bool> ConnectCircuit(string circuitId) { throw null; }
        public override System.Threading.Tasks.Task OnDisconnectedAsync(System.Exception exception) { throw null; }
        public void OnRenderCompleted(long renderId, string errorMessageOrNull) { }
        public string StartCircuit(string uriAbsolute, string baseUriAbsolute) { throw null; }
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
    public static partial class WasmMediaTypeNames
    {
        public static partial class Application
        {
            public const string Wasm = "application/wasm";
        }
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
    public partial class RemoteUriHelper : Microsoft.AspNetCore.Components.UriHelperBase
    {
        public RemoteUriHelper(Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Components.Server.Circuits.RemoteUriHelper> logger) { }
        public bool HasAttachedJSRuntime { get { throw null; } }
        public override void InitializeState(string uriAbsolute, string baseUriAbsolute) { }
        protected override void NavigateToCore(string uri, bool forceLoad) { }
        [Microsoft.JSInterop.JSInvokableAttribute("NotifyLocationChanged")]
        public static void NotifyLocationChanged(string uriAbsolute) { }
    }
}
namespace Microsoft.AspNetCore.SignalR
{
    public partial class ComponentEndpointConventionBuilder : Microsoft.AspNetCore.SignalR.HubEndpointConventionBuilder
    {
        internal ComponentEndpointConventionBuilder() : base (default(Microsoft.AspNetCore.Builder.IEndpointConventionBuilder)) { }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ComponentServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddServerSideBlazor(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
    }
}
