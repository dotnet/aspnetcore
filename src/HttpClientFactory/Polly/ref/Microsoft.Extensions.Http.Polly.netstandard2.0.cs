// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class PollyHttpClientBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddPolicyHandler(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, Polly.IAsyncPolicy<System.Net.Http.HttpResponseMessage> policy) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddPolicyHandler(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Func<System.IServiceProvider, System.Net.Http.HttpRequestMessage, Polly.IAsyncPolicy<System.Net.Http.HttpResponseMessage>> policySelector) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddPolicyHandler(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Func<System.IServiceProvider, System.Net.Http.HttpRequestMessage, string, Polly.IAsyncPolicy<System.Net.Http.HttpResponseMessage>> policyFactory, System.Func<System.Net.Http.HttpRequestMessage, string> keySelector) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddPolicyHandler(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Func<System.Net.Http.HttpRequestMessage, Polly.IAsyncPolicy<System.Net.Http.HttpResponseMessage>> policySelector) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddPolicyHandlerFromRegistry(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Func<Polly.Registry.IReadOnlyPolicyRegistry<string>, System.Net.Http.HttpRequestMessage, Polly.IAsyncPolicy<System.Net.Http.HttpResponseMessage>> policySelector) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddPolicyHandlerFromRegistry(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, string policyKey) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddTransientHttpErrorPolicy(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Func<Polly.PolicyBuilder<System.Net.Http.HttpResponseMessage>, Polly.IAsyncPolicy<System.Net.Http.HttpResponseMessage>> configurePolicy) { throw null; }
    }
    public static partial class PollyServiceCollectionExtensions
    {
        public static Polly.Registry.IPolicyRegistry<string> AddPolicyRegistry(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Polly.Registry.IPolicyRegistry<string> AddPolicyRegistry(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, Polly.Registry.IPolicyRegistry<string> registry) { throw null; }
    }
}
namespace Microsoft.Extensions.Http
{
    public partial class PolicyHttpMessageHandler : System.Net.Http.DelegatingHandler
    {
        public PolicyHttpMessageHandler(Polly.IAsyncPolicy<System.Net.Http.HttpResponseMessage> policy) { }
        public PolicyHttpMessageHandler(System.Func<System.Net.Http.HttpRequestMessage, Polly.IAsyncPolicy<System.Net.Http.HttpResponseMessage>> policySelector) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected virtual System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendCoreAsync(System.Net.Http.HttpRequestMessage request, Polly.Context context, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
}
namespace Polly
{
    public static partial class HttpRequestMessageExtensions
    {
        public static Polly.Context GetPolicyExecutionContext(this System.Net.Http.HttpRequestMessage request) { throw null; }
        public static void SetPolicyExecutionContext(this System.Net.Http.HttpRequestMessage request, Polly.Context context) { }
    }
}
