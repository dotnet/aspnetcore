// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.WebAssembly
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static partial class JSInteropMethods
    {
        [Microsoft.JSInterop.JSInvokableAttribute("NotifyLocationChanged")]
        public static void NotifyLocationChanged(string uri, bool isInterceptedLink) { }
    }
}
namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct RootComponentMapping
    {
        private readonly object _dummy;
        public RootComponentMapping(System.Type componentType, string selector) { throw null; }
        public System.Type ComponentType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Selector { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class RootComponentMappingCollection : System.Collections.ObjectModel.Collection<Microsoft.AspNetCore.Components.WebAssembly.Hosting.RootComponentMapping>
    {
        public RootComponentMappingCollection() { }
        public void Add(System.Type componentType, string selector) { }
        public void AddRange(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Components.WebAssembly.Hosting.RootComponentMapping> items) { }
        public void Add<TComponent>(string selector) where TComponent : Microsoft.AspNetCore.Components.IComponent { }
    }
    public sealed partial class WebAssemblyHost : System.IAsyncDisposable
    {
        internal WebAssemblyHost() { }
        public Microsoft.Extensions.Configuration.IConfiguration Configuration { get { throw null; } }
        public System.IServiceProvider Services { get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        public System.Threading.Tasks.Task RunAsync() { throw null; }
    }
    public sealed partial class WebAssemblyHostBuilder
    {
        internal WebAssemblyHostBuilder() { }
        public Microsoft.Extensions.Configuration.IConfigurationBuilder Configuration { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Components.WebAssembly.Hosting.RootComponentMappingCollection RootComponents { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.Extensions.DependencyInjection.IServiceCollection Services { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Components.WebAssembly.Hosting.WebAssemblyHost Build() { throw null; }
        public static Microsoft.AspNetCore.Components.WebAssembly.Hosting.WebAssemblyHostBuilder CreateDefault(string[] args = null) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Components.WebAssembly.Http
{
    public enum FetchCredentialsOption
    {
        Omit = 0,
        SameOrigin = 1,
        Include = 2,
    }
    public static partial class WebAssemblyHttpMessageHandlerOptions
    {
        public static Microsoft.AspNetCore.Components.WebAssembly.Http.FetchCredentialsOption DefaultCredentials { get { throw null; } set { } }
    }
}
namespace Microsoft.AspNetCore.Components.WebAssembly.Rendering
{
    public static partial class WebAssemblyEventDispatcher
    {
        [Microsoft.JSInterop.JSInvokableAttribute("DispatchEvent")]
        public static System.Threading.Tasks.Task DispatchEvent(Microsoft.AspNetCore.Components.RenderTree.WebEventDescriptor eventDescriptor, string eventArgsJson) { throw null; }
    }
}
