# Microsoft.AspNetCore.HttpsPolicy

``` diff
 namespace Microsoft.AspNetCore.HttpsPolicy {
     public class HstsMiddleware {
         public HstsMiddleware(RequestDelegate next, IOptions<HstsOptions> options);
         public HstsMiddleware(RequestDelegate next, IOptions<HstsOptions> options, ILoggerFactory loggerFactory);
         public Task Invoke(HttpContext context);
     }
     public class HstsOptions {
         public HstsOptions();
         public IList<string> ExcludedHosts { get; }
         public bool IncludeSubDomains { get; set; }
         public TimeSpan MaxAge { get; set; }
         public bool Preload { get; set; }
     }
     public class HttpsRedirectionMiddleware {
         public HttpsRedirectionMiddleware(RequestDelegate next, IOptions<HttpsRedirectionOptions> options, IConfiguration config, ILoggerFactory loggerFactory);
         public HttpsRedirectionMiddleware(RequestDelegate next, IOptions<HttpsRedirectionOptions> options, IConfiguration config, ILoggerFactory loggerFactory, IServerAddressesFeature serverAddressesFeature);
         public Task Invoke(HttpContext context);
     }
     public class HttpsRedirectionOptions {
         public HttpsRedirectionOptions();
         public Nullable<int> HttpsPort { get; set; }
         public int RedirectStatusCode { get; set; }
     }
 }
```

