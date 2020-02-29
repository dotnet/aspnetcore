// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public partial class IISOptions
    {
        public IISOptions() { }
        public string AuthenticationDisplayName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool AutomaticAuthentication { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool ForwardClientCertificate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
}
namespace Microsoft.AspNetCore.Hosting
{
    public static partial class WebHostBuilderIISExtensions
    {
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder UseIISIntegration(this Microsoft.AspNetCore.Hosting.IWebHostBuilder hostBuilder) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Server.IISIntegration
{
    public partial class IISDefaults
    {
        public const string AuthenticationScheme = "Windows";
        public const string Negotiate = "Negotiate";
        public const string Ntlm = "NTLM";
        public IISDefaults() { }
    }
    public partial class IISHostingStartup : Microsoft.AspNetCore.Hosting.IHostingStartup
    {
        public IISHostingStartup() { }
        public void Configure(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder) { }
    }
    public partial class IISMiddleware
    {
        public IISMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Builder.IISOptions> options, string pairingToken, Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider authentication, Microsoft.Extensions.Hosting.IHostApplicationLifetime applicationLifetime) { }
        public IISMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Builder.IISOptions> options, string pairingToken, bool isWebsocketsSupported, Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider authentication, Microsoft.Extensions.Hosting.IHostApplicationLifetime applicationLifetime) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
    }
}
