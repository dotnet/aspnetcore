// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class HttpClientBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpMessageHandler(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Func<System.IServiceProvider, System.Net.Http.DelegatingHandler> configureHandler) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpMessageHandler(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Func<System.Net.Http.DelegatingHandler> configureHandler) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpMessageHandler<THandler>(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder) where THandler : System.Net.Http.DelegatingHandler { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddTypedClient<TClient>(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder) where TClient : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddTypedClient<TClient>(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Func<System.Net.Http.HttpClient, System.IServiceProvider, TClient> factory) where TClient : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddTypedClient<TClient>(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Func<System.Net.Http.HttpClient, TClient> factory) where TClient : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddTypedClient<TClient, TImplementation>(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder) where TClient : class where TImplementation : class, TClient { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder ConfigureHttpClient(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Action<System.IServiceProvider, System.Net.Http.HttpClient> configureClient) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder ConfigureHttpClient(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Action<System.Net.Http.HttpClient> configureClient) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder ConfigureHttpMessageHandlerBuilder(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Action<Microsoft.Extensions.Http.HttpMessageHandlerBuilder> configureBuilder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder ConfigurePrimaryHttpMessageHandler(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Func<System.IServiceProvider, System.Net.Http.HttpMessageHandler> configureHandler) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder ConfigurePrimaryHttpMessageHandler(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Func<System.Net.Http.HttpMessageHandler> configureHandler) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder ConfigurePrimaryHttpMessageHandler<THandler>(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder) where THandler : System.Net.Http.HttpMessageHandler { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder RedactLoggedHeaders(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Collections.Generic.IEnumerable<string> redactedLoggedHeaderNames) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder RedactLoggedHeaders(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Func<string, bool> shouldRedactHeaderValue) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder SetHandlerLifetime(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.TimeSpan handlerLifetime) { throw null; }
    }
    public static partial class HttpClientFactoryServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddHttpClient(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string name) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string name, System.Action<System.IServiceProvider, System.Net.Http.HttpClient> configureClient) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string name, System.Action<System.Net.Http.HttpClient> configureClient) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<TClient>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) where TClient : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<TClient>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<System.IServiceProvider, System.Net.Http.HttpClient> configureClient) where TClient : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<TClient>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<System.Net.Http.HttpClient> configureClient) where TClient : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<TClient>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string name) where TClient : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<TClient>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string name, System.Action<System.IServiceProvider, System.Net.Http.HttpClient> configureClient) where TClient : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<TClient>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string name, System.Action<System.Net.Http.HttpClient> configureClient) where TClient : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<TClient, TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) where TClient : class where TImplementation : class, TClient { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<TClient, TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<System.IServiceProvider, System.Net.Http.HttpClient> configureClient) where TClient : class where TImplementation : class, TClient { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<TClient, TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<System.Net.Http.HttpClient> configureClient) where TClient : class where TImplementation : class, TClient { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<TClient, TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string name) where TClient : class where TImplementation : class, TClient { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<TClient, TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string name, System.Action<System.IServiceProvider, System.Net.Http.HttpClient> configureClient) where TClient : class where TImplementation : class, TClient { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHttpClient<TClient, TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string name, System.Action<System.Net.Http.HttpClient> configureClient) where TClient : class where TImplementation : class, TClient { throw null; }
    }
    public partial interface IHttpClientBuilder
    {
        string Name { get; }
        Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; }
    }
}
namespace Microsoft.Extensions.Http
{
    public partial class HttpClientFactoryOptions
    {
        public HttpClientFactoryOptions() { }
        public System.TimeSpan HandlerLifetime { get { throw null; } set { } }
        public System.Collections.Generic.IList<System.Action<System.Net.Http.HttpClient>> HttpClientActions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IList<System.Action<Microsoft.Extensions.Http.HttpMessageHandlerBuilder>> HttpMessageHandlerBuilderActions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Func<string, bool> ShouldRedactHeaderValue { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool SuppressHandlerScope { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public abstract partial class HttpMessageHandlerBuilder
    {
        protected HttpMessageHandlerBuilder() { }
        public abstract System.Collections.Generic.IList<System.Net.Http.DelegatingHandler> AdditionalHandlers { get; }
        public abstract string Name { get; set; }
        public abstract System.Net.Http.HttpMessageHandler PrimaryHandler { get; set; }
        public virtual System.IServiceProvider Services { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public abstract System.Net.Http.HttpMessageHandler Build();
        protected internal static System.Net.Http.HttpMessageHandler CreateHandlerPipeline(System.Net.Http.HttpMessageHandler primaryHandler, System.Collections.Generic.IEnumerable<System.Net.Http.DelegatingHandler> additionalHandlers) { throw null; }
    }
    public partial interface IHttpMessageHandlerBuilderFilter
    {
        System.Action<Microsoft.Extensions.Http.HttpMessageHandlerBuilder> Configure(System.Action<Microsoft.Extensions.Http.HttpMessageHandlerBuilder> next);
    }
    public partial interface ITypedHttpClientFactory<TClient>
    {
        TClient CreateClient(System.Net.Http.HttpClient httpClient);
    }
}
namespace Microsoft.Extensions.Http.Logging
{
    public partial class LoggingHttpMessageHandler : System.Net.Http.DelegatingHandler
    {
        public LoggingHttpMessageHandler(Microsoft.Extensions.Logging.ILogger logger) { }
        public LoggingHttpMessageHandler(Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.Http.HttpClientFactoryOptions options) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public partial class LoggingScopeHttpMessageHandler : System.Net.Http.DelegatingHandler
    {
        public LoggingScopeHttpMessageHandler(Microsoft.Extensions.Logging.ILogger logger) { }
        public LoggingScopeHttpMessageHandler(Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.Http.HttpClientFactoryOptions options) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
}
namespace System.Net.Http
{
    public static partial class HttpClientFactoryExtensions
    {
        public static System.Net.Http.HttpClient CreateClient(this System.Net.Http.IHttpClientFactory factory) { throw null; }
    }
    public static partial class HttpMessageHandlerFactoryExtensions
    {
        public static System.Net.Http.HttpMessageHandler CreateHandler(this System.Net.Http.IHttpMessageHandlerFactory factory) { throw null; }
    }
    public partial interface IHttpClientFactory
    {
        System.Net.Http.HttpClient CreateClient(string name);
    }
    public partial interface IHttpMessageHandlerFactory
    {
        System.Net.Http.HttpMessageHandler CreateHandler(string name);
    }
}
