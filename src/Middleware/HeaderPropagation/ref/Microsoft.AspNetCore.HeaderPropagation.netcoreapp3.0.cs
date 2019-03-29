// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class HeaderPropagationApplicationBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseHeaderPropagation(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
    }
}
namespace Microsoft.AspNetCore.HeaderPropagation
{
    public partial class HeaderPropagationEntry
    {
        public HeaderPropagationEntry() { }
        public Microsoft.Extensions.Primitives.StringValues DefaultValue { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string OutboundHeaderName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<string, Microsoft.AspNetCore.Http.HttpContext, Microsoft.Extensions.Primitives.StringValues> ValueFactory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class HeaderPropagationMessageHandler : System.Net.Http.DelegatingHandler
    {
        public HeaderPropagationMessageHandler(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.HeaderPropagation.HeaderPropagationOptions> options, Microsoft.AspNetCore.HeaderPropagation.HeaderPropagationValues values) { }
        protected override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public partial class HeaderPropagationMiddleware
    {
        public HeaderPropagationMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.HeaderPropagation.HeaderPropagationOptions> options, Microsoft.AspNetCore.HeaderPropagation.HeaderPropagationValues values) { }
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
    public partial class HeaderPropagationOptions
    {
        public HeaderPropagationOptions() { }
        public System.Collections.Generic.IDictionary<string, Microsoft.AspNetCore.HeaderPropagation.HeaderPropagationEntry> Headers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class HeaderPropagationValues
    {
        public HeaderPropagationValues() { }
        public System.Collections.Generic.IDictionary<string, Microsoft.Extensions.Primitives.StringValues> Headers { get { throw null; } }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class HeaderPropagationHttpClientBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddHeaderPropagation(this Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder) { throw null; }
    }
    public static partial class HeaderPropagationServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddHeaderPropagation(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddHeaderPropagation(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.HeaderPropagation.HeaderPropagationOptions> configureOptions) { throw null; }
    }
}
