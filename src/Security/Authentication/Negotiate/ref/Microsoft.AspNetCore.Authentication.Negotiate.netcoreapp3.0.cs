// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication.Negotiate
{
    public partial class AuthenticatedContext : Microsoft.AspNetCore.Authentication.ResultContext<Microsoft.AspNetCore.Authentication.Negotiate.NegotiateOptions>
    {
        public AuthenticatedContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.Negotiate.NegotiateOptions options) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.Negotiate.NegotiateOptions)) { }
    }
    public partial class AuthenticationFailedContext : Microsoft.AspNetCore.Authentication.RemoteAuthenticationContext<Microsoft.AspNetCore.Authentication.Negotiate.NegotiateOptions>
    {
        public AuthenticationFailedContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.Negotiate.NegotiateOptions options) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.Negotiate.NegotiateOptions), default(Microsoft.AspNetCore.Authentication.AuthenticationProperties)) { }
        public System.Exception Exception { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class ChallengeContext : Microsoft.AspNetCore.Authentication.PropertiesContext<Microsoft.AspNetCore.Authentication.Negotiate.NegotiateOptions>
    {
        public ChallengeContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.Negotiate.NegotiateOptions options, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.Negotiate.NegotiateOptions), default(Microsoft.AspNetCore.Authentication.AuthenticationProperties)) { }
        public bool Handled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void HandleResponse() { }
    }
    public static partial class NegotiateDefaults
    {
        public const string AuthenticationScheme = "Negotiate";
    }
    public partial class NegotiateEvents
    {
        public NegotiateEvents() { }
        public System.Func<Microsoft.AspNetCore.Authentication.Negotiate.AuthenticatedContext, System.Threading.Tasks.Task> OnAuthenticated { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.Negotiate.AuthenticationFailedContext, System.Threading.Tasks.Task> OnAuthenticationFailed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.Negotiate.ChallengeContext, System.Threading.Tasks.Task> OnChallenge { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task Authenticated(Microsoft.AspNetCore.Authentication.Negotiate.AuthenticatedContext context) { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticationFailed(Microsoft.AspNetCore.Authentication.Negotiate.AuthenticationFailedContext context) { throw null; }
        public virtual System.Threading.Tasks.Task Challenge(Microsoft.AspNetCore.Authentication.Negotiate.ChallengeContext context) { throw null; }
    }
    public partial class NegotiateHandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<Microsoft.AspNetCore.Authentication.Negotiate.NegotiateOptions>, Microsoft.AspNetCore.Authentication.IAuthenticationHandler, Microsoft.AspNetCore.Authentication.IAuthenticationRequestHandler
    {
        public NegotiateHandler(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.Negotiate.NegotiateOptions> options, Microsoft.Extensions.Logging.ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder, Microsoft.AspNetCore.Authentication.ISystemClock clock) : base (default(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.Negotiate.NegotiateOptions>), default(Microsoft.Extensions.Logging.ILoggerFactory), default(System.Text.Encodings.Web.UrlEncoder), default(Microsoft.AspNetCore.Authentication.ISystemClock)) { }
        protected new Microsoft.AspNetCore.Authentication.Negotiate.NegotiateEvents Events { get { throw null; } set { } }
        protected override System.Threading.Tasks.Task<object> CreateEventsAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task HandleChallengeAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<bool> HandleRequestAsync() { throw null; }
    }
    public partial class NegotiateOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions
    {
        public NegotiateOptions() { }
        public new Microsoft.AspNetCore.Authentication.Negotiate.NegotiateEvents Events { get { throw null; } set { } }
        public bool PersistKerberosCredentials { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool PersistNtlmCredentials { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class PostConfigureNegotiateOptions : Microsoft.Extensions.Options.IPostConfigureOptions<Microsoft.AspNetCore.Authentication.Negotiate.NegotiateOptions>
    {
        public PostConfigureNegotiateOptions(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Hosting.Server.IServerIntegratedAuth> serverAuthServices, Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Authentication.Negotiate.NegotiateHandler> logger) { }
        public void PostConfigure(string name, Microsoft.AspNetCore.Authentication.Negotiate.NegotiateOptions options) { }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class NegotiateExtensions
    {
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddNegotiate(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddNegotiate(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, System.Action<Microsoft.AspNetCore.Authentication.Negotiate.NegotiateOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddNegotiate(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string authenticationScheme, System.Action<Microsoft.AspNetCore.Authentication.Negotiate.NegotiateOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddNegotiate(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string authenticationScheme, string displayName, System.Action<Microsoft.AspNetCore.Authentication.Negotiate.NegotiateOptions> configureOptions) { throw null; }
    }
}
