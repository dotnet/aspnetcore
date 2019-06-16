# Microsoft.AspNetCore.Mvc.Authorization

``` diff
 namespace Microsoft.AspNetCore.Mvc.Authorization {
     public class AllowAnonymousFilter : IAllowAnonymousFilter, IFilterMetadata {
         public AllowAnonymousFilter();
     }
     public class AuthorizeFilter : IAsyncAuthorizationFilter, IFilterFactory, IFilterMetadata {
         public AuthorizeFilter();
         public AuthorizeFilter(AuthorizationPolicy policy);
         public AuthorizeFilter(IAuthorizationPolicyProvider policyProvider, IEnumerable<IAuthorizeData> authorizeData);
         public AuthorizeFilter(IEnumerable<IAuthorizeData> authorizeData);
         public AuthorizeFilter(string policy);
         public IEnumerable<IAuthorizeData> AuthorizeData { get; }
         bool Microsoft.AspNetCore.Mvc.Filters.IFilterFactory.IsReusable { get; }
         public AuthorizationPolicy Policy { get; }
         public IAuthorizationPolicyProvider PolicyProvider { get; }
         IFilterMetadata Microsoft.AspNetCore.Mvc.Filters.IFilterFactory.CreateInstance(IServiceProvider serviceProvider);
         public virtual Task OnAuthorizationAsync(AuthorizationFilterContext context);
     }
     public interface IAllowAnonymousFilter : IFilterMetadata
 }
```

