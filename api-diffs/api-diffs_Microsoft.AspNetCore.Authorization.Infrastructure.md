# Microsoft.AspNetCore.Authorization.Infrastructure

``` diff
 namespace Microsoft.AspNetCore.Authorization.Infrastructure {
     public class AssertionRequirement : IAuthorizationHandler, IAuthorizationRequirement {
         public AssertionRequirement(Func<AuthorizationHandlerContext, bool> handler);
         public AssertionRequirement(Func<AuthorizationHandlerContext, Task<bool>> handler);
         public Func<AuthorizationHandlerContext, Task<bool>> Handler { get; }
         public Task HandleAsync(AuthorizationHandlerContext context);
     }
     public class ClaimsAuthorizationRequirement : AuthorizationHandler<ClaimsAuthorizationRequirement>, IAuthorizationRequirement {
         public ClaimsAuthorizationRequirement(string claimType, IEnumerable<string> allowedValues);
         public IEnumerable<string> AllowedValues { get; }
         public string ClaimType { get; }
         protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ClaimsAuthorizationRequirement requirement);
     }
     public class DenyAnonymousAuthorizationRequirement : AuthorizationHandler<DenyAnonymousAuthorizationRequirement>, IAuthorizationRequirement {
         public DenyAnonymousAuthorizationRequirement();
         protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, DenyAnonymousAuthorizationRequirement requirement);
     }
     public class NameAuthorizationRequirement : AuthorizationHandler<NameAuthorizationRequirement>, IAuthorizationRequirement {
         public NameAuthorizationRequirement(string requiredName);
         public string RequiredName { get; }
         protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, NameAuthorizationRequirement requirement);
     }
     public class OperationAuthorizationRequirement : IAuthorizationRequirement {
         public OperationAuthorizationRequirement();
         public string Name { get; set; }
     }
     public class PassThroughAuthorizationHandler : IAuthorizationHandler {
         public PassThroughAuthorizationHandler();
         public Task HandleAsync(AuthorizationHandlerContext context);
     }
     public class RolesAuthorizationRequirement : AuthorizationHandler<RolesAuthorizationRequirement>, IAuthorizationRequirement {
         public RolesAuthorizationRequirement(IEnumerable<string> allowedRoles);
         public IEnumerable<string> AllowedRoles { get; }
         protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RolesAuthorizationRequirement requirement);
     }
 }
```

