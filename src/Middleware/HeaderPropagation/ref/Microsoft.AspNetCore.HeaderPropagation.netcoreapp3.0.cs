// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class HeaderPropagationApplicationBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseHeaderPropagation(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseHeaderPropagation(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, bool includeInLoggerScope) { throw null; }
    }
}
namespace Microsoft.AspNetCore.HeaderPropagation
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct HeaderPropagationContext
    {
        private readonly object _dummy;
        public HeaderPropagationContext(Microsoft.AspNetCore.Http.HttpContext httpContext, string headerName, Microsoft.Extensions.Primitives.StringValues headerValue) { throw null; }
        public string HeaderName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.Extensions.Primitives.StringValues HeaderValue { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Http.HttpContext HttpContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class HeaderPropagationEntry
    {
        public HeaderPropagationEntry(string inboundHeaderName, string capturedHeaderName, System.Func<Microsoft.AspNetCore.HeaderPropagation.HeaderPropagationContext, Microsoft.Extensions.Primitives.StringValues> valueFilter) { }
        public string CapturedHeaderName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string InboundHeaderName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Func<Microsoft.AspNetCore.HeaderPropagation.HeaderPropagationContext, Microsoft.Extensions.Primitives.StringValues> ValueFilter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public sealed partial class HeaderPropagationEntryCollection : System.Collections.ObjectModel.Collection<Microsoft.AspNetCore.HeaderPropagation.HeaderPropagationEntry>
    {
        public HeaderPropagationEntryCollection() { }
        public void Add(string headerName) { }
        public void Add(string headerName, System.Func<Microsoft.AspNetCore.HeaderPropagation.HeaderPropagationContext, Microsoft.Extensions.Primitives.StringValues> valueFilter) { }
        public void Add(string inboundHeaderName, string outboundHeaderName) { }
        public void Add(string inboundHeaderName, string outboundHeaderName, System.Func<Microsoft.AspNetCore.HeaderPropagation.HeaderPropagationContext, Microsoft.Extensions.Primitives.StringValues> valueFilter) { }
    }
    public partial class HeaderPropagationLoggerScopeMiddleware
    {
        public HeaderPropagationLoggerScopeMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.HeaderPropagation.HeaderPropagationLoggerScopeMiddleware> logger, Microsoft.AspNetCore.HeaderPropagation.IHeaderPropagationLoggerScopeBuilder loggerScopeBuilder) { }
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
    public partial class HeaderPropagationMessageHandler : System.Net.Http.DelegatingHandler
    {
        public HeaderPropagationMessageHandler(Microsoft.AspNetCore.HeaderPropagation.HeaderPropagationMessageHandlerOptions options, Microsoft.AspNetCore.HeaderPropagation.HeaderPropagationValues values) { }
        protected override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public partial class HeaderPropagationMessageHandlerEntry
    {
        public HeaderPropagationMessageHandlerEntry(string capturedHeaderName, string outboundHeaderName) { }
        public string CapturedHeaderName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string OutboundHeaderName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public sealed partial class HeaderPropagationMessageHandlerEntryCollection : System.Collections.ObjectModel.Collection<Microsoft.AspNetCore.HeaderPropagation.HeaderPropagationMessageHandlerEntry>
    {
        public HeaderPropagationMessageHandlerEntryCollection() { }
        public void Add(string headerName) { }
        public void Add(string capturedHeaderName, string outboundHeaderName) { }
    }
    public partial class HeaderPropagationMessageHandlerOptions
    {
        public HeaderPropagationMessageHandlerOptions() { }
        public Microsoft.AspNetCore.HeaderPropagation.HeaderPropagationMessageHandlerEntryCollection Headers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class HeaderPropagationMiddleware
    {
        public HeaderPropagationMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.HeaderPropagation.HeaderPropagationOptions> options, Microsoft.AspNetCore.HeaderPropagation.HeaderPropagationValues values) { }
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
    public partial class HeaderPropagationOptions
    {
        public HeaderPropagationOptions() { }
        public Microsoft.AspNetCore.HeaderPropagation.HeaderPropagationEntryCollection Headers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class HeaderPropagationValues
    {
        public HeaderPropagationValues() { }
    }
    public partial interface IHeaderPropagationLoggerScopeBuilder
    {
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class HeaderPropagationHttpClientBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHeaderPropagation(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHeaderPropagation(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder, System.Action<Microsoft.AspNetCore.HeaderPropagation.HeaderPropagationMessageHandlerOptions> configure) { throw null; }
    }
    public static partial class HeaderPropagationServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddHeaderPropagation(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddHeaderPropagation(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.HeaderPropagation.HeaderPropagationOptions> configureOptions) { throw null; }
    }
}
