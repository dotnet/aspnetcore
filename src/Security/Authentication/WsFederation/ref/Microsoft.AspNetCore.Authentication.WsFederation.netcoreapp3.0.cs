// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication.WsFederation
{
    public partial class AuthenticationFailedContext : Microsoft.AspNetCore.Authentication.RemoteAuthenticationContext<Microsoft.AspNetCore.Authentication.WsFederation.WsFederationOptions>
    {
        public AuthenticationFailedContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.WsFederation.WsFederationOptions options) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.WsFederation.WsFederationOptions), default(Microsoft.AspNetCore.Authentication.AuthenticationProperties)) { }
        public System.Exception Exception { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.IdentityModel.Protocols.WsFederation.WsFederationMessage ProtocolMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class MessageReceivedContext : Microsoft.AspNetCore.Authentication.RemoteAuthenticationContext<Microsoft.AspNetCore.Authentication.WsFederation.WsFederationOptions>
    {
        public MessageReceivedContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.WsFederation.WsFederationOptions options, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.WsFederation.WsFederationOptions), default(Microsoft.AspNetCore.Authentication.AuthenticationProperties)) { }
        public Microsoft.IdentityModel.Protocols.WsFederation.WsFederationMessage ProtocolMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class RedirectContext : Microsoft.AspNetCore.Authentication.PropertiesContext<Microsoft.AspNetCore.Authentication.WsFederation.WsFederationOptions>
    {
        public RedirectContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.WsFederation.WsFederationOptions options, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.WsFederation.WsFederationOptions), default(Microsoft.AspNetCore.Authentication.AuthenticationProperties)) { }
        public bool Handled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.IdentityModel.Protocols.WsFederation.WsFederationMessage ProtocolMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public void HandleResponse() { }
    }
    public partial class RemoteSignOutContext : Microsoft.AspNetCore.Authentication.RemoteAuthenticationContext<Microsoft.AspNetCore.Authentication.WsFederation.WsFederationOptions>
    {
        public RemoteSignOutContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.WsFederation.WsFederationOptions options, Microsoft.IdentityModel.Protocols.WsFederation.WsFederationMessage message) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.WsFederation.WsFederationOptions), default(Microsoft.AspNetCore.Authentication.AuthenticationProperties)) { }
        public Microsoft.IdentityModel.Protocols.WsFederation.WsFederationMessage ProtocolMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class SecurityTokenReceivedContext : Microsoft.AspNetCore.Authentication.RemoteAuthenticationContext<Microsoft.AspNetCore.Authentication.WsFederation.WsFederationOptions>
    {
        public SecurityTokenReceivedContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.WsFederation.WsFederationOptions options, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.WsFederation.WsFederationOptions), default(Microsoft.AspNetCore.Authentication.AuthenticationProperties)) { }
        public Microsoft.IdentityModel.Protocols.WsFederation.WsFederationMessage ProtocolMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class SecurityTokenValidatedContext : Microsoft.AspNetCore.Authentication.RemoteAuthenticationContext<Microsoft.AspNetCore.Authentication.WsFederation.WsFederationOptions>
    {
        public SecurityTokenValidatedContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.WsFederation.WsFederationOptions options, System.Security.Claims.ClaimsPrincipal principal, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.WsFederation.WsFederationOptions), default(Microsoft.AspNetCore.Authentication.AuthenticationProperties)) { }
        public Microsoft.IdentityModel.Protocols.WsFederation.WsFederationMessage ProtocolMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.IdentityModel.Tokens.SecurityToken SecurityToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public static partial class WsFederationDefaults
    {
        public const string AuthenticationScheme = "WsFederation";
        public const string DisplayName = "WsFederation";
        public static readonly string UserstatePropertiesKey;
    }
    public partial class WsFederationEvents : Microsoft.AspNetCore.Authentication.RemoteAuthenticationEvents
    {
        public WsFederationEvents() { }
        public System.Func<Microsoft.AspNetCore.Authentication.WsFederation.AuthenticationFailedContext, System.Threading.Tasks.Task> OnAuthenticationFailed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.WsFederation.MessageReceivedContext, System.Threading.Tasks.Task> OnMessageReceived { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.WsFederation.RedirectContext, System.Threading.Tasks.Task> OnRedirectToIdentityProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.WsFederation.RemoteSignOutContext, System.Threading.Tasks.Task> OnRemoteSignOut { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.WsFederation.SecurityTokenReceivedContext, System.Threading.Tasks.Task> OnSecurityTokenReceived { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.WsFederation.SecurityTokenValidatedContext, System.Threading.Tasks.Task> OnSecurityTokenValidated { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task AuthenticationFailed(Microsoft.AspNetCore.Authentication.WsFederation.AuthenticationFailedContext context) { throw null; }
        public virtual System.Threading.Tasks.Task MessageReceived(Microsoft.AspNetCore.Authentication.WsFederation.MessageReceivedContext context) { throw null; }
        public virtual System.Threading.Tasks.Task RedirectToIdentityProvider(Microsoft.AspNetCore.Authentication.WsFederation.RedirectContext context) { throw null; }
        public virtual System.Threading.Tasks.Task RemoteSignOut(Microsoft.AspNetCore.Authentication.WsFederation.RemoteSignOutContext context) { throw null; }
        public virtual System.Threading.Tasks.Task SecurityTokenReceived(Microsoft.AspNetCore.Authentication.WsFederation.SecurityTokenReceivedContext context) { throw null; }
        public virtual System.Threading.Tasks.Task SecurityTokenValidated(Microsoft.AspNetCore.Authentication.WsFederation.SecurityTokenValidatedContext context) { throw null; }
    }
    public partial class WsFederationHandler : Microsoft.AspNetCore.Authentication.RemoteAuthenticationHandler<Microsoft.AspNetCore.Authentication.WsFederation.WsFederationOptions>, Microsoft.AspNetCore.Authentication.IAuthenticationHandler, Microsoft.AspNetCore.Authentication.IAuthenticationSignOutHandler
    {
        public WsFederationHandler(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.WsFederation.WsFederationOptions> options, Microsoft.Extensions.Logging.ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder, Microsoft.AspNetCore.Authentication.ISystemClock clock) : base (default(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.WsFederation.WsFederationOptions>), default(Microsoft.Extensions.Logging.ILoggerFactory), default(System.Text.Encodings.Web.UrlEncoder), default(Microsoft.AspNetCore.Authentication.ISystemClock)) { }
        protected new Microsoft.AspNetCore.Authentication.WsFederation.WsFederationEvents Events { get { throw null; } set { } }
        protected override System.Threading.Tasks.Task<object> CreateEventsAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task HandleChallengeAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.HandleRequestResult> HandleRemoteAuthenticateAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected virtual System.Threading.Tasks.Task<bool> HandleRemoteSignOutAsync() { throw null; }
        public override System.Threading.Tasks.Task<bool> HandleRequestAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task SignOutAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
    }
    public partial class WsFederationOptions : Microsoft.AspNetCore.Authentication.RemoteAuthenticationOptions
    {
        public WsFederationOptions() { }
        public bool AllowUnsolicitedLogins { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.IdentityModel.Protocols.WsFederation.WsFederationConfiguration Configuration { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.IdentityModel.Protocols.IConfigurationManager<Microsoft.IdentityModel.Protocols.WsFederation.WsFederationConfiguration> ConfigurationManager { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public new Microsoft.AspNetCore.Authentication.WsFederation.WsFederationEvents Events { get { throw null; } set { } }
        public string MetadataAddress { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool RefreshOnIssuerKeyNotFound { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.PathString RemoteSignOutPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool RequireHttpsMetadata { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new bool SaveTokens { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.ICollection<Microsoft.IdentityModel.Tokens.ISecurityTokenValidator> SecurityTokenHandlers { get { throw null; } set { } }
        public string SignOutScheme { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string SignOutWreply { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool SkipUnrecognizedRequests { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Authentication.ISecureDataFormat<Microsoft.AspNetCore.Authentication.AuthenticationProperties> StateDataFormat { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.IdentityModel.Tokens.TokenValidationParameters TokenValidationParameters { get { throw null; } set { } }
        public bool UseTokenLifetime { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Wreply { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Wtrealm { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override void Validate() { }
    }
    public partial class WsFederationPostConfigureOptions : Microsoft.Extensions.Options.IPostConfigureOptions<Microsoft.AspNetCore.Authentication.WsFederation.WsFederationOptions>
    {
        public WsFederationPostConfigureOptions(Microsoft.AspNetCore.DataProtection.IDataProtectionProvider dataProtection) { }
        public void PostConfigure(string name, Microsoft.AspNetCore.Authentication.WsFederation.WsFederationOptions options) { }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class WsFederationExtensions
    {
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddWsFederation(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddWsFederation(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, System.Action<Microsoft.AspNetCore.Authentication.WsFederation.WsFederationOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddWsFederation(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string authenticationScheme, System.Action<Microsoft.AspNetCore.Authentication.WsFederation.WsFederationOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddWsFederation(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string authenticationScheme, string displayName, System.Action<Microsoft.AspNetCore.Authentication.WsFederation.WsFederationOptions> configureOptions) { throw null; }
    }
}
