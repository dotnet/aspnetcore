// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication.JwtBearer
{
    public partial class AuthenticationFailedContext : Microsoft.AspNetCore.Authentication.ResultContext<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>
    {
        public AuthenticationFailedContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions options) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions)) { }
        public System.Exception Exception { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class ForbiddenContext : Microsoft.AspNetCore.Authentication.ResultContext<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>
    {
        public ForbiddenContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions options) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions)) { }
    }
    public partial class JwtBearerChallengeContext : Microsoft.AspNetCore.Authentication.PropertiesContext<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>
    {
        public JwtBearerChallengeContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions options, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions), default(Microsoft.AspNetCore.Authentication.AuthenticationProperties)) { }
        public System.Exception AuthenticateFailure { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Error { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ErrorDescription { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ErrorUri { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool Handled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void HandleResponse() { }
    }
    public static partial class JwtBearerDefaults
    {
        public const string AuthenticationScheme = "Bearer";
    }
    public partial class JwtBearerEvents
    {
        public JwtBearerEvents() { }
        public System.Func<Microsoft.AspNetCore.Authentication.JwtBearer.AuthenticationFailedContext, System.Threading.Tasks.Task> OnAuthenticationFailed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerChallengeContext, System.Threading.Tasks.Task> OnChallenge { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.JwtBearer.ForbiddenContext, System.Threading.Tasks.Task> OnForbidden { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.JwtBearer.MessageReceivedContext, System.Threading.Tasks.Task> OnMessageReceived { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.JwtBearer.TokenValidatedContext, System.Threading.Tasks.Task> OnTokenValidated { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task AuthenticationFailed(Microsoft.AspNetCore.Authentication.JwtBearer.AuthenticationFailedContext context) { throw null; }
        public virtual System.Threading.Tasks.Task Challenge(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerChallengeContext context) { throw null; }
        public virtual System.Threading.Tasks.Task Forbidden(Microsoft.AspNetCore.Authentication.JwtBearer.ForbiddenContext context) { throw null; }
        public virtual System.Threading.Tasks.Task MessageReceived(Microsoft.AspNetCore.Authentication.JwtBearer.MessageReceivedContext context) { throw null; }
        public virtual System.Threading.Tasks.Task TokenValidated(Microsoft.AspNetCore.Authentication.JwtBearer.TokenValidatedContext context) { throw null; }
    }
    public partial class JwtBearerHandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>
    {
        public JwtBearerHandler(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions> options, Microsoft.Extensions.Logging.ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder, Microsoft.AspNetCore.Authentication.ISystemClock clock) : base (default(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>), default(Microsoft.Extensions.Logging.ILoggerFactory), default(System.Text.Encodings.Web.UrlEncoder), default(Microsoft.AspNetCore.Authentication.ISystemClock)) { }
        protected new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents Events { get { throw null; } set { } }
        protected override System.Threading.Tasks.Task<object> CreateEventsAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task HandleChallengeAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        protected override System.Threading.Tasks.Task HandleForbiddenAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
    }
    public partial class JwtBearerOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions
    {
        public JwtBearerOptions() { }
        public string Audience { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Authority { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Net.Http.HttpMessageHandler BackchannelHttpHandler { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.TimeSpan BackchannelTimeout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Challenge { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration Configuration { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.IdentityModel.Protocols.IConfigurationManager<Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration> ConfigurationManager { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents Events { get { throw null; } set { } }
        public bool IncludeErrorDetails { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string MetadataAddress { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool RefreshOnIssuerKeyNotFound { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool RequireHttpsMetadata { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool SaveToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IList<Microsoft.IdentityModel.Tokens.ISecurityTokenValidator> SecurityTokenValidators { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.IdentityModel.Tokens.TokenValidationParameters TokenValidationParameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class JwtBearerPostConfigureOptions : Microsoft.Extensions.Options.IPostConfigureOptions<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>
    {
        public JwtBearerPostConfigureOptions() { }
        public void PostConfigure(string name, Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions options) { }
    }
    public partial class MessageReceivedContext : Microsoft.AspNetCore.Authentication.ResultContext<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>
    {
        public MessageReceivedContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions options) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions)) { }
        public string Token { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class TokenValidatedContext : Microsoft.AspNetCore.Authentication.ResultContext<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>
    {
        public TokenValidatedContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions options) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions)) { }
        public Microsoft.IdentityModel.Tokens.SecurityToken SecurityToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class JwtBearerExtensions
    {
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddJwtBearer(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddJwtBearer(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, System.Action<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddJwtBearer(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string authenticationScheme, System.Action<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddJwtBearer(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string authenticationScheme, string displayName, System.Action<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions> configureOptions) { throw null; }
    }
}
