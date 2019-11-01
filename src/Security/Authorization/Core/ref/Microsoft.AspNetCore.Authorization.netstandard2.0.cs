// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authorization
{
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
    public partial class AllowAnonymousAttribute : System.Attribute, Microsoft.AspNetCore.Authorization.IAllowAnonymous
    {
        public AllowAnonymousAttribute() { }
    }
    public partial class AuthorizationFailure
    {
        internal AuthorizationFailure() { }
        public bool FailCalled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authorization.IAuthorizationRequirement> FailedRequirements { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public static Microsoft.AspNetCore.Authorization.AuthorizationFailure ExplicitFail() { throw null; }
        public static Microsoft.AspNetCore.Authorization.AuthorizationFailure Failed(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authorization.IAuthorizationRequirement> failed) { throw null; }
    }
    public partial class AuthorizationHandlerContext
    {
        public AuthorizationHandlerContext(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authorization.IAuthorizationRequirement> requirements, System.Security.Claims.ClaimsPrincipal user, object resource) { }
        public virtual bool HasFailed { get { throw null; } }
        public virtual bool HasSucceeded { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authorization.IAuthorizationRequirement> PendingRequirements { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authorization.IAuthorizationRequirement> Requirements { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual object Resource { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual System.Security.Claims.ClaimsPrincipal User { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual void Fail() { }
        public virtual void Succeed(Microsoft.AspNetCore.Authorization.IAuthorizationRequirement requirement) { }
    }
    public abstract partial class AuthorizationHandler<TRequirement> : Microsoft.AspNetCore.Authorization.IAuthorizationHandler where TRequirement : Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
    {
        protected AuthorizationHandler() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task HandleAsync(Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext context) { throw null; }
        protected abstract System.Threading.Tasks.Task HandleRequirementAsync(Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext context, TRequirement requirement);
    }
    public abstract partial class AuthorizationHandler<TRequirement, TResource> : Microsoft.AspNetCore.Authorization.IAuthorizationHandler where TRequirement : Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
    {
        protected AuthorizationHandler() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task HandleAsync(Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext context) { throw null; }
        protected abstract System.Threading.Tasks.Task HandleRequirementAsync(Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext context, TRequirement requirement, TResource resource);
    }
    public partial class AuthorizationOptions
    {
        public AuthorizationOptions() { }
        public Microsoft.AspNetCore.Authorization.AuthorizationPolicy DefaultPolicy { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Authorization.AuthorizationPolicy FallbackPolicy { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool InvokeHandlersAfterFailure { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public void AddPolicy(string name, Microsoft.AspNetCore.Authorization.AuthorizationPolicy policy) { }
        public void AddPolicy(string name, System.Action<Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder> configurePolicy) { }
        public Microsoft.AspNetCore.Authorization.AuthorizationPolicy GetPolicy(string name) { throw null; }
    }
    public partial class AuthorizationPolicy
    {
        public AuthorizationPolicy(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authorization.IAuthorizationRequirement> requirements, System.Collections.Generic.IEnumerable<string> authenticationSchemes) { }
        public System.Collections.Generic.IReadOnlyList<string> AuthenticationSchemes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Authorization.IAuthorizationRequirement> Requirements { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public static Microsoft.AspNetCore.Authorization.AuthorizationPolicy Combine(params Microsoft.AspNetCore.Authorization.AuthorizationPolicy[] policies) { throw null; }
        public static Microsoft.AspNetCore.Authorization.AuthorizationPolicy Combine(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authorization.AuthorizationPolicy> policies) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.Task<Microsoft.AspNetCore.Authorization.AuthorizationPolicy> CombineAsync(Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider policyProvider, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authorization.IAuthorizeData> authorizeData) { throw null; }
    }
    public partial class AuthorizationPolicyBuilder
    {
        public AuthorizationPolicyBuilder(Microsoft.AspNetCore.Authorization.AuthorizationPolicy policy) { }
        public AuthorizationPolicyBuilder(params string[] authenticationSchemes) { }
        public System.Collections.Generic.IList<string> AuthenticationSchemes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Authorization.IAuthorizationRequirement> Requirements { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder AddAuthenticationSchemes(params string[] schemes) { throw null; }
        public Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder AddRequirements(params Microsoft.AspNetCore.Authorization.IAuthorizationRequirement[] requirements) { throw null; }
        public Microsoft.AspNetCore.Authorization.AuthorizationPolicy Build() { throw null; }
        public Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder Combine(Microsoft.AspNetCore.Authorization.AuthorizationPolicy policy) { throw null; }
        public Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder RequireAssertion(System.Func<Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext, bool> handler) { throw null; }
        public Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder RequireAssertion(System.Func<Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext, System.Threading.Tasks.Task<bool>> handler) { throw null; }
        public Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder RequireAuthenticatedUser() { throw null; }
        public Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder RequireClaim(string claimType) { throw null; }
        public Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder RequireClaim(string claimType, System.Collections.Generic.IEnumerable<string> allowedValues) { throw null; }
        public Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder RequireClaim(string claimType, params string[] allowedValues) { throw null; }
        public Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder RequireRole(System.Collections.Generic.IEnumerable<string> roles) { throw null; }
        public Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder RequireRole(params string[] roles) { throw null; }
        public Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder RequireUserName(string userName) { throw null; }
    }
    public partial class AuthorizationResult
    {
        internal AuthorizationResult() { }
        public Microsoft.AspNetCore.Authorization.AuthorizationFailure Failure { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool Succeeded { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public static Microsoft.AspNetCore.Authorization.AuthorizationResult Failed() { throw null; }
        public static Microsoft.AspNetCore.Authorization.AuthorizationResult Failed(Microsoft.AspNetCore.Authorization.AuthorizationFailure failure) { throw null; }
        public static Microsoft.AspNetCore.Authorization.AuthorizationResult Success() { throw null; }
    }
    public static partial class AuthorizationServiceExtensions
    {
        public static System.Threading.Tasks.Task<Microsoft.AspNetCore.Authorization.AuthorizationResult> AuthorizeAsync(this Microsoft.AspNetCore.Authorization.IAuthorizationService service, System.Security.Claims.ClaimsPrincipal user, Microsoft.AspNetCore.Authorization.AuthorizationPolicy policy) { throw null; }
        public static System.Threading.Tasks.Task<Microsoft.AspNetCore.Authorization.AuthorizationResult> AuthorizeAsync(this Microsoft.AspNetCore.Authorization.IAuthorizationService service, System.Security.Claims.ClaimsPrincipal user, object resource, Microsoft.AspNetCore.Authorization.AuthorizationPolicy policy) { throw null; }
        public static System.Threading.Tasks.Task<Microsoft.AspNetCore.Authorization.AuthorizationResult> AuthorizeAsync(this Microsoft.AspNetCore.Authorization.IAuthorizationService service, System.Security.Claims.ClaimsPrincipal user, object resource, Microsoft.AspNetCore.Authorization.IAuthorizationRequirement requirement) { throw null; }
        public static System.Threading.Tasks.Task<Microsoft.AspNetCore.Authorization.AuthorizationResult> AuthorizeAsync(this Microsoft.AspNetCore.Authorization.IAuthorizationService service, System.Security.Claims.ClaimsPrincipal user, string policyName) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=true, Inherited=true)]
    public partial class AuthorizeAttribute : System.Attribute, Microsoft.AspNetCore.Authorization.IAuthorizeData
    {
        public AuthorizeAttribute() { }
        public AuthorizeAttribute(string policy) { }
        public string AuthenticationSchemes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Policy { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Roles { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class DefaultAuthorizationEvaluator : Microsoft.AspNetCore.Authorization.IAuthorizationEvaluator
    {
        public DefaultAuthorizationEvaluator() { }
        public Microsoft.AspNetCore.Authorization.AuthorizationResult Evaluate(Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext context) { throw null; }
    }
    public partial class DefaultAuthorizationHandlerContextFactory : Microsoft.AspNetCore.Authorization.IAuthorizationHandlerContextFactory
    {
        public DefaultAuthorizationHandlerContextFactory() { }
        public virtual Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext CreateContext(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authorization.IAuthorizationRequirement> requirements, System.Security.Claims.ClaimsPrincipal user, object resource) { throw null; }
    }
    public partial class DefaultAuthorizationHandlerProvider : Microsoft.AspNetCore.Authorization.IAuthorizationHandlerProvider
    {
        public DefaultAuthorizationHandlerProvider(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authorization.IAuthorizationHandler> handlers) { }
        public System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authorization.IAuthorizationHandler>> GetHandlersAsync(Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext context) { throw null; }
    }
    public partial class DefaultAuthorizationPolicyProvider : Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider
    {
        public DefaultAuthorizationPolicyProvider(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Authorization.AuthorizationOptions> options) { }
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Authorization.AuthorizationPolicy> GetDefaultPolicyAsync() { throw null; }
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Authorization.AuthorizationPolicy> GetFallbackPolicyAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Authorization.AuthorizationPolicy> GetPolicyAsync(string policyName) { throw null; }
    }
    public partial class DefaultAuthorizationService : Microsoft.AspNetCore.Authorization.IAuthorizationService
    {
        public DefaultAuthorizationService(Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider policyProvider, Microsoft.AspNetCore.Authorization.IAuthorizationHandlerProvider handlers, Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Authorization.DefaultAuthorizationService> logger, Microsoft.AspNetCore.Authorization.IAuthorizationHandlerContextFactory contextFactory, Microsoft.AspNetCore.Authorization.IAuthorizationEvaluator evaluator, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Authorization.AuthorizationOptions> options) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Authorization.AuthorizationResult> AuthorizeAsync(System.Security.Claims.ClaimsPrincipal user, object resource, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authorization.IAuthorizationRequirement> requirements) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Authorization.AuthorizationResult> AuthorizeAsync(System.Security.Claims.ClaimsPrincipal user, object resource, string policyName) { throw null; }
    }
    public partial interface IAuthorizationEvaluator
    {
        Microsoft.AspNetCore.Authorization.AuthorizationResult Evaluate(Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext context);
    }
    public partial interface IAuthorizationHandler
    {
        System.Threading.Tasks.Task HandleAsync(Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext context);
    }
    public partial interface IAuthorizationHandlerContextFactory
    {
        Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext CreateContext(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authorization.IAuthorizationRequirement> requirements, System.Security.Claims.ClaimsPrincipal user, object resource);
    }
    public partial interface IAuthorizationHandlerProvider
    {
        System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authorization.IAuthorizationHandler>> GetHandlersAsync(Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext context);
    }
    public partial interface IAuthorizationPolicyProvider
    {
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Authorization.AuthorizationPolicy> GetDefaultPolicyAsync();
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Authorization.AuthorizationPolicy> GetFallbackPolicyAsync();
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Authorization.AuthorizationPolicy> GetPolicyAsync(string policyName);
    }
    public partial interface IAuthorizationRequirement
    {
    }
    public partial interface IAuthorizationService
    {
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Authorization.AuthorizationResult> AuthorizeAsync(System.Security.Claims.ClaimsPrincipal user, object resource, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authorization.IAuthorizationRequirement> requirements);
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Authorization.AuthorizationResult> AuthorizeAsync(System.Security.Claims.ClaimsPrincipal user, object resource, string policyName);
    }
}
namespace Microsoft.AspNetCore.Authorization.Infrastructure
{
    public partial class AssertionRequirement : Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
    {
        public AssertionRequirement(System.Func<Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext, bool> handler) { }
        public AssertionRequirement(System.Func<Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext, System.Threading.Tasks.Task<bool>> handler) { }
        public System.Func<Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext, System.Threading.Tasks.Task<bool>> Handler { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task HandleAsync(Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext context) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class ClaimsAuthorizationRequirement : Microsoft.AspNetCore.Authorization.AuthorizationHandler<Microsoft.AspNetCore.Authorization.Infrastructure.ClaimsAuthorizationRequirement>, Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
    {
        public ClaimsAuthorizationRequirement(string claimType, System.Collections.Generic.IEnumerable<string> allowedValues) { }
        public System.Collections.Generic.IEnumerable<string> AllowedValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string ClaimType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Threading.Tasks.Task HandleRequirementAsync(Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext context, Microsoft.AspNetCore.Authorization.Infrastructure.ClaimsAuthorizationRequirement requirement) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class DenyAnonymousAuthorizationRequirement : Microsoft.AspNetCore.Authorization.AuthorizationHandler<Microsoft.AspNetCore.Authorization.Infrastructure.DenyAnonymousAuthorizationRequirement>, Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
    {
        public DenyAnonymousAuthorizationRequirement() { }
        protected override System.Threading.Tasks.Task HandleRequirementAsync(Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext context, Microsoft.AspNetCore.Authorization.Infrastructure.DenyAnonymousAuthorizationRequirement requirement) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class NameAuthorizationRequirement : Microsoft.AspNetCore.Authorization.AuthorizationHandler<Microsoft.AspNetCore.Authorization.Infrastructure.NameAuthorizationRequirement>, Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
    {
        public NameAuthorizationRequirement(string requiredName) { }
        public string RequiredName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Threading.Tasks.Task HandleRequirementAsync(Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext context, Microsoft.AspNetCore.Authorization.Infrastructure.NameAuthorizationRequirement requirement) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class OperationAuthorizationRequirement : Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
    {
        public OperationAuthorizationRequirement() { }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override string ToString() { throw null; }
    }
    public partial class PassThroughAuthorizationHandler : Microsoft.AspNetCore.Authorization.IAuthorizationHandler
    {
        public PassThroughAuthorizationHandler() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task HandleAsync(Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext context) { throw null; }
    }
    public partial class RolesAuthorizationRequirement : Microsoft.AspNetCore.Authorization.AuthorizationHandler<Microsoft.AspNetCore.Authorization.Infrastructure.RolesAuthorizationRequirement>, Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
    {
        public RolesAuthorizationRequirement(System.Collections.Generic.IEnumerable<string> allowedRoles) { }
        public System.Collections.Generic.IEnumerable<string> AllowedRoles { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Threading.Tasks.Task HandleRequirementAsync(Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext context, Microsoft.AspNetCore.Authorization.Infrastructure.RolesAuthorizationRequirement requirement) { throw null; }
        public override string ToString() { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class AuthorizationServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAuthorizationCore(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAuthorizationCore(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.Authorization.AuthorizationOptions> configure) { throw null; }
    }
}
