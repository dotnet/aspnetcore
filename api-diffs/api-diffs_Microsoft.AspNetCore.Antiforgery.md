# Microsoft.AspNetCore.Antiforgery

``` diff
 namespace Microsoft.AspNetCore.Antiforgery {
     public class AntiforgeryOptions {
         public static readonly string DefaultCookiePrefix;
         public AntiforgeryOptions();
         public CookieBuilder Cookie { get; set; }
-        public string CookieDomain { get; set; }

-        public string CookieName { get; set; }

-        public Nullable<PathString> CookiePath { get; set; }

         public string FormFieldName { get; set; }
         public string HeaderName { get; set; }
-        public bool RequireSsl { get; set; }

         public bool SuppressXFrameOptionsHeader { get; set; }
     }
     public class AntiforgeryTokenSet {
         public AntiforgeryTokenSet(string requestToken, string cookieToken, string formFieldName, string headerName);
         public string CookieToken { get; }
         public string FormFieldName { get; }
         public string HeaderName { get; }
         public string RequestToken { get; }
     }
     public class AntiforgeryValidationException : Exception {
         public AntiforgeryValidationException(string message);
         public AntiforgeryValidationException(string message, Exception innerException);
     }
     public interface IAntiforgery {
         AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext);
         AntiforgeryTokenSet GetTokens(HttpContext httpContext);
         Task<bool> IsRequestValidAsync(HttpContext httpContext);
         void SetCookieTokenAndHeader(HttpContext httpContext);
         Task ValidateRequestAsync(HttpContext httpContext);
     }
     public interface IAntiforgeryAdditionalDataProvider {
         string GetAdditionalData(HttpContext context);
         bool ValidateAdditionalData(HttpContext context, string additionalData);
     }
 }
```

