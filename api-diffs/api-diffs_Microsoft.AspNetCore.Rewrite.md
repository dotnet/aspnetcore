# Microsoft.AspNetCore.Rewrite

``` diff
 namespace Microsoft.AspNetCore.Rewrite {
     public static class IISUrlRewriteOptionsExtensions {
-        public static RewriteOptions AddIISUrlRewrite(this RewriteOptions options, IFileProvider fileProvider, string filePath);

+        public static RewriteOptions AddIISUrlRewrite(this RewriteOptions options, IFileProvider fileProvider, string filePath, bool alwaysUseManagedServerVariables = false);
-        public static RewriteOptions AddIISUrlRewrite(this RewriteOptions options, TextReader reader);

+        public static RewriteOptions AddIISUrlRewrite(this RewriteOptions options, TextReader reader, bool alwaysUseManagedServerVariables = false);
     }
     public class RewriteMiddleware {
-        public RewriteMiddleware(RequestDelegate next, IHostingEnvironment hostingEnvironment, ILoggerFactory loggerFactory, IOptions<RewriteOptions> options);

+        public RewriteMiddleware(RequestDelegate next, IWebHostEnvironment hostingEnvironment, ILoggerFactory loggerFactory, IOptions<RewriteOptions> options);
     }
     public static class RewriteOptionsExtensions {
+        public static RewriteOptions AddRedirectToWww(this RewriteOptions options, int statusCode, params string[] domains);
+        public static RewriteOptions AddRedirectToWww(this RewriteOptions options, params string[] domains);
+        public static RewriteOptions AddRedirectToWwwPermanent(this RewriteOptions options, params string[] domains);
     }
 }
```

