# Microsoft.AspNetCore.Builder.Extensions

``` diff
 namespace Microsoft.AspNetCore.Builder.Extensions {
     public class MapMiddleware {
         public MapMiddleware(RequestDelegate next, MapOptions options);
         public Task Invoke(HttpContext context);
     }
     public class MapOptions {
         public MapOptions();
         public RequestDelegate Branch { get; set; }
         public PathString PathMatch { get; set; }
     }
     public class MapWhenMiddleware {
         public MapWhenMiddleware(RequestDelegate next, MapWhenOptions options);
         public Task Invoke(HttpContext context);
     }
     public class MapWhenOptions {
         public MapWhenOptions();
         public RequestDelegate Branch { get; set; }
         public Func<HttpContext, bool> Predicate { get; set; }
     }
     public class UsePathBaseMiddleware {
         public UsePathBaseMiddleware(RequestDelegate next, PathString pathBase);
         public Task Invoke(HttpContext context);
     }
 }
```

