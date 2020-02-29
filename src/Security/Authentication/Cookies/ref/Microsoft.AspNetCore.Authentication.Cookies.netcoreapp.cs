// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication.Cookies
{
    public partial class ChunkingCookieManager : Microsoft.AspNetCore.Authentication.Cookies.ICookieManager
    {
        public const int DefaultChunkSize = 4050;
        public ChunkingCookieManager() { }
        public int? ChunkSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool ThrowForPartialCookies { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public void AppendResponseCookie(Microsoft.AspNetCore.Http.HttpContext context, string key, string value, Microsoft.AspNetCore.Http.CookieOptions options) { }
        public void DeleteCookie(Microsoft.AspNetCore.Http.HttpContext context, string key, Microsoft.AspNetCore.Http.CookieOptions options) { }
        public string GetRequestCookie(Microsoft.AspNetCore.Http.HttpContext context, string key) { throw null; }
    }
    public static partial class CookieAuthenticationDefaults
    {
        public static readonly Microsoft.AspNetCore.Http.PathString AccessDeniedPath;
        public const string AuthenticationScheme = "Cookies";
        public static readonly string CookiePrefix;
        public static readonly Microsoft.AspNetCore.Http.PathString LoginPath;
        public static readonly Microsoft.AspNetCore.Http.PathString LogoutPath;
        public static readonly string ReturnUrlParameter;
    }
    public partial class CookieAuthenticationEvents
    {
        public CookieAuthenticationEvents() { }
        public System.Func<Microsoft.AspNetCore.Authentication.RedirectContext<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>, System.Threading.Tasks.Task> OnRedirectToAccessDenied { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.RedirectContext<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>, System.Threading.Tasks.Task> OnRedirectToLogin { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.RedirectContext<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>, System.Threading.Tasks.Task> OnRedirectToLogout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.RedirectContext<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>, System.Threading.Tasks.Task> OnRedirectToReturnUrl { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.Cookies.CookieSignedInContext, System.Threading.Tasks.Task> OnSignedIn { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.Cookies.CookieSigningInContext, System.Threading.Tasks.Task> OnSigningIn { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.Cookies.CookieSigningOutContext, System.Threading.Tasks.Task> OnSigningOut { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.Cookies.CookieValidatePrincipalContext, System.Threading.Tasks.Task> OnValidatePrincipal { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual System.Threading.Tasks.Task RedirectToAccessDenied(Microsoft.AspNetCore.Authentication.RedirectContext<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions> context) { throw null; }
        public virtual System.Threading.Tasks.Task RedirectToLogin(Microsoft.AspNetCore.Authentication.RedirectContext<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions> context) { throw null; }
        public virtual System.Threading.Tasks.Task RedirectToLogout(Microsoft.AspNetCore.Authentication.RedirectContext<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions> context) { throw null; }
        public virtual System.Threading.Tasks.Task RedirectToReturnUrl(Microsoft.AspNetCore.Authentication.RedirectContext<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions> context) { throw null; }
        public virtual System.Threading.Tasks.Task SignedIn(Microsoft.AspNetCore.Authentication.Cookies.CookieSignedInContext context) { throw null; }
        public virtual System.Threading.Tasks.Task SigningIn(Microsoft.AspNetCore.Authentication.Cookies.CookieSigningInContext context) { throw null; }
        public virtual System.Threading.Tasks.Task SigningOut(Microsoft.AspNetCore.Authentication.Cookies.CookieSigningOutContext context) { throw null; }
        public virtual System.Threading.Tasks.Task ValidatePrincipal(Microsoft.AspNetCore.Authentication.Cookies.CookieValidatePrincipalContext context) { throw null; }
    }
    public partial class CookieAuthenticationHandler : Microsoft.AspNetCore.Authentication.SignInAuthenticationHandler<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>
    {
        public CookieAuthenticationHandler(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions> options, Microsoft.Extensions.Logging.ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder, Microsoft.AspNetCore.Authentication.ISystemClock clock) : base (default(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>), default(Microsoft.Extensions.Logging.ILoggerFactory), default(System.Text.Encodings.Web.UrlEncoder), default(Microsoft.AspNetCore.Authentication.ISystemClock)) { }
        protected new Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationEvents Events { get { throw null; } set { } }
        protected override System.Threading.Tasks.Task<object> CreateEventsAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected virtual System.Threading.Tasks.Task FinishResponseAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task HandleChallengeAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task HandleForbiddenAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task HandleSignInAsync(System.Security.Claims.ClaimsPrincipal user, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task HandleSignOutAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        protected override System.Threading.Tasks.Task InitializeHandlerAsync() { throw null; }
    }
    public partial class CookieAuthenticationOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions
    {
        public CookieAuthenticationOptions() { }
        public Microsoft.AspNetCore.Http.PathString AccessDeniedPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Http.CookieBuilder Cookie { get { throw null; } set { } }
        public Microsoft.AspNetCore.Authentication.Cookies.ICookieManager CookieManager { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.DataProtection.IDataProtectionProvider DataProtectionProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public new Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationEvents Events { get { throw null; } set { } }
        public System.TimeSpan ExpireTimeSpan { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Http.PathString LoginPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Http.PathString LogoutPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ReturnUrlParameter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Authentication.Cookies.ITicketStore SessionStore { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool SlidingExpiration { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Authentication.ISecureDataFormat<Microsoft.AspNetCore.Authentication.AuthenticationTicket> TicketDataFormat { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class CookieSignedInContext : Microsoft.AspNetCore.Authentication.PrincipalContext<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>
    {
        public CookieSignedInContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, System.Security.Claims.ClaimsPrincipal principal, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions options) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions), default(Microsoft.AspNetCore.Authentication.AuthenticationProperties)) { }
    }
    public partial class CookieSigningInContext : Microsoft.AspNetCore.Authentication.PrincipalContext<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>
    {
        public CookieSigningInContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions options, System.Security.Claims.ClaimsPrincipal principal, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, Microsoft.AspNetCore.Http.CookieOptions cookieOptions) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions), default(Microsoft.AspNetCore.Authentication.AuthenticationProperties)) { }
        public Microsoft.AspNetCore.Http.CookieOptions CookieOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class CookieSigningOutContext : Microsoft.AspNetCore.Authentication.PropertiesContext<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>
    {
        public CookieSigningOutContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions options, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, Microsoft.AspNetCore.Http.CookieOptions cookieOptions) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions), default(Microsoft.AspNetCore.Authentication.AuthenticationProperties)) { }
        public Microsoft.AspNetCore.Http.CookieOptions CookieOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class CookieValidatePrincipalContext : Microsoft.AspNetCore.Authentication.PrincipalContext<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>
    {
        public CookieValidatePrincipalContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions options, Microsoft.AspNetCore.Authentication.AuthenticationTicket ticket) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions), default(Microsoft.AspNetCore.Authentication.AuthenticationProperties)) { }
        public bool ShouldRenew { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public void RejectPrincipal() { }
        public void ReplacePrincipal(System.Security.Claims.ClaimsPrincipal principal) { }
    }
    public partial interface ICookieManager
    {
        void AppendResponseCookie(Microsoft.AspNetCore.Http.HttpContext context, string key, string value, Microsoft.AspNetCore.Http.CookieOptions options);
        void DeleteCookie(Microsoft.AspNetCore.Http.HttpContext context, string key, Microsoft.AspNetCore.Http.CookieOptions options);
        string GetRequestCookie(Microsoft.AspNetCore.Http.HttpContext context, string key);
    }
    public partial interface ITicketStore
    {
        System.Threading.Tasks.Task RemoveAsync(string key);
        System.Threading.Tasks.Task RenewAsync(string key, Microsoft.AspNetCore.Authentication.AuthenticationTicket ticket);
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticationTicket> RetrieveAsync(string key);
        System.Threading.Tasks.Task<string> StoreAsync(Microsoft.AspNetCore.Authentication.AuthenticationTicket ticket);
    }
    public partial class PostConfigureCookieAuthenticationOptions : Microsoft.Extensions.Options.IPostConfigureOptions<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>
    {
        public PostConfigureCookieAuthenticationOptions(Microsoft.AspNetCore.DataProtection.IDataProtectionProvider dataProtection) { }
        public void PostConfigure(string name, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions options) { }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class CookieExtensions
    {
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddCookie(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddCookie(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, System.Action<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddCookie(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string authenticationScheme) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddCookie(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string authenticationScheme, System.Action<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddCookie(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string authenticationScheme, string displayName, System.Action<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions> configureOptions) { throw null; }
    }
}
