// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication
{
    public partial class AuthenticateResult
    {
        protected AuthenticateResult() { }
        public System.Exception Failure { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] protected set { } }
        public bool None { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] protected set { } }
        public System.Security.Claims.ClaimsPrincipal Principal { get { throw null; } }
        public Microsoft.AspNetCore.Authentication.AuthenticationProperties Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] protected set { } }
        public bool Succeeded { get { throw null; } }
        public Microsoft.AspNetCore.Authentication.AuthenticationTicket Ticket { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] protected set { } }
        public static Microsoft.AspNetCore.Authentication.AuthenticateResult Fail(System.Exception failure) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticateResult Fail(System.Exception failure, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticateResult Fail(string failureMessage) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticateResult Fail(string failureMessage, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticateResult NoResult() { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticateResult Success(Microsoft.AspNetCore.Authentication.AuthenticationTicket ticket) { throw null; }
    }
    public static partial class AuthenticationHttpContextExtensions
    {
        public static System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> AuthenticateAsync(this Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
        public static System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> AuthenticateAsync(this Microsoft.AspNetCore.Http.HttpContext context, string scheme) { throw null; }
        public static System.Threading.Tasks.Task ChallengeAsync(this Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
        public static System.Threading.Tasks.Task ChallengeAsync(this Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        public static System.Threading.Tasks.Task ChallengeAsync(this Microsoft.AspNetCore.Http.HttpContext context, string scheme) { throw null; }
        public static System.Threading.Tasks.Task ChallengeAsync(this Microsoft.AspNetCore.Http.HttpContext context, string scheme, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        public static System.Threading.Tasks.Task ForbidAsync(this Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
        public static System.Threading.Tasks.Task ForbidAsync(this Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        public static System.Threading.Tasks.Task ForbidAsync(this Microsoft.AspNetCore.Http.HttpContext context, string scheme) { throw null; }
        public static System.Threading.Tasks.Task ForbidAsync(this Microsoft.AspNetCore.Http.HttpContext context, string scheme, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        public static System.Threading.Tasks.Task<string> GetTokenAsync(this Microsoft.AspNetCore.Http.HttpContext context, string tokenName) { throw null; }
        public static System.Threading.Tasks.Task<string> GetTokenAsync(this Microsoft.AspNetCore.Http.HttpContext context, string scheme, string tokenName) { throw null; }
        public static System.Threading.Tasks.Task SignInAsync(this Microsoft.AspNetCore.Http.HttpContext context, System.Security.Claims.ClaimsPrincipal principal) { throw null; }
        public static System.Threading.Tasks.Task SignInAsync(this Microsoft.AspNetCore.Http.HttpContext context, System.Security.Claims.ClaimsPrincipal principal, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        public static System.Threading.Tasks.Task SignInAsync(this Microsoft.AspNetCore.Http.HttpContext context, string scheme, System.Security.Claims.ClaimsPrincipal principal) { throw null; }
        public static System.Threading.Tasks.Task SignInAsync(this Microsoft.AspNetCore.Http.HttpContext context, string scheme, System.Security.Claims.ClaimsPrincipal principal, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        public static System.Threading.Tasks.Task SignOutAsync(this Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
        public static System.Threading.Tasks.Task SignOutAsync(this Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        public static System.Threading.Tasks.Task SignOutAsync(this Microsoft.AspNetCore.Http.HttpContext context, string scheme) { throw null; }
        public static System.Threading.Tasks.Task SignOutAsync(this Microsoft.AspNetCore.Http.HttpContext context, string scheme, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
    }
    public partial class AuthenticationOptions
    {
        public AuthenticationOptions() { }
        public string DefaultAuthenticateScheme { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string DefaultChallengeScheme { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string DefaultForbidScheme { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string DefaultScheme { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string DefaultSignInScheme { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string DefaultSignOutScheme { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool RequireAuthenticatedSignIn { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IDictionary<string, Microsoft.AspNetCore.Authentication.AuthenticationSchemeBuilder> SchemeMap { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authentication.AuthenticationSchemeBuilder> Schemes { get { throw null; } }
        public void AddScheme(string name, System.Action<Microsoft.AspNetCore.Authentication.AuthenticationSchemeBuilder> configureBuilder) { }
        public void AddScheme<THandler>(string name, string displayName) where THandler : Microsoft.AspNetCore.Authentication.IAuthenticationHandler { }
    }
    public partial class AuthenticationProperties
    {
        public AuthenticationProperties() { }
        public AuthenticationProperties(System.Collections.Generic.IDictionary<string, string> items) { }
        public AuthenticationProperties(System.Collections.Generic.IDictionary<string, string> items, System.Collections.Generic.IDictionary<string, object> parameters) { }
        public bool? AllowRefresh { get { throw null; } set { } }
        public System.DateTimeOffset? ExpiresUtc { get { throw null; } set { } }
        public bool IsPersistent { get { throw null; } set { } }
        public System.DateTimeOffset? IssuedUtc { get { throw null; } set { } }
        public System.Collections.Generic.IDictionary<string, string> Items { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IDictionary<string, object> Parameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string RedirectUri { get { throw null; } set { } }
        protected bool? GetBool(string key) { throw null; }
        protected System.DateTimeOffset? GetDateTimeOffset(string key) { throw null; }
        public T GetParameter<T>(string key) { throw null; }
        public string GetString(string key) { throw null; }
        protected void SetBool(string key, bool? value) { }
        protected void SetDateTimeOffset(string key, System.DateTimeOffset? value) { }
        public void SetParameter<T>(string key, T value) { }
        public void SetString(string key, string value) { }
    }
    public partial class AuthenticationScheme
    {
        public AuthenticationScheme(string name, string displayName, System.Type handlerType) { }
        public string DisplayName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Type HandlerType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class AuthenticationSchemeBuilder
    {
        public AuthenticationSchemeBuilder(string name) { }
        public string DisplayName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Type HandlerType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Authentication.AuthenticationScheme Build() { throw null; }
    }
    public partial class AuthenticationTicket
    {
        public AuthenticationTicket(System.Security.Claims.ClaimsPrincipal principal, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, string authenticationScheme) { }
        public AuthenticationTicket(System.Security.Claims.ClaimsPrincipal principal, string authenticationScheme) { }
        public string AuthenticationScheme { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Security.Claims.ClaimsPrincipal Principal { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Authentication.AuthenticationProperties Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class AuthenticationToken
    {
        public AuthenticationToken() { }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public static partial class AuthenticationTokenExtensions
    {
        public static System.Threading.Tasks.Task<string> GetTokenAsync(this Microsoft.AspNetCore.Authentication.IAuthenticationService auth, Microsoft.AspNetCore.Http.HttpContext context, string tokenName) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.Task<string> GetTokenAsync(this Microsoft.AspNetCore.Authentication.IAuthenticationService auth, Microsoft.AspNetCore.Http.HttpContext context, string scheme, string tokenName) { throw null; }
        public static System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authentication.AuthenticationToken> GetTokens(this Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        public static string GetTokenValue(this Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, string tokenName) { throw null; }
        public static void StoreTokens(this Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authentication.AuthenticationToken> tokens) { }
        public static bool UpdateTokenValue(this Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, string tokenName, string tokenValue) { throw null; }
    }
    public partial interface IAuthenticationFeature
    {
        Microsoft.AspNetCore.Http.PathString OriginalPath { get; set; }
        Microsoft.AspNetCore.Http.PathString OriginalPathBase { get; set; }
    }
    public partial interface IAuthenticationHandler
    {
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> AuthenticateAsync();
        System.Threading.Tasks.Task ChallengeAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties);
        System.Threading.Tasks.Task ForbidAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties);
        System.Threading.Tasks.Task InitializeAsync(Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Http.HttpContext context);
    }
    public partial interface IAuthenticationHandlerProvider
    {
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.IAuthenticationHandler> GetHandlerAsync(Microsoft.AspNetCore.Http.HttpContext context, string authenticationScheme);
    }
    public partial interface IAuthenticationRequestHandler : Microsoft.AspNetCore.Authentication.IAuthenticationHandler
    {
        System.Threading.Tasks.Task<bool> HandleRequestAsync();
    }
    public partial interface IAuthenticationSchemeProvider
    {
        void AddScheme(Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme);
        System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authentication.AuthenticationScheme>> GetAllSchemesAsync();
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticationScheme> GetDefaultAuthenticateSchemeAsync();
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticationScheme> GetDefaultChallengeSchemeAsync();
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticationScheme> GetDefaultForbidSchemeAsync();
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticationScheme> GetDefaultSignInSchemeAsync();
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticationScheme> GetDefaultSignOutSchemeAsync();
        System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authentication.AuthenticationScheme>> GetRequestHandlerSchemesAsync();
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticationScheme> GetSchemeAsync(string name);
        void RemoveScheme(string name);
        bool TryAddScheme(Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme) { throw null; }
    }
    public partial interface IAuthenticationService
    {
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> AuthenticateAsync(Microsoft.AspNetCore.Http.HttpContext context, string scheme);
        System.Threading.Tasks.Task ChallengeAsync(Microsoft.AspNetCore.Http.HttpContext context, string scheme, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties);
        System.Threading.Tasks.Task ForbidAsync(Microsoft.AspNetCore.Http.HttpContext context, string scheme, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties);
        System.Threading.Tasks.Task SignInAsync(Microsoft.AspNetCore.Http.HttpContext context, string scheme, System.Security.Claims.ClaimsPrincipal principal, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties);
        System.Threading.Tasks.Task SignOutAsync(Microsoft.AspNetCore.Http.HttpContext context, string scheme, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties);
    }
    public partial interface IAuthenticationSignInHandler : Microsoft.AspNetCore.Authentication.IAuthenticationHandler, Microsoft.AspNetCore.Authentication.IAuthenticationSignOutHandler
    {
        System.Threading.Tasks.Task SignInAsync(System.Security.Claims.ClaimsPrincipal user, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties);
    }
    public partial interface IAuthenticationSignOutHandler : Microsoft.AspNetCore.Authentication.IAuthenticationHandler
    {
        System.Threading.Tasks.Task SignOutAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties);
    }
    public partial interface IClaimsTransformation
    {
        System.Threading.Tasks.Task<System.Security.Claims.ClaimsPrincipal> TransformAsync(System.Security.Claims.ClaimsPrincipal principal);
    }
}
