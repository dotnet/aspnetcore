# Microsoft.AspNetCore.Localization.Routing

``` diff
 namespace Microsoft.AspNetCore.Localization.Routing {
     public class RouteDataRequestCultureProvider : RequestCultureProvider {
         public RouteDataRequestCultureProvider();
         public string RouteDataStringKey { get; set; }
         public string UIRouteDataStringKey { get; set; }
         public override Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext httpContext);
     }
 }
```

