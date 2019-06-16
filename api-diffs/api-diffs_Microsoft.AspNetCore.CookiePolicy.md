# Microsoft.AspNetCore.CookiePolicy

``` diff
 namespace Microsoft.AspNetCore.CookiePolicy {
     public class AppendCookieContext {
         public AppendCookieContext(HttpContext context, CookieOptions options, string name, string value);
         public HttpContext Context { get; }
         public string CookieName { get; set; }
         public CookieOptions CookieOptions { get; }
         public string CookieValue { get; set; }
         public bool HasConsent { get; internal set; }
         public bool IsConsentNeeded { get; internal set; }
         public bool IssueCookie { get; set; }
     }
     public class CookiePolicyMiddleware {
         public CookiePolicyMiddleware(RequestDelegate next, IOptions<CookiePolicyOptions> options);
         public CookiePolicyMiddleware(RequestDelegate next, IOptions<CookiePolicyOptions> options, ILoggerFactory factory);
         public CookiePolicyOptions Options { get; set; }
         public Task Invoke(HttpContext context);
     }
     public class DeleteCookieContext {
         public DeleteCookieContext(HttpContext context, CookieOptions options, string name);
         public HttpContext Context { get; }
         public string CookieName { get; set; }
         public CookieOptions CookieOptions { get; }
         public bool HasConsent { get; internal set; }
         public bool IsConsentNeeded { get; internal set; }
         public bool IssueCookie { get; set; }
     }
     public enum HttpOnlyPolicy {
         Always = 1,
         None = 0,
     }
 }
```

