// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication
{
    public static partial class ClaimActionCollectionMapExtensions
    {
        public static void DeleteClaim(this Microsoft.AspNetCore.Authentication.OAuth.Claims.ClaimActionCollection collection, string claimType) { }
        public static void DeleteClaims(this Microsoft.AspNetCore.Authentication.OAuth.Claims.ClaimActionCollection collection, params string[] claimTypes) { }
        public static void MapAll(this Microsoft.AspNetCore.Authentication.OAuth.Claims.ClaimActionCollection collection) { }
        public static void MapAllExcept(this Microsoft.AspNetCore.Authentication.OAuth.Claims.ClaimActionCollection collection, params string[] exclusions) { }
        public static void MapCustomJson(this Microsoft.AspNetCore.Authentication.OAuth.Claims.ClaimActionCollection collection, string claimType, System.Func<System.Text.Json.JsonElement, string> resolver) { }
        public static void MapCustomJson(this Microsoft.AspNetCore.Authentication.OAuth.Claims.ClaimActionCollection collection, string claimType, string valueType, System.Func<System.Text.Json.JsonElement, string> resolver) { }
        public static void MapJsonKey(this Microsoft.AspNetCore.Authentication.OAuth.Claims.ClaimActionCollection collection, string claimType, string jsonKey) { }
        public static void MapJsonKey(this Microsoft.AspNetCore.Authentication.OAuth.Claims.ClaimActionCollection collection, string claimType, string jsonKey, string valueType) { }
        public static void MapJsonSubKey(this Microsoft.AspNetCore.Authentication.OAuth.Claims.ClaimActionCollection collection, string claimType, string jsonKey, string subKey) { }
        public static void MapJsonSubKey(this Microsoft.AspNetCore.Authentication.OAuth.Claims.ClaimActionCollection collection, string claimType, string jsonKey, string subKey, string valueType) { }
    }
}
namespace Microsoft.AspNetCore.Authentication.OAuth
{
    public partial class OAuthChallengeProperties : Microsoft.AspNetCore.Authentication.AuthenticationProperties
    {
        public static readonly string ScopeKey;
        public OAuthChallengeProperties() { }
        public OAuthChallengeProperties(System.Collections.Generic.IDictionary<string, string> items) { }
        public OAuthChallengeProperties(System.Collections.Generic.IDictionary<string, string> items, System.Collections.Generic.IDictionary<string, object> parameters) { }
        public System.Collections.Generic.ICollection<string> Scope { get { throw null; } set { } }
        public virtual void SetScope(params string[] scopes) { }
    }
    public partial class OAuthCodeExchangeContext
    {
        public OAuthCodeExchangeContext(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, string code, string redirectUri) { }
        public string Code { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Authentication.AuthenticationProperties Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string RedirectUri { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public static partial class OAuthConstants
    {
        public static readonly string CodeChallengeKey;
        public static readonly string CodeChallengeMethodKey;
        public static readonly string CodeChallengeMethodS256;
        public static readonly string CodeVerifierKey;
    }
    public partial class OAuthCreatingTicketContext : Microsoft.AspNetCore.Authentication.ResultContext<Microsoft.AspNetCore.Authentication.OAuth.OAuthOptions>
    {
        public OAuthCreatingTicketContext(System.Security.Claims.ClaimsPrincipal principal, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.OAuth.OAuthOptions options, System.Net.Http.HttpClient backchannel, Microsoft.AspNetCore.Authentication.OAuth.OAuthTokenResponse tokens, System.Text.Json.JsonElement user) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.OAuth.OAuthOptions)) { }
        public string AccessToken { get { throw null; } }
        public System.Net.Http.HttpClient Backchannel { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.TimeSpan? ExpiresIn { get { throw null; } }
        public System.Security.Claims.ClaimsIdentity Identity { get { throw null; } }
        public string RefreshToken { get { throw null; } }
        public Microsoft.AspNetCore.Authentication.OAuth.OAuthTokenResponse TokenResponse { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string TokenType { get { throw null; } }
        public System.Text.Json.JsonElement User { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public void RunClaimActions() { }
        public void RunClaimActions(System.Text.Json.JsonElement userData) { }
    }
    public static partial class OAuthDefaults
    {
        public static readonly string DisplayName;
    }
    public partial class OAuthEvents : Microsoft.AspNetCore.Authentication.RemoteAuthenticationEvents
    {
        public OAuthEvents() { }
        public System.Func<Microsoft.AspNetCore.Authentication.OAuth.OAuthCreatingTicketContext, System.Threading.Tasks.Task> OnCreatingTicket { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.RedirectContext<Microsoft.AspNetCore.Authentication.OAuth.OAuthOptions>, System.Threading.Tasks.Task> OnRedirectToAuthorizationEndpoint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual System.Threading.Tasks.Task CreatingTicket(Microsoft.AspNetCore.Authentication.OAuth.OAuthCreatingTicketContext context) { throw null; }
        public virtual System.Threading.Tasks.Task RedirectToAuthorizationEndpoint(Microsoft.AspNetCore.Authentication.RedirectContext<Microsoft.AspNetCore.Authentication.OAuth.OAuthOptions> context) { throw null; }
    }
    public partial class OAuthHandler<TOptions> : Microsoft.AspNetCore.Authentication.RemoteAuthenticationHandler<TOptions> where TOptions : Microsoft.AspNetCore.Authentication.OAuth.OAuthOptions, new()
    {
        public OAuthHandler(Microsoft.Extensions.Options.IOptionsMonitor<TOptions> options, Microsoft.Extensions.Logging.ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder, Microsoft.AspNetCore.Authentication.ISystemClock clock) : base (default(Microsoft.Extensions.Options.IOptionsMonitor<TOptions>), default(Microsoft.Extensions.Logging.ILoggerFactory), default(System.Text.Encodings.Web.UrlEncoder), default(Microsoft.AspNetCore.Authentication.ISystemClock)) { }
        protected System.Net.Http.HttpClient Backchannel { get { throw null; } }
        protected new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents Events { get { throw null; } set { } }
        protected virtual string BuildChallengeUrl(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, string redirectUri) { throw null; }
        protected override System.Threading.Tasks.Task<object> CreateEventsAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticationTicket> CreateTicketAsync(System.Security.Claims.ClaimsIdentity identity, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, Microsoft.AspNetCore.Authentication.OAuth.OAuthTokenResponse tokens) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.OAuth.OAuthTokenResponse> ExchangeCodeAsync(Microsoft.AspNetCore.Authentication.OAuth.OAuthCodeExchangeContext context) { throw null; }
        protected virtual string FormatScope() { throw null; }
        protected virtual string FormatScope(System.Collections.Generic.IEnumerable<string> scopes) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task HandleChallengeAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.HandleRequestResult> HandleRemoteAuthenticateAsync() { throw null; }
    }
    public partial class OAuthOptions : Microsoft.AspNetCore.Authentication.RemoteAuthenticationOptions
    {
        public OAuthOptions() { }
        public string AuthorizationEndpoint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Authentication.OAuth.Claims.ClaimActionCollection ClaimActions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string ClientId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ClientSecret { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents Events { get { throw null; } set { } }
        public System.Collections.Generic.ICollection<string> Scope { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Authentication.ISecureDataFormat<Microsoft.AspNetCore.Authentication.AuthenticationProperties> StateDataFormat { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string TokenEndpoint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool UsePkce { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string UserInformationEndpoint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override void Validate() { }
    }
    public partial class OAuthTokenResponse : System.IDisposable
    {
        internal OAuthTokenResponse() { }
        public string AccessToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Exception Error { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ExpiresIn { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string RefreshToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Text.Json.JsonDocument Response { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string TokenType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public void Dispose() { }
        public static Microsoft.AspNetCore.Authentication.OAuth.OAuthTokenResponse Failed(System.Exception error) { throw null; }
        public static Microsoft.AspNetCore.Authentication.OAuth.OAuthTokenResponse Success(System.Text.Json.JsonDocument response) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Authentication.OAuth.Claims
{
    public abstract partial class ClaimAction
    {
        public ClaimAction(string claimType, string valueType) { }
        public string ClaimType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string ValueType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public abstract void Run(System.Text.Json.JsonElement userData, System.Security.Claims.ClaimsIdentity identity, string issuer);
    }
    public partial class ClaimActionCollection : System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authentication.OAuth.Claims.ClaimAction>, System.Collections.IEnumerable
    {
        public ClaimActionCollection() { }
        public void Add(Microsoft.AspNetCore.Authentication.OAuth.Claims.ClaimAction action) { }
        public void Clear() { }
        public System.Collections.Generic.IEnumerator<Microsoft.AspNetCore.Authentication.OAuth.Claims.ClaimAction> GetEnumerator() { throw null; }
        public void Remove(string claimType) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public partial class CustomJsonClaimAction : Microsoft.AspNetCore.Authentication.OAuth.Claims.ClaimAction
    {
        public CustomJsonClaimAction(string claimType, string valueType, System.Func<System.Text.Json.JsonElement, string> resolver) : base (default(string), default(string)) { }
        public System.Func<System.Text.Json.JsonElement, string> Resolver { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public override void Run(System.Text.Json.JsonElement userData, System.Security.Claims.ClaimsIdentity identity, string issuer) { }
    }
    public partial class DeleteClaimAction : Microsoft.AspNetCore.Authentication.OAuth.Claims.ClaimAction
    {
        public DeleteClaimAction(string claimType) : base (default(string), default(string)) { }
        public override void Run(System.Text.Json.JsonElement userData, System.Security.Claims.ClaimsIdentity identity, string issuer) { }
    }
    public partial class JsonKeyClaimAction : Microsoft.AspNetCore.Authentication.OAuth.Claims.ClaimAction
    {
        public JsonKeyClaimAction(string claimType, string valueType, string jsonKey) : base (default(string), default(string)) { }
        public string JsonKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public override void Run(System.Text.Json.JsonElement userData, System.Security.Claims.ClaimsIdentity identity, string issuer) { }
    }
    public partial class JsonSubKeyClaimAction : Microsoft.AspNetCore.Authentication.OAuth.Claims.JsonKeyClaimAction
    {
        public JsonSubKeyClaimAction(string claimType, string valueType, string jsonKey, string subKey) : base (default(string), default(string), default(string)) { }
        public string SubKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public override void Run(System.Text.Json.JsonElement userData, System.Security.Claims.ClaimsIdentity identity, string issuer) { }
    }
    public partial class MapAllClaimsAction : Microsoft.AspNetCore.Authentication.OAuth.Claims.ClaimAction
    {
        public MapAllClaimsAction() : base (default(string), default(string)) { }
        public override void Run(System.Text.Json.JsonElement userData, System.Security.Claims.ClaimsIdentity identity, string issuer) { }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class OAuthExtensions
    {
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddOAuth(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string authenticationScheme, System.Action<Microsoft.AspNetCore.Authentication.OAuth.OAuthOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddOAuth(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string authenticationScheme, string displayName, System.Action<Microsoft.AspNetCore.Authentication.OAuth.OAuthOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddOAuth<TOptions, THandler>(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string authenticationScheme, System.Action<TOptions> configureOptions) where TOptions : Microsoft.AspNetCore.Authentication.OAuth.OAuthOptions, new() where THandler : Microsoft.AspNetCore.Authentication.OAuth.OAuthHandler<TOptions> { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddOAuth<TOptions, THandler>(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string authenticationScheme, string displayName, System.Action<TOptions> configureOptions) where TOptions : Microsoft.AspNetCore.Authentication.OAuth.OAuthOptions, new() where THandler : Microsoft.AspNetCore.Authentication.OAuth.OAuthHandler<TOptions> { throw null; }
    }
    public partial class OAuthPostConfigureOptions<TOptions, THandler> : Microsoft.Extensions.Options.IPostConfigureOptions<TOptions> where TOptions : Microsoft.AspNetCore.Authentication.OAuth.OAuthOptions, new() where THandler : Microsoft.AspNetCore.Authentication.OAuth.OAuthHandler<TOptions>
    {
        public OAuthPostConfigureOptions(Microsoft.AspNetCore.DataProtection.IDataProtectionProvider dataProtection) { }
        public void PostConfigure(string name, TOptions options) { }
    }
}
