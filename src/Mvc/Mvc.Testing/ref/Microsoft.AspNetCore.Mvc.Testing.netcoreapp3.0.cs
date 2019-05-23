// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Testing
{
    public partial class WebApplicationFactoryClientOptions
    {
        public WebApplicationFactoryClientOptions() { }
        public bool AllowAutoRedirect { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Uri BaseAddress { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool HandleCookies { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int MaxAutomaticRedirections { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly, Inherited=false, AllowMultiple=true)]
    public sealed partial class WebApplicationFactoryContentRootAttribute : System.Attribute
    {
        public WebApplicationFactoryContentRootAttribute(string key, string contentRootPath, string contentRootTest, string priority) { }
        public string ContentRootPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string ContentRootTest { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Key { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public int Priority { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class WebApplicationFactory<TEntryPoint> : System.IDisposable where TEntryPoint : class
    {
        public WebApplicationFactory() { }
        public Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions ClientOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<TEntryPoint>> Factories { get { throw null; } }
        public Microsoft.AspNetCore.TestHost.TestServer Server { get { throw null; } }
        public virtual System.IServiceProvider Services { get { throw null; } }
        protected virtual void ConfigureClient(System.Net.Http.HttpClient client) { }
        protected virtual void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder) { }
        public System.Net.Http.HttpClient CreateClient() { throw null; }
        public System.Net.Http.HttpClient CreateClient(Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions options) { throw null; }
        public System.Net.Http.HttpClient CreateDefaultClient(params System.Net.Http.DelegatingHandler[] handlers) { throw null; }
        public System.Net.Http.HttpClient CreateDefaultClient(System.Uri baseAddress, params System.Net.Http.DelegatingHandler[] handlers) { throw null; }
        protected virtual Microsoft.Extensions.Hosting.IHost CreateHost(Microsoft.Extensions.Hosting.IHostBuilder builder) { throw null; }
        protected virtual Microsoft.Extensions.Hosting.IHostBuilder CreateHostBuilder() { throw null; }
        protected virtual Microsoft.AspNetCore.TestHost.TestServer CreateServer(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder) { throw null; }
        protected virtual Microsoft.AspNetCore.Hosting.IWebHostBuilder CreateWebHostBuilder() { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~WebApplicationFactory() { }
        protected virtual System.Collections.Generic.IEnumerable<System.Reflection.Assembly> GetTestAssemblies() { throw null; }
        public Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<TEntryPoint> WithWebHostBuilder(System.Action<Microsoft.AspNetCore.Hosting.IWebHostBuilder> configuration) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.Testing.Handlers
{
    public partial class CookieContainerHandler : System.Net.Http.DelegatingHandler
    {
        public CookieContainerHandler() { }
        public CookieContainerHandler(System.Net.CookieContainer cookieContainer) { }
        public System.Net.CookieContainer Container { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public partial class RedirectHandler : System.Net.Http.DelegatingHandler
    {
        public RedirectHandler() { }
        public RedirectHandler(int maxRedirects) { }
        public int MaxRedirects { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
}
