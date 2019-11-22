// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication
{
    public static partial class ClaimActionCollectionUniqueExtensions
    {
        public static void MapUniqueJsonKey(this Microsoft.AspNetCore.Authentication.OAuth.Claims.ClaimActionCollection collection, string claimType, string jsonKey) { }
        public static void MapUniqueJsonKey(this Microsoft.AspNetCore.Authentication.OAuth.Claims.ClaimActionCollection collection, string claimType, string jsonKey, string valueType) { }
    }
}
namespace Microsoft.AspNetCore.Authentication.OpenIdConnect
{
    public partial class AuthenticationFailedContext : Microsoft.AspNetCore.Authentication.RemoteAuthenticationContext<Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions>
    {
        public AuthenticationFailedContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions options) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions), default(Microsoft.AspNetCore.Authentication.AuthenticationProperties)) { }
        public System.Exception Exception { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage ProtocolMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class AuthorizationCodeReceivedContext : Microsoft.AspNetCore.Authentication.RemoteAuthenticationContext<Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions>
    {
        public AuthorizationCodeReceivedContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions options, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions), default(Microsoft.AspNetCore.Authentication.AuthenticationProperties)) { }
        public System.Net.Http.HttpClient Backchannel { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool HandledCodeRedemption { get { throw null; } }
        public System.IdentityModel.Tokens.Jwt.JwtSecurityToken JwtSecurityToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage ProtocolMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage TokenEndpointRequest { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage TokenEndpointResponse { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public void HandleCodeRedemption() { }
        public void HandleCodeRedemption(Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage tokenEndpointResponse) { }
        public void HandleCodeRedemption(string accessToken, string idToken) { }
    }
    public partial class MessageReceivedContext : Microsoft.AspNetCore.Authentication.RemoteAuthenticationContext<Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions>
    {
        public MessageReceivedContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions options, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions), default(Microsoft.AspNetCore.Authentication.AuthenticationProperties)) { }
        public Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage ProtocolMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Token { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class OpenIdConnectChallengeProperties : Microsoft.AspNetCore.Authentication.OAuth.OAuthChallengeProperties
    {
        public static readonly string MaxAgeKey;
        public static readonly string PromptKey;
        public OpenIdConnectChallengeProperties() { }
        public OpenIdConnectChallengeProperties(System.Collections.Generic.IDictionary<string, string> items) { }
        public OpenIdConnectChallengeProperties(System.Collections.Generic.IDictionary<string, string> items, System.Collections.Generic.IDictionary<string, object> parameters) { }
        public System.TimeSpan? MaxAge { get { throw null; } set { } }
        public string Prompt { get { throw null; } set { } }
    }
    public static partial class OpenIdConnectDefaults
    {
        public static readonly string AuthenticationPropertiesKey;
        public const string AuthenticationScheme = "OpenIdConnect";
        public static readonly string CookieNoncePrefix;
        public static readonly string DisplayName;
        public static readonly string RedirectUriForCodePropertiesKey;
        public static readonly string UserstatePropertiesKey;
    }
    public partial class OpenIdConnectEvents : Microsoft.AspNetCore.Authentication.RemoteAuthenticationEvents
    {
        public OpenIdConnectEvents() { }
        public System.Func<Microsoft.AspNetCore.Authentication.OpenIdConnect.AuthenticationFailedContext, System.Threading.Tasks.Task> OnAuthenticationFailed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.OpenIdConnect.AuthorizationCodeReceivedContext, System.Threading.Tasks.Task> OnAuthorizationCodeReceived { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.OpenIdConnect.MessageReceivedContext, System.Threading.Tasks.Task> OnMessageReceived { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.OpenIdConnect.RedirectContext, System.Threading.Tasks.Task> OnRedirectToIdentityProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.OpenIdConnect.RedirectContext, System.Threading.Tasks.Task> OnRedirectToIdentityProviderForSignOut { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.OpenIdConnect.RemoteSignOutContext, System.Threading.Tasks.Task> OnRemoteSignOut { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.OpenIdConnect.RemoteSignOutContext, System.Threading.Tasks.Task> OnSignedOutCallbackRedirect { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenResponseReceivedContext, System.Threading.Tasks.Task> OnTokenResponseReceived { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext, System.Threading.Tasks.Task> OnTokenValidated { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.OpenIdConnect.UserInformationReceivedContext, System.Threading.Tasks.Task> OnUserInformationReceived { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task AuthenticationFailed(Microsoft.AspNetCore.Authentication.OpenIdConnect.AuthenticationFailedContext context) { throw null; }
        public virtual System.Threading.Tasks.Task AuthorizationCodeReceived(Microsoft.AspNetCore.Authentication.OpenIdConnect.AuthorizationCodeReceivedContext context) { throw null; }
        public virtual System.Threading.Tasks.Task MessageReceived(Microsoft.AspNetCore.Authentication.OpenIdConnect.MessageReceivedContext context) { throw null; }
        public virtual System.Threading.Tasks.Task RedirectToIdentityProvider(Microsoft.AspNetCore.Authentication.OpenIdConnect.RedirectContext context) { throw null; }
        public virtual System.Threading.Tasks.Task RedirectToIdentityProviderForSignOut(Microsoft.AspNetCore.Authentication.OpenIdConnect.RedirectContext context) { throw null; }
        public virtual System.Threading.Tasks.Task RemoteSignOut(Microsoft.AspNetCore.Authentication.OpenIdConnect.RemoteSignOutContext context) { throw null; }
        public virtual System.Threading.Tasks.Task SignedOutCallbackRedirect(Microsoft.AspNetCore.Authentication.OpenIdConnect.RemoteSignOutContext context) { throw null; }
        public virtual System.Threading.Tasks.Task TokenResponseReceived(Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenResponseReceivedContext context) { throw null; }
        public virtual System.Threading.Tasks.Task TokenValidated(Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext context) { throw null; }
        public virtual System.Threading.Tasks.Task UserInformationReceived(Microsoft.AspNetCore.Authentication.OpenIdConnect.UserInformationReceivedContext context) { throw null; }
    }
    public partial class OpenIdConnectHandler : Microsoft.AspNetCore.Authentication.RemoteAuthenticationHandler<Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions>, Microsoft.AspNetCore.Authentication.IAuthenticationHandler, Microsoft.AspNetCore.Authentication.IAuthenticationSignOutHandler
    {
        public OpenIdConnectHandler(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions> options, Microsoft.Extensions.Logging.ILoggerFactory logger, System.Text.Encodings.Web.HtmlEncoder htmlEncoder, System.Text.Encodings.Web.UrlEncoder encoder, Microsoft.AspNetCore.Authentication.ISystemClock clock) : base (default(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions>), default(Microsoft.Extensions.Logging.ILoggerFactory), default(System.Text.Encodings.Web.UrlEncoder), default(Microsoft.AspNetCore.Authentication.ISystemClock)) { }
        protected System.Net.Http.HttpClient Backchannel { get { throw null; } }
        protected new Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectEvents Events { get { throw null; } set { } }
        protected System.Text.Encodings.Web.HtmlEncoder HtmlEncoder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        protected override System.Threading.Tasks.Task<object> CreateEventsAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.HandleRequestResult> GetUserInformationAsync(Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage message, System.IdentityModel.Tokens.Jwt.JwtSecurityToken jwt, System.Security.Claims.ClaimsPrincipal principal, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task HandleChallengeAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.HandleRequestResult> HandleRemoteAuthenticateAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected virtual System.Threading.Tasks.Task<bool> HandleRemoteSignOutAsync() { throw null; }
        public override System.Threading.Tasks.Task<bool> HandleRequestAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected virtual System.Threading.Tasks.Task<bool> HandleSignOutCallbackAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected virtual System.Threading.Tasks.Task<Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage> RedeemAuthorizationCodeAsync(Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage tokenEndpointRequest) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task SignOutAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
    }
    public partial class OpenIdConnectOptions : Microsoft.AspNetCore.Authentication.RemoteAuthenticationOptions
    {
        public OpenIdConnectOptions() { }
        public Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectRedirectBehavior AuthenticationMethod { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Authority { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Authentication.OAuth.Claims.ClaimActionCollection ClaimActions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string ClientId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ClientSecret { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration Configuration { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.IdentityModel.Protocols.IConfigurationManager<Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration> ConfigurationManager { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool DisableTelemetry { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public new Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectEvents Events { get { throw null; } set { } }
        public bool GetClaimsFromUserInfoEndpoint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.TimeSpan? MaxAge { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string MetadataAddress { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.CookieBuilder NonceCookie { get { throw null; } set { } }
        public string Prompt { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectProtocolValidator ProtocolValidator { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool RefreshOnIssuerKeyNotFound { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.PathString RemoteSignOutPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool RequireHttpsMetadata { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Resource { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ResponseMode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ResponseType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.ICollection<string> Scope { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.IdentityModel.Tokens.ISecurityTokenValidator SecurityTokenValidator { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.PathString SignedOutCallbackPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string SignedOutRedirectUri { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string SignOutScheme { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool SkipUnrecognizedRequests { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Authentication.ISecureDataFormat<Microsoft.AspNetCore.Authentication.AuthenticationProperties> StateDataFormat { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Authentication.ISecureDataFormat<string> StringDataFormat { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.IdentityModel.Tokens.TokenValidationParameters TokenValidationParameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool UsePkce { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool UseTokenLifetime { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override void Validate() { }
    }
    public partial class OpenIdConnectPostConfigureOptions : Microsoft.Extensions.Options.IPostConfigureOptions<Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions>
    {
        public OpenIdConnectPostConfigureOptions(Microsoft.AspNetCore.DataProtection.IDataProtectionProvider dataProtection) { }
        public void PostConfigure(string name, Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions options) { }
    }
    public enum OpenIdConnectRedirectBehavior
    {
        RedirectGet = 0,
        FormPost = 1,
    }
    public partial class RedirectContext : Microsoft.AspNetCore.Authentication.PropertiesContext<Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions>
    {
        public RedirectContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions options, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions), default(Microsoft.AspNetCore.Authentication.AuthenticationProperties)) { }
        public bool Handled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage ProtocolMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public void HandleResponse() { }
    }
    public partial class RemoteSignOutContext : Microsoft.AspNetCore.Authentication.RemoteAuthenticationContext<Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions>
    {
        public RemoteSignOutContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions options, Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage message) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions), default(Microsoft.AspNetCore.Authentication.AuthenticationProperties)) { }
        public Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage ProtocolMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class TokenResponseReceivedContext : Microsoft.AspNetCore.Authentication.RemoteAuthenticationContext<Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions>
    {
        public TokenResponseReceivedContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions options, System.Security.Claims.ClaimsPrincipal user, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions), default(Microsoft.AspNetCore.Authentication.AuthenticationProperties)) { }
        public Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage ProtocolMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage TokenEndpointResponse { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class TokenValidatedContext : Microsoft.AspNetCore.Authentication.RemoteAuthenticationContext<Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions>
    {
        public TokenValidatedContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions options, System.Security.Claims.ClaimsPrincipal principal, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions), default(Microsoft.AspNetCore.Authentication.AuthenticationProperties)) { }
        public string Nonce { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage ProtocolMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.IdentityModel.Tokens.Jwt.JwtSecurityToken SecurityToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage TokenEndpointResponse { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class UserInformationReceivedContext : Microsoft.AspNetCore.Authentication.RemoteAuthenticationContext<Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions>
    {
        public UserInformationReceivedContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions options, System.Security.Claims.ClaimsPrincipal principal, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions), default(Microsoft.AspNetCore.Authentication.AuthenticationProperties)) { }
        public Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage ProtocolMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Text.Json.JsonDocument User { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace Microsoft.AspNetCore.Authentication.OpenIdConnect.Claims
{
    public partial class UniqueJsonKeyClaimAction : Microsoft.AspNetCore.Authentication.OAuth.Claims.JsonKeyClaimAction
    {
        public UniqueJsonKeyClaimAction(string claimType, string valueType, string jsonKey) : base (default(string), default(string), default(string)) { }
        public override void Run(System.Text.Json.JsonElement userData, System.Security.Claims.ClaimsIdentity identity, string issuer) { }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class OpenIdConnectExtensions
    {
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddOpenIdConnect(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddOpenIdConnect(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, System.Action<Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddOpenIdConnect(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string authenticationScheme, System.Action<Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddOpenIdConnect(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string authenticationScheme, string displayName, System.Action<Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions> configureOptions) { throw null; }
    }
}
