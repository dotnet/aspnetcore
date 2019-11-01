// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication
{
    public partial class AuthenticationFeature : Microsoft.AspNetCore.Authentication.IAuthenticationFeature
    {
        public AuthenticationFeature() { }
        public Microsoft.AspNetCore.Http.PathString OriginalPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Http.PathString OriginalPathBase { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class AuthenticationHandlerProvider : Microsoft.AspNetCore.Authentication.IAuthenticationHandlerProvider
    {
        public AuthenticationHandlerProvider(Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider schemes) { }
        public Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider Schemes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.IAuthenticationHandler> GetHandlerAsync(Microsoft.AspNetCore.Http.HttpContext context, string authenticationScheme) { throw null; }
    }
    public partial class AuthenticationSchemeProvider : Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider
    {
        public AuthenticationSchemeProvider(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Authentication.AuthenticationOptions> options) { }
        protected AuthenticationSchemeProvider(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Authentication.AuthenticationOptions> options, System.Collections.Generic.IDictionary<string, Microsoft.AspNetCore.Authentication.AuthenticationScheme> schemes) { }
        public virtual void AddScheme(Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme) { }
        public virtual System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authentication.AuthenticationScheme>> GetAllSchemesAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticationScheme> GetDefaultAuthenticateSchemeAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticationScheme> GetDefaultChallengeSchemeAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticationScheme> GetDefaultForbidSchemeAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticationScheme> GetDefaultSignInSchemeAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticationScheme> GetDefaultSignOutSchemeAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authentication.AuthenticationScheme>> GetRequestHandlerSchemesAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticationScheme> GetSchemeAsync(string name) { throw null; }
        public virtual void RemoveScheme(string name) { }
        public virtual bool TryAddScheme(Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme) { throw null; }
    }
    public partial class AuthenticationService : Microsoft.AspNetCore.Authentication.IAuthenticationService
    {
        public AuthenticationService(Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider schemes, Microsoft.AspNetCore.Authentication.IAuthenticationHandlerProvider handlers, Microsoft.AspNetCore.Authentication.IClaimsTransformation transform, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Authentication.AuthenticationOptions> options) { }
        public Microsoft.AspNetCore.Authentication.IAuthenticationHandlerProvider Handlers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Authentication.AuthenticationOptions Options { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider Schemes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Authentication.IClaimsTransformation Transform { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> AuthenticateAsync(Microsoft.AspNetCore.Http.HttpContext context, string scheme) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task ChallengeAsync(Microsoft.AspNetCore.Http.HttpContext context, string scheme, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task ForbidAsync(Microsoft.AspNetCore.Http.HttpContext context, string scheme, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task SignInAsync(Microsoft.AspNetCore.Http.HttpContext context, string scheme, System.Security.Claims.ClaimsPrincipal principal, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task SignOutAsync(Microsoft.AspNetCore.Http.HttpContext context, string scheme, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
    }
    public partial class NoopClaimsTransformation : Microsoft.AspNetCore.Authentication.IClaimsTransformation
    {
        public NoopClaimsTransformation() { }
        public virtual System.Threading.Tasks.Task<System.Security.Claims.ClaimsPrincipal> TransformAsync(System.Security.Claims.ClaimsPrincipal principal) { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class AuthenticationCoreServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAuthenticationCore(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAuthenticationCore(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.Authentication.AuthenticationOptions> configureOptions) { throw null; }
    }
}
