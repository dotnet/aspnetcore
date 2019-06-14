# Microsoft.AspNetCore.Localization

``` diff
 namespace Microsoft.AspNetCore.Localization {
     public class RequestLocalizationMiddleware {
+        public RequestLocalizationMiddleware(RequestDelegate next, IOptions<RequestLocalizationOptions> options, ILoggerFactory loggerFactory);
     }
 }
```

