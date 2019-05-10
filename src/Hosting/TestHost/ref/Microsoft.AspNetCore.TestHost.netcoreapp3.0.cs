// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.TestHost
{
    public partial class ClientHandler : System.Net.Http.HttpMessageHandler
    {
        internal ClientHandler() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public static partial class HostBuilderTestServerExtensions
    {
        public static System.Net.Http.HttpClient GetTestClient(this Microsoft.Extensions.Hosting.IHost host) { throw null; }
        public static Microsoft.AspNetCore.TestHost.TestServer GetTestServer(this Microsoft.Extensions.Hosting.IHost host) { throw null; }
    }
    public partial class RequestBuilder
    {
        public RequestBuilder(Microsoft.AspNetCore.TestHost.TestServer server, string path) { }
        public Microsoft.AspNetCore.TestHost.RequestBuilder AddHeader(string name, string value) { throw null; }
        public Microsoft.AspNetCore.TestHost.RequestBuilder And(System.Action<System.Net.Http.HttpRequestMessage> configure) { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> GetAsync() { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PostAsync() { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(string method) { throw null; }
    }
    public partial class TestServer : Microsoft.AspNetCore.Hosting.Server.IServer, System.IDisposable
    {
        public TestServer() { }
        public TestServer(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder) { }
        public TestServer(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder, Microsoft.AspNetCore.Http.Features.IFeatureCollection featureCollection) { }
        public TestServer(Microsoft.AspNetCore.Http.Features.IFeatureCollection featureCollection) { }
        public bool AllowSynchronousIO { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Uri BaseAddress { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.Features.IFeatureCollection Features { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Hosting.IWebHost Host { get { throw null; } }
        public bool PreserveExecutionContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Net.Http.HttpClient CreateClient() { throw null; }
        public System.Net.Http.HttpMessageHandler CreateHandler() { throw null; }
        public Microsoft.AspNetCore.TestHost.RequestBuilder CreateRequest(string path) { throw null; }
        public Microsoft.AspNetCore.TestHost.WebSocketClient CreateWebSocketClient() { throw null; }
        public void Dispose() { }
        System.Threading.Tasks.Task Microsoft.AspNetCore.Hosting.Server.IServer.StartAsync<TContext>(Microsoft.AspNetCore.Hosting.Server.IHttpApplication<TContext> application, System.Threading.CancellationToken cancellationToken) { throw null; }
        System.Threading.Tasks.Task Microsoft.AspNetCore.Hosting.Server.IServer.StopAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Http.HttpContext> SendAsync(System.Action<Microsoft.AspNetCore.Http.HttpContext> configureContext, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public static partial class WebHostBuilderExtensions
    {
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder ConfigureTestContainer<TContainer>(this Microsoft.AspNetCore.Hosting.IWebHostBuilder webHostBuilder, System.Action<TContainer> servicesConfiguration) { throw null; }
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder ConfigureTestServices(this Microsoft.AspNetCore.Hosting.IWebHostBuilder webHostBuilder, System.Action<Microsoft.Extensions.DependencyInjection.IServiceCollection> servicesConfiguration) { throw null; }
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder UseSolutionRelativeContentRoot(this Microsoft.AspNetCore.Hosting.IWebHostBuilder builder, string solutionRelativePath, string solutionName = "*.sln") { throw null; }
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder UseSolutionRelativeContentRoot(this Microsoft.AspNetCore.Hosting.IWebHostBuilder builder, string solutionRelativePath, string applicationBasePath, string solutionName = "*.sln") { throw null; }
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder UseTestServer(this Microsoft.AspNetCore.Hosting.IWebHostBuilder builder) { throw null; }
    }
    public static partial class WebHostBuilderFactory
    {
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder CreateFromAssemblyEntryPoint(System.Reflection.Assembly assembly, string[] args) { throw null; }
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder CreateFromTypesAssemblyEntryPoint<T>(string[] args) { throw null; }
    }
    public partial class WebSocketClient
    {
        internal WebSocketClient() { }
        public System.Action<Microsoft.AspNetCore.Http.HttpRequest> ConfigureRequest { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IList<string> SubProtocols { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<System.Net.WebSockets.WebSocket> ConnectAsync(System.Uri uri, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
}
