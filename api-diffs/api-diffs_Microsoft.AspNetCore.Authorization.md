# Microsoft.AspNetCore.Authorization

``` diff
 namespace Microsoft.AspNetCore.Authorization {
     public class AllowAnonymousAttribute : Attribute, IAllowAnonymous {
         public AllowAnonymousAttribute();
     }
     public class AuthorizationFailure {
         public bool FailCalled { get; private set; }
         public IEnumerable<IAuthorizationRequirement> FailedRequirements { get; private set; }
         public static AuthorizationFailure ExplicitFail();
         public static AuthorizationFailure Failed(IEnumerable<IAuthorizationRequirement> failed);
     }
     public abstract class AuthorizationHandler<TRequirement> : IAuthorizationHandler where TRequirement : IAuthorizationRequirement {
         protected AuthorizationHandler();
         public virtual Task HandleAsync(AuthorizationHandlerContext context);
         protected abstract Task HandleRequirementAsync(AuthorizationHandlerContext context, TRequirement requirement);
     }
     public abstract class AuthorizationHandler<TRequirement, TResource> : IAuthorizationHandler where TRequirement : IAuthorizationRequirement {
         protected AuthorizationHandler();
         public virtual Task HandleAsync(AuthorizationHandlerContext context);
         protected abstract Task HandleRequirementAsync(AuthorizationHandlerContext context, TRequirement requirement, TResource resource);
     }
     public class AuthorizationHandlerContext {
         public AuthorizationHandlerContext(IEnumerable<IAuthorizationRequirement> requirements, ClaimsPrincipal user, object resource);
         public virtual bool HasFailed { get; }
         public virtual bool HasSucceeded { get; }
         public virtual IEnumerable<IAuthorizationRequirement> PendingRequirements { get; }
         public virtual IEnumerable<IAuthorizationRequirement> Requirements { get; }
         public virtual object Resource { get; }
         public virtual ClaimsPrincipal User { get; }
         public virtual void Fail();
         public virtual void Succeed(IAuthorizationRequirement requirement);
     }
+    public class AuthorizationMiddleware {
+        public AuthorizationMiddleware(RequestDelegate next, IAuthorizationPolicyProvider policyProvider);
+        public Task Invoke(HttpContext context);
+    }
     public class AuthorizationOptions {
         public AuthorizationOptions();
         public AuthorizationPolicy DefaultPolicy { get; set; }
+        public AuthorizationPolicy FallbackPolicy { get; set; }
         public bool InvokeHandlersAfterFailure { get; set; }
         public void AddPolicy(string name, AuthorizationPolicy policy);
         public void AddPolicy(string name, Action<AuthorizationPolicyBuilder> configurePolicy);
         public AuthorizationPolicy GetPolicy(string name);
     }
     public class AuthorizationPolicy {
         public AuthorizationPolicy(IEnumerable<IAuthorizationRequirement> requirements, IEnumerable<string> authenticationSchemes);
         public IReadOnlyList<string> AuthenticationSchemes { get; }
         public IReadOnlyList<IAuthorizationRequirement> Requirements { get; }
         public static AuthorizationPolicy Combine(params AuthorizationPolicy[] policies);
         public static AuthorizationPolicy Combine(IEnumerable<AuthorizationPolicy> policies);
         public static Task<AuthorizationPolicy> CombineAsync(IAuthorizationPolicyProvider policyProvider, IEnumerable<IAuthorizeData> authorizeData);
     }
     public class AuthorizationPolicyBuilder {
         public AuthorizationPolicyBuilder(AuthorizationPolicy policy);
         public AuthorizationPolicyBuilder(params string[] authenticationSchemes);
         public IList<string> AuthenticationSchemes { get; set; }
         public IList<IAuthorizationRequirement> Requirements { get; set; }
         public AuthorizationPolicyBuilder AddAuthenticationSchemes(params string[] schemes);
         public AuthorizationPolicyBuilder AddRequirements(params IAuthorizationRequirement[] requirements);
         public AuthorizationPolicy Build();
         public AuthorizationPolicyBuilder Combine(AuthorizationPolicy policy);
         public AuthorizationPolicyBuilder RequireAssertion(Func<AuthorizationHandlerContext, bool> handler);
         public AuthorizationPolicyBuilder RequireAssertion(Func<AuthorizationHandlerContext, Task<bool>> handler);
         public AuthorizationPolicyBuilder RequireAuthenticatedUser();
         public AuthorizationPolicyBuilder RequireClaim(string claimType);
-        public AuthorizationPolicyBuilder RequireClaim(string claimType, IEnumerable<string> requiredValues);
+        public AuthorizationPolicyBuilder RequireClaim(string claimType, IEnumerable<string> allowedValues);
-        public AuthorizationPolicyBuilder RequireClaim(string claimType, params string[] requiredValues);
+        public AuthorizationPolicyBuilder RequireClaim(string claimType, params string[] allowedValues);
         public AuthorizationPolicyBuilder RequireRole(IEnumerable<string> roles);
         public AuthorizationPolicyBuilder RequireRole(params string[] roles);
         public AuthorizationPolicyBuilder RequireUserName(string userName);
     }
     public class AuthorizationResult {
         public AuthorizationFailure Failure { get; private set; }
         public bool Succeeded { get; private set; }
         public static AuthorizationResult Failed();
         public static AuthorizationResult Failed(AuthorizationFailure failure);
         public static AuthorizationResult Success();
     }
     public static class AuthorizationServiceExtensions {
         public static Task<AuthorizationResult> AuthorizeAsync(this IAuthorizationService service, ClaimsPrincipal user, AuthorizationPolicy policy);
         public static Task<AuthorizationResult> AuthorizeAsync(this IAuthorizationService service, ClaimsPrincipal user, object resource, AuthorizationPolicy policy);
         public static Task<AuthorizationResult> AuthorizeAsync(this IAuthorizationService service, ClaimsPrincipal user, object resource, IAuthorizationRequirement requirement);
         public static Task<AuthorizationResult> AuthorizeAsync(this IAuthorizationService service, ClaimsPrincipal user, string policyName);
     }
     public class AuthorizeAttribute : Attribute, IAuthorizeData {
         public AuthorizeAttribute();
         public AuthorizeAttribute(string policy);
-        public string ActiveAuthenticationSchemes { get; set; }

         public string AuthenticationSchemes { get; set; }
         public string Policy { get; set; }
         public string Roles { get; set; }
     }
     public class DefaultAuthorizationEvaluator : IAuthorizationEvaluator {
         public DefaultAuthorizationEvaluator();
         public AuthorizationResult Evaluate(AuthorizationHandlerContext context);
     }
     public class DefaultAuthorizationHandlerContextFactory : IAuthorizationHandlerContextFactory {
         public DefaultAuthorizationHandlerContextFactory();
         public virtual AuthorizationHandlerContext CreateContext(IEnumerable<IAuthorizationRequirement> requirements, ClaimsPrincipal user, object resource);
     }
     public class DefaultAuthorizationHandlerProvider : IAuthorizationHandlerProvider {
         public DefaultAuthorizationHandlerProvider(IEnumerable<IAuthorizationHandler> handlers);
         public Task<IEnumerable<IAuthorizationHandler>> GetHandlersAsync(AuthorizationHandlerContext context);
     }
     public class DefaultAuthorizationPolicyProvider : IAuthorizationPolicyProvider {
         public DefaultAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options);
         public Task<AuthorizationPolicy> GetDefaultPolicyAsync();
+        public Task<AuthorizationPolicy> GetFallbackPolicyAsync();
         public virtual Task<AuthorizationPolicy> GetPolicyAsync(string policyName);
     }
     public class DefaultAuthorizationService : IAuthorizationService {
         public DefaultAuthorizationService(IAuthorizationPolicyProvider policyProvider, IAuthorizationHandlerProvider handlers, ILogger<DefaultAuthorizationService> logger, IAuthorizationHandlerContextFactory contextFactory, IAuthorizationEvaluator evaluator, IOptions<AuthorizationOptions> options);
         public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object resource, IEnumerable<IAuthorizationRequirement> requirements);
         public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object resource, string policyName);
     }
     public interface IAllowAnonymous
     public interface IAuthorizationEvaluator {
         AuthorizationResult Evaluate(AuthorizationHandlerContext context);
     }
     public interface IAuthorizationHandler {
         Task HandleAsync(AuthorizationHandlerContext context);
     }
     public interface IAuthorizationHandlerContextFactory {
         AuthorizationHandlerContext CreateContext(IEnumerable<IAuthorizationRequirement> requirements, ClaimsPrincipal user, object resource);
     }
     public interface IAuthorizationHandlerProvider {
         Task<IEnumerable<IAuthorizationHandler>> GetHandlersAsync(AuthorizationHandlerContext context);
     }
     public interface IAuthorizationPolicyProvider {
         Task<AuthorizationPolicy> GetDefaultPolicyAsync();
+        Task<AuthorizationPolicy> GetFallbackPolicyAsync();
         Task<AuthorizationPolicy> GetPolicyAsync(string policyName);
     }
     public interface IAuthorizationRequirement
     public interface IAuthorizationService {
         Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object resource, IEnumerable<IAuthorizationRequirement> requirements);
         Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object resource, string policyName);
     }
     public interface IAuthorizeData {
         string AuthenticationSchemes { get; set; }
         string Policy { get; set; }
         string Roles { get; set; }
     }
 }
```

