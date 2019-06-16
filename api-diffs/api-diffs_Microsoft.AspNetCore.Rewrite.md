# Microsoft.AspNetCore.Rewrite

``` diff
 namespace Microsoft.AspNetCore.Rewrite {
     public static class ApacheModRewriteOptionsExtensions {
         public static RewriteOptions AddApacheModRewrite(this RewriteOptions options, IFileProvider fileProvider, string filePath);
         public static RewriteOptions AddApacheModRewrite(this RewriteOptions options, TextReader reader);
     }
     public static class IISUrlRewriteOptionsExtensions {
-        public static RewriteOptions AddIISUrlRewrite(this RewriteOptions options, IFileProvider fileProvider, string filePath);

+        public static RewriteOptions AddIISUrlRewrite(this RewriteOptions options, IFileProvider fileProvider, string filePath, bool alwaysUseManagedServerVariables = false);
-        public static RewriteOptions AddIISUrlRewrite(this RewriteOptions options, TextReader reader);

+        public static RewriteOptions AddIISUrlRewrite(this RewriteOptions options, TextReader reader, bool alwaysUseManagedServerVariables = false);
     }
     public interface IRule {
         void ApplyRule(RewriteContext context);
     }
     public class RewriteContext {
         public RewriteContext();
         public HttpContext HttpContext { get; set; }
         public ILogger Logger { get; set; }
         public RuleResult Result { get; set; }
         public IFileProvider StaticFileProvider { get; set; }
     }
     public class RewriteMiddleware {
-        public RewriteMiddleware(RequestDelegate next, IHostingEnvironment hostingEnvironment, ILoggerFactory loggerFactory, IOptions<RewriteOptions> options);

+        public RewriteMiddleware(RequestDelegate next, IWebHostEnvironment hostingEnvironment, ILoggerFactory loggerFactory, IOptions<RewriteOptions> options);
         public Task Invoke(HttpContext context);
     }
     public class RewriteOptions {
         public RewriteOptions();
         public IList<IRule> Rules { get; }
         public IFileProvider StaticFileProvider { get; set; }
     }
     public static class RewriteOptionsExtensions {
         public static RewriteOptions Add(this RewriteOptions options, IRule rule);
         public static RewriteOptions Add(this RewriteOptions options, Action<RewriteContext> applyRule);
         public static RewriteOptions AddRedirect(this RewriteOptions options, string regex, string replacement);
         public static RewriteOptions AddRedirect(this RewriteOptions options, string regex, string replacement, int statusCode);
         public static RewriteOptions AddRedirectToHttps(this RewriteOptions options);
         public static RewriteOptions AddRedirectToHttps(this RewriteOptions options, int statusCode);
         public static RewriteOptions AddRedirectToHttps(this RewriteOptions options, int statusCode, Nullable<int> sslPort);
         public static RewriteOptions AddRedirectToHttpsPermanent(this RewriteOptions options);
         public static RewriteOptions AddRedirectToWww(this RewriteOptions options);
         public static RewriteOptions AddRedirectToWww(this RewriteOptions options, int statusCode);
+        public static RewriteOptions AddRedirectToWww(this RewriteOptions options, int statusCode, params string[] domains);
+        public static RewriteOptions AddRedirectToWww(this RewriteOptions options, params string[] domains);
         public static RewriteOptions AddRedirectToWwwPermanent(this RewriteOptions options);
+        public static RewriteOptions AddRedirectToWwwPermanent(this RewriteOptions options, params string[] domains);
         public static RewriteOptions AddRewrite(this RewriteOptions options, string regex, string replacement, bool skipRemainingRules);
     }
     public enum RuleResult {
         ContinueRules = 0,
         EndResponse = 1,
         SkipRemainingRules = 2,
     }
 }
```

