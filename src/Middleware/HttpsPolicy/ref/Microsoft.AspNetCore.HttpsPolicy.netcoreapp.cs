// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class HstsBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseHsts(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
    }
    public static partial class HstsServicesExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddHsts(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.HttpsPolicy.HstsOptions> configureOptions) { throw null; }
    }
    public static partial class HttpsPolicyBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseHttpsRedirection(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
    }
    public static partial class HttpsRedirectionServicesExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddHttpsRedirection(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionOptions> configureOptions) { throw null; }
    }
}
namespace Microsoft.AspNetCore.HttpsPolicy
{
    public partial class HstsMiddleware
    {
        public HstsMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.HttpsPolicy.HstsOptions> options) { }
        public HstsMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.HttpsPolicy.HstsOptions> options, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
    public partial class HstsOptions
    {
        public HstsOptions() { }
        public System.Collections.Generic.IList<string> ExcludedHosts { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool IncludeSubDomains { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.TimeSpan MaxAge { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool Preload { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class HttpsRedirectionMiddleware
    {
        public HttpsRedirectionMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionOptions> options, Microsoft.Extensions.Configuration.IConfiguration config, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public HttpsRedirectionMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionOptions> options, Microsoft.Extensions.Configuration.IConfiguration config, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature serverAddressesFeature) { }
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
    public partial class HttpsRedirectionOptions
    {
        public HttpsRedirectionOptions() { }
        public int? HttpsPort { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int RedirectStatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
}
