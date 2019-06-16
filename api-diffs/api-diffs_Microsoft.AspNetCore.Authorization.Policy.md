# Microsoft.AspNetCore.Authorization.Policy

``` diff
 namespace Microsoft.AspNetCore.Authorization.Policy {
     public interface IPolicyEvaluator {
         Task<AuthenticateResult> AuthenticateAsync(AuthorizationPolicy policy, HttpContext context);
         Task<PolicyAuthorizationResult> AuthorizeAsync(AuthorizationPolicy policy, AuthenticateResult authenticationResult, HttpContext context, object resource);
     }
     public class PolicyAuthorizationResult {
         public bool Challenged { get; private set; }
         public bool Forbidden { get; private set; }
         public bool Succeeded { get; private set; }
         public static PolicyAuthorizationResult Challenge();
         public static PolicyAuthorizationResult Forbid();
         public static PolicyAuthorizationResult Success();
     }
     public class PolicyEvaluator : IPolicyEvaluator {
         public PolicyEvaluator(IAuthorizationService authorization);
         public virtual Task<AuthenticateResult> AuthenticateAsync(AuthorizationPolicy policy, HttpContext context);
         public virtual Task<PolicyAuthorizationResult> AuthorizeAsync(AuthorizationPolicy policy, AuthenticateResult authenticationResult, HttpContext context, object resource);
     }
 }
```

