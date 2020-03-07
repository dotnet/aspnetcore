// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication
{
    public partial class AccessDeniedContext : Microsoft.AspNetCore.Authentication.HandleRequestContext<Microsoft.AspNetCore.Authentication.RemoteAuthenticationOptions>
    {
        public AccessDeniedContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.RemoteAuthenticationOptions options) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.RemoteAuthenticationOptions)) { }
        public Microsoft.AspNetCore.Http.PathString AccessDeniedPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Authentication.AuthenticationProperties Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ReturnUrl { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ReturnUrlParameter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class AuthenticationBuilder
    {
        public AuthenticationBuilder(Microsoft.Extensions.DependencyInjection.IServiceCollection services) { }
        public virtual Microsoft.Extensions.DependencyInjection.IServiceCollection Services { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddPolicyScheme(string authenticationScheme, string displayName, System.Action<Microsoft.AspNetCore.Authentication.PolicySchemeOptions> configureOptions) { throw null; }
        public virtual Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddRemoteScheme<TOptions, THandler>(string authenticationScheme, string displayName, System.Action<TOptions> configureOptions) where TOptions : Microsoft.AspNetCore.Authentication.RemoteAuthenticationOptions, new() where THandler : Microsoft.AspNetCore.Authentication.RemoteAuthenticationHandler<TOptions> { throw null; }
        public virtual Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddScheme<TOptions, THandler>(string authenticationScheme, System.Action<TOptions> configureOptions) where TOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, new() where THandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<TOptions> { throw null; }
        public virtual Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddScheme<TOptions, THandler>(string authenticationScheme, string displayName, System.Action<TOptions> configureOptions) where TOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, new() where THandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<TOptions> { throw null; }
    }
    public abstract partial class AuthenticationHandler<TOptions> : Microsoft.AspNetCore.Authentication.IAuthenticationHandler where TOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, new()
    {
        protected AuthenticationHandler(Microsoft.Extensions.Options.IOptionsMonitor<TOptions> options, Microsoft.Extensions.Logging.ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder, Microsoft.AspNetCore.Authentication.ISystemClock clock) { }
        protected virtual string ClaimsIssuer { get { throw null; } }
        protected Microsoft.AspNetCore.Authentication.ISystemClock Clock { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected Microsoft.AspNetCore.Http.HttpContext Context { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected string CurrentUri { get { throw null; } }
        protected virtual object Events { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        protected Microsoft.Extensions.Logging.ILogger Logger { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public TOptions Options { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected Microsoft.Extensions.Options.IOptionsMonitor<TOptions> OptionsMonitor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected Microsoft.AspNetCore.Http.PathString OriginalPath { get { throw null; } }
        protected Microsoft.AspNetCore.Http.PathString OriginalPathBase { get { throw null; } }
        protected Microsoft.AspNetCore.Http.HttpRequest Request { get { throw null; } }
        protected Microsoft.AspNetCore.Http.HttpResponse Response { get { throw null; } }
        public Microsoft.AspNetCore.Authentication.AuthenticationScheme Scheme { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected System.Text.Encodings.Web.UrlEncoder UrlEncoder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> AuthenticateAsync() { throw null; }
        protected string BuildRedirectUri(string targetPath) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task ChallengeAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        protected virtual System.Threading.Tasks.Task<object> CreateEventsAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task ForbidAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        protected abstract System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync();
        protected System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateOnceAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateOnceSafeAsync() { throw null; }
        protected virtual System.Threading.Tasks.Task HandleChallengeAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        protected virtual System.Threading.Tasks.Task HandleForbiddenAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task InitializeAsync(Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected virtual System.Threading.Tasks.Task InitializeEventsAsync() { throw null; }
        protected virtual System.Threading.Tasks.Task InitializeHandlerAsync() { throw null; }
        protected virtual string ResolveTarget(string scheme) { throw null; }
    }
    public partial class AuthenticationMiddleware
    {
        public AuthenticationMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider schemes) { }
        public Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider Schemes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
    public partial class AuthenticationSchemeOptions
    {
        public AuthenticationSchemeOptions() { }
        public string ClaimsIssuer { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public object Events { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Type EventsType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ForwardAuthenticate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ForwardChallenge { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ForwardDefault { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Func<Microsoft.AspNetCore.Http.HttpContext, string> ForwardDefaultSelector { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ForwardForbid { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ForwardSignIn { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ForwardSignOut { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual void Validate() { }
        public virtual void Validate(string scheme) { }
    }
    public static partial class Base64UrlTextEncoder
    {
        public static byte[] Decode(string text) { throw null; }
        public static string Encode(byte[] data) { throw null; }
    }
    public abstract partial class BaseContext<TOptions> where TOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions
    {
        protected BaseContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, TOptions options) { }
        public Microsoft.AspNetCore.Http.HttpContext HttpContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public TOptions Options { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Http.HttpRequest Request { get { throw null; } }
        public Microsoft.AspNetCore.Http.HttpResponse Response { get { throw null; } }
        public Microsoft.AspNetCore.Authentication.AuthenticationScheme Scheme { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class HandleRequestContext<TOptions> : Microsoft.AspNetCore.Authentication.BaseContext<TOptions> where TOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions
    {
        protected HandleRequestContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, TOptions options) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(TOptions)) { }
        public Microsoft.AspNetCore.Authentication.HandleRequestResult Result { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] protected set { } }
        public void HandleResponse() { }
        public void SkipHandler() { }
    }
    public partial class HandleRequestResult : Microsoft.AspNetCore.Authentication.AuthenticateResult
    {
        public HandleRequestResult() { }
        public bool Handled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool Skipped { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public static new Microsoft.AspNetCore.Authentication.HandleRequestResult Fail(System.Exception failure) { throw null; }
        public static new Microsoft.AspNetCore.Authentication.HandleRequestResult Fail(System.Exception failure, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        public static new Microsoft.AspNetCore.Authentication.HandleRequestResult Fail(string failureMessage) { throw null; }
        public static new Microsoft.AspNetCore.Authentication.HandleRequestResult Fail(string failureMessage, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        public static Microsoft.AspNetCore.Authentication.HandleRequestResult Handle() { throw null; }
        public static new Microsoft.AspNetCore.Authentication.HandleRequestResult NoResult() { throw null; }
        public static Microsoft.AspNetCore.Authentication.HandleRequestResult SkipHandler() { throw null; }
        public static new Microsoft.AspNetCore.Authentication.HandleRequestResult Success(Microsoft.AspNetCore.Authentication.AuthenticationTicket ticket) { throw null; }
    }
    public partial interface IDataSerializer<TModel>
    {
        TModel Deserialize(byte[] data);
        byte[] Serialize(TModel model);
    }
    public partial interface ISecureDataFormat<TData>
    {
        string Protect(TData data);
        string Protect(TData data, string purpose);
        TData Unprotect(string protectedText);
        TData Unprotect(string protectedText, string purpose);
    }
    public partial interface ISystemClock
    {
        System.DateTimeOffset UtcNow { get; }
    }
    public static partial class JsonDocumentAuthExtensions
    {
        public static string GetString(this System.Text.Json.JsonElement element, string key) { throw null; }
    }
    public partial class PolicySchemeHandler : Microsoft.AspNetCore.Authentication.SignInAuthenticationHandler<Microsoft.AspNetCore.Authentication.PolicySchemeOptions>
    {
        public PolicySchemeHandler(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.PolicySchemeOptions> options, Microsoft.Extensions.Logging.ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder, Microsoft.AspNetCore.Authentication.ISystemClock clock) : base (default(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.PolicySchemeOptions>), default(Microsoft.Extensions.Logging.ILoggerFactory), default(System.Text.Encodings.Web.UrlEncoder), default(Microsoft.AspNetCore.Authentication.ISystemClock)) { }
        protected override System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync() { throw null; }
        protected override System.Threading.Tasks.Task HandleChallengeAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        protected override System.Threading.Tasks.Task HandleForbiddenAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        protected override System.Threading.Tasks.Task HandleSignInAsync(System.Security.Claims.ClaimsPrincipal user, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        protected override System.Threading.Tasks.Task HandleSignOutAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
    }
    public partial class PolicySchemeOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions
    {
        public PolicySchemeOptions() { }
    }
    public abstract partial class PrincipalContext<TOptions> : Microsoft.AspNetCore.Authentication.PropertiesContext<TOptions> where TOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions
    {
        protected PrincipalContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, TOptions options, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(TOptions), default(Microsoft.AspNetCore.Authentication.AuthenticationProperties)) { }
        public virtual System.Security.Claims.ClaimsPrincipal Principal { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public abstract partial class PropertiesContext<TOptions> : Microsoft.AspNetCore.Authentication.BaseContext<TOptions> where TOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions
    {
        protected PropertiesContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, TOptions options, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(TOptions)) { }
        public virtual Microsoft.AspNetCore.Authentication.AuthenticationProperties Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] protected set { } }
    }
    public partial class PropertiesDataFormat : Microsoft.AspNetCore.Authentication.SecureDataFormat<Microsoft.AspNetCore.Authentication.AuthenticationProperties>
    {
        public PropertiesDataFormat(Microsoft.AspNetCore.DataProtection.IDataProtector protector) : base (default(Microsoft.AspNetCore.Authentication.IDataSerializer<Microsoft.AspNetCore.Authentication.AuthenticationProperties>), default(Microsoft.AspNetCore.DataProtection.IDataProtector)) { }
    }
    public partial class PropertiesSerializer : Microsoft.AspNetCore.Authentication.IDataSerializer<Microsoft.AspNetCore.Authentication.AuthenticationProperties>
    {
        public PropertiesSerializer() { }
        public static Microsoft.AspNetCore.Authentication.PropertiesSerializer Default { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual Microsoft.AspNetCore.Authentication.AuthenticationProperties Deserialize(byte[] data) { throw null; }
        public virtual Microsoft.AspNetCore.Authentication.AuthenticationProperties Read(System.IO.BinaryReader reader) { throw null; }
        public virtual byte[] Serialize(Microsoft.AspNetCore.Authentication.AuthenticationProperties model) { throw null; }
        public virtual void Write(System.IO.BinaryWriter writer, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { }
    }
    public partial class RedirectContext<TOptions> : Microsoft.AspNetCore.Authentication.PropertiesContext<TOptions> where TOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions
    {
        public RedirectContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, TOptions options, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, string redirectUri) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(TOptions), default(Microsoft.AspNetCore.Authentication.AuthenticationProperties)) { }
        public string RedirectUri { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public abstract partial class RemoteAuthenticationContext<TOptions> : Microsoft.AspNetCore.Authentication.HandleRequestContext<TOptions> where TOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions
    {
        protected RemoteAuthenticationContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, TOptions options, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(TOptions)) { }
        public System.Security.Claims.ClaimsPrincipal Principal { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual Microsoft.AspNetCore.Authentication.AuthenticationProperties Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public void Fail(System.Exception failure) { }
        public void Fail(string failureMessage) { }
        public void Success() { }
    }
    public partial class RemoteAuthenticationEvents
    {
        public RemoteAuthenticationEvents() { }
        public System.Func<Microsoft.AspNetCore.Authentication.AccessDeniedContext, System.Threading.Tasks.Task> OnAccessDenied { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.RemoteFailureContext, System.Threading.Tasks.Task> OnRemoteFailure { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.TicketReceivedContext, System.Threading.Tasks.Task> OnTicketReceived { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual System.Threading.Tasks.Task AccessDenied(Microsoft.AspNetCore.Authentication.AccessDeniedContext context) { throw null; }
        public virtual System.Threading.Tasks.Task RemoteFailure(Microsoft.AspNetCore.Authentication.RemoteFailureContext context) { throw null; }
        public virtual System.Threading.Tasks.Task TicketReceived(Microsoft.AspNetCore.Authentication.TicketReceivedContext context) { throw null; }
    }
    public abstract partial class RemoteAuthenticationHandler<TOptions> : Microsoft.AspNetCore.Authentication.AuthenticationHandler<TOptions>, Microsoft.AspNetCore.Authentication.IAuthenticationHandler, Microsoft.AspNetCore.Authentication.IAuthenticationRequestHandler where TOptions : Microsoft.AspNetCore.Authentication.RemoteAuthenticationOptions, new()
    {
        protected RemoteAuthenticationHandler(Microsoft.Extensions.Options.IOptionsMonitor<TOptions> options, Microsoft.Extensions.Logging.ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder, Microsoft.AspNetCore.Authentication.ISystemClock clock) : base (default(Microsoft.Extensions.Options.IOptionsMonitor<TOptions>), default(Microsoft.Extensions.Logging.ILoggerFactory), default(System.Text.Encodings.Web.UrlEncoder), default(Microsoft.AspNetCore.Authentication.ISystemClock)) { }
        protected new Microsoft.AspNetCore.Authentication.RemoteAuthenticationEvents Events { get { throw null; } set { } }
        protected string SignInScheme { get { throw null; } }
        protected override System.Threading.Tasks.Task<object> CreateEventsAsync() { throw null; }
        protected virtual void GenerateCorrelationId(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.HandleRequestResult> HandleAccessDeniedErrorAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync() { throw null; }
        protected override System.Threading.Tasks.Task HandleForbiddenAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        protected abstract System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.HandleRequestResult> HandleRemoteAuthenticateAsync();
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<bool> HandleRequestAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<bool> ShouldHandleRequestAsync() { throw null; }
        protected virtual bool ValidateCorrelationId(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
    }
    public partial class RemoteAuthenticationOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions
    {
        public RemoteAuthenticationOptions() { }
        public Microsoft.AspNetCore.Http.PathString AccessDeniedPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Net.Http.HttpClient Backchannel { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Net.Http.HttpMessageHandler BackchannelHttpHandler { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.TimeSpan BackchannelTimeout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Http.PathString CallbackPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Http.CookieBuilder CorrelationCookie { get { throw null; } set { } }
        public Microsoft.AspNetCore.DataProtection.IDataProtectionProvider DataProtectionProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public new Microsoft.AspNetCore.Authentication.RemoteAuthenticationEvents Events { get { throw null; } set { } }
        public System.TimeSpan RemoteAuthenticationTimeout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ReturnUrlParameter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool SaveTokens { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string SignInScheme { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override void Validate() { }
        public override void Validate(string scheme) { }
    }
    public partial class RemoteFailureContext : Microsoft.AspNetCore.Authentication.HandleRequestContext<Microsoft.AspNetCore.Authentication.RemoteAuthenticationOptions>
    {
        public RemoteFailureContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.RemoteAuthenticationOptions options, System.Exception failure) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.RemoteAuthenticationOptions)) { }
        public System.Exception Failure { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Authentication.AuthenticationProperties Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class RequestPathBaseCookieBuilder : Microsoft.AspNetCore.Http.CookieBuilder
    {
        public RequestPathBaseCookieBuilder() { }
        protected virtual string AdditionalPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public override Microsoft.AspNetCore.Http.CookieOptions Build(Microsoft.AspNetCore.Http.HttpContext context, System.DateTimeOffset expiresFrom) { throw null; }
    }
    public abstract partial class ResultContext<TOptions> : Microsoft.AspNetCore.Authentication.BaseContext<TOptions> where TOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions
    {
        protected ResultContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, TOptions options) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(TOptions)) { }
        public System.Security.Claims.ClaimsPrincipal Principal { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Authentication.AuthenticationProperties Properties { get { throw null; } set { } }
        public Microsoft.AspNetCore.Authentication.AuthenticateResult Result { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public void Fail(System.Exception failure) { }
        public void Fail(string failureMessage) { }
        public void NoResult() { }
        public void Success() { }
    }
    public partial class SecureDataFormat<TData> : Microsoft.AspNetCore.Authentication.ISecureDataFormat<TData>
    {
        public SecureDataFormat(Microsoft.AspNetCore.Authentication.IDataSerializer<TData> serializer, Microsoft.AspNetCore.DataProtection.IDataProtector protector) { }
        public string Protect(TData data) { throw null; }
        public string Protect(TData data, string purpose) { throw null; }
        public TData Unprotect(string protectedText) { throw null; }
        public TData Unprotect(string protectedText, string purpose) { throw null; }
    }
    public abstract partial class SignInAuthenticationHandler<TOptions> : Microsoft.AspNetCore.Authentication.SignOutAuthenticationHandler<TOptions>, Microsoft.AspNetCore.Authentication.IAuthenticationHandler, Microsoft.AspNetCore.Authentication.IAuthenticationSignInHandler, Microsoft.AspNetCore.Authentication.IAuthenticationSignOutHandler where TOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, new()
    {
        public SignInAuthenticationHandler(Microsoft.Extensions.Options.IOptionsMonitor<TOptions> options, Microsoft.Extensions.Logging.ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder, Microsoft.AspNetCore.Authentication.ISystemClock clock) : base (default(Microsoft.Extensions.Options.IOptionsMonitor<TOptions>), default(Microsoft.Extensions.Logging.ILoggerFactory), default(System.Text.Encodings.Web.UrlEncoder), default(Microsoft.AspNetCore.Authentication.ISystemClock)) { }
        protected abstract System.Threading.Tasks.Task HandleSignInAsync(System.Security.Claims.ClaimsPrincipal user, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties);
        public virtual System.Threading.Tasks.Task SignInAsync(System.Security.Claims.ClaimsPrincipal user, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
    }
    public abstract partial class SignOutAuthenticationHandler<TOptions> : Microsoft.AspNetCore.Authentication.AuthenticationHandler<TOptions>, Microsoft.AspNetCore.Authentication.IAuthenticationHandler, Microsoft.AspNetCore.Authentication.IAuthenticationSignOutHandler where TOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, new()
    {
        public SignOutAuthenticationHandler(Microsoft.Extensions.Options.IOptionsMonitor<TOptions> options, Microsoft.Extensions.Logging.ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder, Microsoft.AspNetCore.Authentication.ISystemClock clock) : base (default(Microsoft.Extensions.Options.IOptionsMonitor<TOptions>), default(Microsoft.Extensions.Logging.ILoggerFactory), default(System.Text.Encodings.Web.UrlEncoder), default(Microsoft.AspNetCore.Authentication.ISystemClock)) { }
        protected abstract System.Threading.Tasks.Task HandleSignOutAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties);
        public virtual System.Threading.Tasks.Task SignOutAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
    }
    public partial class SystemClock : Microsoft.AspNetCore.Authentication.ISystemClock
    {
        public SystemClock() { }
        public System.DateTimeOffset UtcNow { get { throw null; } }
    }
    public partial class TicketDataFormat : Microsoft.AspNetCore.Authentication.SecureDataFormat<Microsoft.AspNetCore.Authentication.AuthenticationTicket>
    {
        public TicketDataFormat(Microsoft.AspNetCore.DataProtection.IDataProtector protector) : base (default(Microsoft.AspNetCore.Authentication.IDataSerializer<Microsoft.AspNetCore.Authentication.AuthenticationTicket>), default(Microsoft.AspNetCore.DataProtection.IDataProtector)) { }
    }
    public partial class TicketReceivedContext : Microsoft.AspNetCore.Authentication.RemoteAuthenticationContext<Microsoft.AspNetCore.Authentication.RemoteAuthenticationOptions>
    {
        public TicketReceivedContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.RemoteAuthenticationOptions options, Microsoft.AspNetCore.Authentication.AuthenticationTicket ticket) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.RemoteAuthenticationOptions), default(Microsoft.AspNetCore.Authentication.AuthenticationProperties)) { }
        public string ReturnUri { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class TicketSerializer : Microsoft.AspNetCore.Authentication.IDataSerializer<Microsoft.AspNetCore.Authentication.AuthenticationTicket>
    {
        public TicketSerializer() { }
        public static Microsoft.AspNetCore.Authentication.TicketSerializer Default { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual Microsoft.AspNetCore.Authentication.AuthenticationTicket Deserialize(byte[] data) { throw null; }
        public virtual Microsoft.AspNetCore.Authentication.AuthenticationTicket Read(System.IO.BinaryReader reader) { throw null; }
        protected virtual System.Security.Claims.Claim ReadClaim(System.IO.BinaryReader reader, System.Security.Claims.ClaimsIdentity identity) { throw null; }
        protected virtual System.Security.Claims.ClaimsIdentity ReadIdentity(System.IO.BinaryReader reader) { throw null; }
        public virtual byte[] Serialize(Microsoft.AspNetCore.Authentication.AuthenticationTicket ticket) { throw null; }
        public virtual void Write(System.IO.BinaryWriter writer, Microsoft.AspNetCore.Authentication.AuthenticationTicket ticket) { }
        protected virtual void WriteClaim(System.IO.BinaryWriter writer, System.Security.Claims.Claim claim) { }
        protected virtual void WriteIdentity(System.IO.BinaryWriter writer, System.Security.Claims.ClaimsIdentity identity) { }
    }
}
namespace Microsoft.AspNetCore.Builder
{
    public static partial class AuthAppBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseAuthentication(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class AuthenticationServiceCollectionExtensions
    {
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddAuthentication(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddAuthentication(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.Authentication.AuthenticationOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddAuthentication(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string defaultScheme) { throw null; }
    }
}
