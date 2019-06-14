# Microsoft.AspNetCore.Authorization

``` diff
 namespace Microsoft.AspNetCore.Authorization {
+    public class AuthorizationMiddleware {
+        public AuthorizationMiddleware(RequestDelegate next, IAuthorizationPolicyProvider policyProvider);
+        public Task Invoke(HttpContext context);
+    }
     public class AuthorizationOptions {
+        public AuthorizationPolicy FallbackPolicy { get; set; }
     }
     public class AuthorizationPolicyBuilder {
-        public AuthorizationPolicyBuilder RequireClaim(string claimType, IEnumerable<string> requiredValues);
+        public AuthorizationPolicyBuilder RequireClaim(string claimType, IEnumerable<string> allowedValues);
-        public AuthorizationPolicyBuilder RequireClaim(string claimType, params string[] requiredValues);
+        public AuthorizationPolicyBuilder RequireClaim(string claimType, params string[] allowedValues);
     }
     public class AuthorizeAttribute : Attribute, IAuthorizeData {
-        public string ActiveAuthenticationSchemes { get; set; }

     }
     public class DefaultAuthorizationPolicyProvider : IAuthorizationPolicyProvider {
+        public Task<AuthorizationPolicy> GetFallbackPolicyAsync();
     }
     public interface IAuthorizationPolicyProvider {
+        Task<AuthorizationPolicy> GetFallbackPolicyAsync();
     }
 }
```

