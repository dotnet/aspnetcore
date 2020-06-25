// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authorization
{
    public partial class AuthorizationMiddleware
    {
        public AuthorizationMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider policyProvider) { }
        public AuthorizationMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider policyProvider, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Authorization.AuthorizationMiddlewareOptions> options) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
    public partial class AuthorizationMiddlewareOptions
    {
        public AuthorizationMiddlewareOptions() { }
        public bool UseHttpContextAsResource  { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }    
    public partial interface IAuthorizationMiddlewareResultHandler
    {
        System.Threading.Tasks.Task HandleAsync(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authorization.AuthorizationPolicy policy, Microsoft.AspNetCore.Authorization.Policy.PolicyAuthorizationResult authorizeResult);
    }
}
namespace Microsoft.AspNetCore.Authorization.Policy
{
    public partial class AuthorizationMiddlewareResultHandler : Microsoft.AspNetCore.Authorization.IAuthorizationMiddlewareResultHandler
    {
        public AuthorizationMiddlewareResultHandler() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task HandleAsync(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authorization.AuthorizationPolicy policy, Microsoft.AspNetCore.Authorization.Policy.PolicyAuthorizationResult authorizeResult) { throw null; }
    }
    public partial interface IPolicyEvaluator
    {
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> AuthenticateAsync(Microsoft.AspNetCore.Authorization.AuthorizationPolicy policy, Microsoft.AspNetCore.Http.HttpContext context);
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Authorization.Policy.PolicyAuthorizationResult> AuthorizeAsync(Microsoft.AspNetCore.Authorization.AuthorizationPolicy policy, Microsoft.AspNetCore.Authentication.AuthenticateResult authenticationResult, Microsoft.AspNetCore.Http.HttpContext context, object? resource);
    }
    public partial class PolicyAuthorizationResult
    {
        internal PolicyAuthorizationResult() { }
        public Microsoft.AspNetCore.Authorization.AuthorizationFailure? AuthorizationFailure { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool Challenged { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool Forbidden { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool Succeeded { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public static Microsoft.AspNetCore.Authorization.Policy.PolicyAuthorizationResult Challenge() { throw null; }
        public static Microsoft.AspNetCore.Authorization.Policy.PolicyAuthorizationResult Forbid() { throw null; }
        public static Microsoft.AspNetCore.Authorization.Policy.PolicyAuthorizationResult Forbid(Microsoft.AspNetCore.Authorization.AuthorizationFailure? authorizationFailure) { throw null; }
        public static Microsoft.AspNetCore.Authorization.Policy.PolicyAuthorizationResult Success() { throw null; }
    }
    public partial class PolicyEvaluator : Microsoft.AspNetCore.Authorization.Policy.IPolicyEvaluator
    {
        public PolicyEvaluator(Microsoft.AspNetCore.Authorization.IAuthorizationService authorization) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> AuthenticateAsync(Microsoft.AspNetCore.Authorization.AuthorizationPolicy policy, Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Authorization.Policy.PolicyAuthorizationResult> AuthorizeAsync(Microsoft.AspNetCore.Authorization.AuthorizationPolicy policy, Microsoft.AspNetCore.Authentication.AuthenticateResult authenticationResult, Microsoft.AspNetCore.Http.HttpContext context, object? resource) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Builder
{
    public static partial class AuthorizationAppBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseAuthorization(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
    }
    public static partial class AuthorizationEndpointConventionBuilderExtensions
    {
        public static TBuilder AllowAnonymous<TBuilder>(this TBuilder builder) where TBuilder : Microsoft.AspNetCore.Builder.IEndpointConventionBuilder { throw null; }
        public static TBuilder RequireAuthorization<TBuilder>(this TBuilder builder) where TBuilder : Microsoft.AspNetCore.Builder.IEndpointConventionBuilder { throw null; }
        public static TBuilder RequireAuthorization<TBuilder>(this TBuilder builder, params Microsoft.AspNetCore.Authorization.IAuthorizeData[] authorizeData) where TBuilder : Microsoft.AspNetCore.Builder.IEndpointConventionBuilder { throw null; }
        public static TBuilder RequireAuthorization<TBuilder>(this TBuilder builder, params string[] policyNames) where TBuilder : Microsoft.AspNetCore.Builder.IEndpointConventionBuilder { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class PolicyServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAuthorization(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAuthorization(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.Authorization.AuthorizationOptions> configure) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAuthorizationPolicyEvaluator(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
    }
}
