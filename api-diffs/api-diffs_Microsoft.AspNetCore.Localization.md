# Microsoft.AspNetCore.Localization

``` diff
 namespace Microsoft.AspNetCore.Localization {
     public class AcceptLanguageHeaderRequestCultureProvider : RequestCultureProvider {
         public AcceptLanguageHeaderRequestCultureProvider();
         public int MaximumAcceptLanguageHeaderValuesToTry { get; set; }
         public override Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext httpContext);
     }
     public class CookieRequestCultureProvider : RequestCultureProvider {
         public static readonly string DefaultCookieName;
         public CookieRequestCultureProvider();
         public string CookieName { get; set; }
         public override Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext httpContext);
         public static string MakeCookieValue(RequestCulture requestCulture);
         public static ProviderCultureResult ParseCookieValue(string value);
     }
     public class CustomRequestCultureProvider : RequestCultureProvider {
         public CustomRequestCultureProvider(Func<HttpContext, Task<ProviderCultureResult>> provider);
         public override Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext httpContext);
     }
     public interface IRequestCultureFeature {
         IRequestCultureProvider Provider { get; }
         RequestCulture RequestCulture { get; }
     }
     public interface IRequestCultureProvider {
         Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext httpContext);
     }
     public class ProviderCultureResult {
         public ProviderCultureResult(StringSegment culture);
         public ProviderCultureResult(StringSegment culture, StringSegment uiCulture);
         public ProviderCultureResult(IList<StringSegment> cultures);
         public ProviderCultureResult(IList<StringSegment> cultures, IList<StringSegment> uiCultures);
         public IList<StringSegment> Cultures { get; }
         public IList<StringSegment> UICultures { get; }
     }
     public class QueryStringRequestCultureProvider : RequestCultureProvider {
         public QueryStringRequestCultureProvider();
         public string QueryStringKey { get; set; }
         public string UIQueryStringKey { get; set; }
         public override Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext httpContext);
     }
     public class RequestCulture {
         public RequestCulture(CultureInfo culture);
         public RequestCulture(CultureInfo culture, CultureInfo uiCulture);
         public RequestCulture(string culture);
         public RequestCulture(string culture, string uiCulture);
         public CultureInfo Culture { get; }
         public CultureInfo UICulture { get; }
     }
     public class RequestCultureFeature : IRequestCultureFeature {
         public RequestCultureFeature(RequestCulture requestCulture, IRequestCultureProvider provider);
         public IRequestCultureProvider Provider { get; }
         public RequestCulture RequestCulture { get; }
     }
     public abstract class RequestCultureProvider : IRequestCultureProvider {
         protected static readonly Task<ProviderCultureResult> NullProviderCultureResult;
         protected RequestCultureProvider();
         public RequestLocalizationOptions Options { get; set; }
         public abstract Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext httpContext);
     }
     public class RequestLocalizationMiddleware {
         public RequestLocalizationMiddleware(RequestDelegate next, IOptions<RequestLocalizationOptions> options);
+        public RequestLocalizationMiddleware(RequestDelegate next, IOptions<RequestLocalizationOptions> options, ILoggerFactory loggerFactory);
         public Task Invoke(HttpContext context);
     }
 }
```

