# Microsoft.AspNetCore.StaticFiles

``` diff
 namespace Microsoft.AspNetCore.StaticFiles {
     public class DefaultFilesMiddleware {
-        public DefaultFilesMiddleware(RequestDelegate next, IHostingEnvironment hostingEnv, IOptions<DefaultFilesOptions> options);

+        public DefaultFilesMiddleware(RequestDelegate next, IWebHostEnvironment hostingEnv, IOptions<DefaultFilesOptions> options);
     }
     public class DirectoryBrowserMiddleware {
-        public DirectoryBrowserMiddleware(RequestDelegate next, IHostingEnvironment hostingEnv, IOptions<DirectoryBrowserOptions> options);

-        public DirectoryBrowserMiddleware(RequestDelegate next, IHostingEnvironment hostingEnv, HtmlEncoder encoder, IOptions<DirectoryBrowserOptions> options);

+        public DirectoryBrowserMiddleware(RequestDelegate next, IWebHostEnvironment hostingEnv, IOptions<DirectoryBrowserOptions> options);
+        public DirectoryBrowserMiddleware(RequestDelegate next, IWebHostEnvironment hostingEnv, HtmlEncoder encoder, IOptions<DirectoryBrowserOptions> options);
     }
     public class StaticFileMiddleware {
-        public StaticFileMiddleware(RequestDelegate next, IHostingEnvironment hostingEnv, IOptions<StaticFileOptions> options, ILoggerFactory loggerFactory);

+        public StaticFileMiddleware(RequestDelegate next, IWebHostEnvironment hostingEnv, IOptions<StaticFileOptions> options, ILoggerFactory loggerFactory);
     }
     public class StaticFileResponseContext {
+        public StaticFileResponseContext(HttpContext context, IFileInfo file);
     }
 }
```

