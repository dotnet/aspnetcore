// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class CertificateForwardingBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseCertificateForwarding(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
    }
    public static partial class ForwardedHeadersExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseForwardedHeaders(this Microsoft.AspNetCore.Builder.IApplicationBuilder builder) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseForwardedHeaders(this Microsoft.AspNetCore.Builder.IApplicationBuilder builder, Microsoft.AspNetCore.Builder.ForwardedHeadersOptions options) { throw null; }
    }
    public partial class ForwardedHeadersOptions
    {
        public ForwardedHeadersOptions() { }
        public System.Collections.Generic.IList<string> AllowedHosts { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ForwardedForHeaderName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders ForwardedHeaders { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ForwardedHostHeaderName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ForwardedProtoHeaderName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int? ForwardLimit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.HttpOverrides.IPNetwork> KnownNetworks { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IList<System.Net.IPAddress> KnownProxies { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string OriginalForHeaderName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string OriginalHostHeaderName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string OriginalProtoHeaderName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool RequireHeaderSymmetry { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public static partial class HttpMethodOverrideExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseHttpMethodOverride(this Microsoft.AspNetCore.Builder.IApplicationBuilder builder) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseHttpMethodOverride(this Microsoft.AspNetCore.Builder.IApplicationBuilder builder, Microsoft.AspNetCore.Builder.HttpMethodOverrideOptions options) { throw null; }
    }
    public partial class HttpMethodOverrideOptions
    {
        public HttpMethodOverrideOptions() { }
        public string FormFieldName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
}
namespace Microsoft.AspNetCore.HttpOverrides
{
    public partial class CertificateForwardingMiddleware
    {
        public CertificateForwardingMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.HttpOverrides.CertificateForwardingOptions> options) { }
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
    }
    public partial class CertificateForwardingOptions
    {
        public System.Func<string, System.Security.Cryptography.X509Certificates.X509Certificate2> HeaderConverter;
        public CertificateForwardingOptions() { }
        public string CertificateHeader { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    [System.FlagsAttribute]
    public enum ForwardedHeaders
    {
        None = 0,
        XForwardedFor = 1,
        XForwardedHost = 2,
        XForwardedProto = 4,
        All = 7,
    }
    public static partial class ForwardedHeadersDefaults
    {
        public static string XForwardedForHeaderName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public static string XForwardedHostHeaderName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public static string XForwardedProtoHeaderName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public static string XOriginalForHeaderName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public static string XOriginalHostHeaderName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public static string XOriginalProtoHeaderName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class ForwardedHeadersMiddleware
    {
        public ForwardedHeadersMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions> options) { }
        public void ApplyForwarders(Microsoft.AspNetCore.Http.HttpContext context) { }
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
    public partial class HttpMethodOverrideMiddleware
    {
        public HttpMethodOverrideMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Builder.HttpMethodOverrideOptions> options) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
    public partial class IPNetwork
    {
        public IPNetwork(System.Net.IPAddress prefix, int prefixLength) { }
        public System.Net.IPAddress Prefix { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public int PrefixLength { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool Contains(System.Net.IPAddress address) { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class CertificateForwardingServiceExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddCertificateForwarding(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.HttpOverrides.CertificateForwardingOptions> configure) { throw null; }
    }
}
