# Microsoft.AspNetCore.Mvc.Cors.Internal

``` diff
-namespace Microsoft.AspNetCore.Mvc.Cors.Internal {
 {
-    public class CorsApplicationModelProvider : IApplicationModelProvider {
 {
-        public CorsApplicationModelProvider();

-        public int Order { get; }

-        public void OnProvidersExecuted(ApplicationModelProviderContext context);

-        public void OnProvidersExecuting(ApplicationModelProviderContext context);

-    }
-    public class CorsAuthorizationFilterFactory : IFilterFactory, IFilterMetadata, IOrderedFilter {
 {
-        public CorsAuthorizationFilterFactory(string policyName);

-        public bool IsReusable { get; }

-        public int Order { get; }

-        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider);

-    }
-    public class CorsHttpMethodActionConstraint : HttpMethodActionConstraint {
 {
-        public CorsHttpMethodActionConstraint(HttpMethodActionConstraint constraint);

-        public override bool Accept(ActionConstraintContext context);

-    }
-    public static class CorsLoggerExtensions {
 {
-        public static void NotMostEffectiveFilter(this ILogger logger, Type policyType);

-    }
-    public class DisableCorsAuthorizationFilter : IAsyncAuthorizationFilter, ICorsAuthorizationFilter, IFilterMetadata, IOrderedFilter {
 {
-        public DisableCorsAuthorizationFilter();

-        public int Order { get; }

-        public Task OnAuthorizationAsync(AuthorizationFilterContext context);

-    }
-    public interface ICorsAuthorizationFilter : IAsyncAuthorizationFilter, IFilterMetadata, IOrderedFilter

-}
```

