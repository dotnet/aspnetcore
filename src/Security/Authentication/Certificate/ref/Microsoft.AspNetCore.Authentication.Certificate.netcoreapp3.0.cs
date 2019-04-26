// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication.Certificate
{
    public static partial class CertificateAuthenticationDefaults
    {
        public const string AuthenticationScheme = "Certificate";
        public const string CertificateItemsKey = "Certificate";
    }
    public partial class CertificateAuthenticationEvents
    {
        public CertificateAuthenticationEvents() { }
        public System.Func<Microsoft.AspNetCore.Authentication.Certificate.CertificateAuthenticationFailedContext, System.Threading.Tasks.Task> OnAuthenticationFailed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.Certificate.ValidateCertificateContext, System.Threading.Tasks.Task> OnValidateCertificate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task AuthenticationFailed(Microsoft.AspNetCore.Authentication.Certificate.CertificateAuthenticationFailedContext context) { throw null; }
        public virtual System.Threading.Tasks.Task ValidateCertificate(Microsoft.AspNetCore.Authentication.Certificate.ValidateCertificateContext context) { throw null; }
    }
    public partial class CertificateAuthenticationFailedContext : Microsoft.AspNetCore.Authentication.ResultContext<Microsoft.AspNetCore.Authentication.Certificate.CertificateAuthenticationOptions>
    {
        public CertificateAuthenticationFailedContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.Certificate.CertificateAuthenticationOptions options) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.Certificate.CertificateAuthenticationOptions)) { }
        public System.Exception Exception { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class CertificateAuthenticationOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions
    {
        public CertificateAuthenticationOptions() { }
        public Microsoft.AspNetCore.Authentication.Certificate.CertificateTypes AllowedCertificateTypes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public new Microsoft.AspNetCore.Authentication.Certificate.CertificateAuthenticationEvents Events { get { throw null; } set { } }
        public System.Security.Cryptography.X509Certificates.X509RevocationFlag RevocationFlag { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Security.Cryptography.X509Certificates.X509RevocationMode RevocationMode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool ValidateCertificateUse { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool ValidateValidityPeriod { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class CertificateForwarderMiddleware
    {
        public CertificateForwarderMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Authentication.Certificate.CertificateForwarderOptions> options) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
    }
    public partial class CertificateForwarderOptions
    {
        public System.Func<string, System.Security.Cryptography.X509Certificates.X509Certificate2> HeaderConverter;
        public CertificateForwarderOptions() { }
        public string CertificateHeader { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    [System.FlagsAttribute]
    public enum CertificateTypes
    {
        Chained = 1,
        SelfSigned = 2,
        All = 3,
    }
    public static partial class CertificateValidator
    {
        public static bool DisableChannelValidation(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors errors) { throw null; }
    }
    public partial class ValidateCertificateContext : Microsoft.AspNetCore.Authentication.ResultContext<Microsoft.AspNetCore.Authentication.Certificate.CertificateAuthenticationOptions>
    {
        public ValidateCertificateContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.Certificate.CertificateAuthenticationOptions options) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.Certificate.CertificateAuthenticationOptions)) { }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 ClientCertificate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public static partial class X509Certificate2Extensions
    {
        public static bool IsSelfSigned(this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Hosting
{
    public static partial class CertificateForwarderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddCertificateHeaderForwarding(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.Authentication.Certificate.CertificateForwarderOptions> configure) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseCertificateHeaderForwarding(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class CertificateAuthenticationAppBuilderExtensions
    {
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddCertificate(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddCertificate(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, System.Action<Microsoft.AspNetCore.Authentication.Certificate.CertificateAuthenticationOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddCertificate(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string authenticationScheme) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddCertificate(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string authenticationScheme, System.Action<Microsoft.AspNetCore.Authentication.Certificate.CertificateAuthenticationOptions> configureOptions) { throw null; }
    }
}
