# Microsoft.AspNetCore.StaticFiles

``` diff
 namespace Microsoft.AspNetCore.StaticFiles {
     public class DefaultFilesMiddleware {
-        public DefaultFilesMiddleware(RequestDelegate next, IHostingEnvironment hostingEnv, IOptions<DefaultFilesOptions> options);

+        public DefaultFilesMiddleware(RequestDelegate next, IWebHostEnvironment hostingEnv, IOptions<DefaultFilesOptions> options);
         public Task Invoke(HttpContext context);
     }
     public class DirectoryBrowserMiddleware {
-        public DirectoryBrowserMiddleware(RequestDelegate next, IHostingEnvironment hostingEnv, IOptions<DirectoryBrowserOptions> options);

-        public DirectoryBrowserMiddleware(RequestDelegate next, IHostingEnvironment hostingEnv, HtmlEncoder encoder, IOptions<DirectoryBrowserOptions> options);

+        public DirectoryBrowserMiddleware(RequestDelegate next, IWebHostEnvironment hostingEnv, IOptions<DirectoryBrowserOptions> options);
+        public DirectoryBrowserMiddleware(RequestDelegate next, IWebHostEnvironment hostingEnv, HtmlEncoder encoder, IOptions<DirectoryBrowserOptions> options);
         public Task Invoke(HttpContext context);
     }
     public class FileExtensionContentTypeProvider : IContentTypeProvider {
         public FileExtensionContentTypeProvider();
         public FileExtensionContentTypeProvider(IDictionary<string, string> mapping);
         public IDictionary<string, string> Mappings { get; private set; }
         public bool TryGetContentType(string subpath, out string contentType);
     }
     public class HtmlDirectoryFormatter : IDirectoryFormatter {
         public HtmlDirectoryFormatter(HtmlEncoder encoder);
         public virtual Task GenerateContentAsync(HttpContext context, IEnumerable<IFileInfo> contents);
     }
     public interface IContentTypeProvider {
         bool TryGetContentType(string subpath, out string contentType);
     }
     public interface IDirectoryFormatter {
         Task GenerateContentAsync(HttpContext context, IEnumerable<IFileInfo> contents);
     }
     public class StaticFileMiddleware {
-        public StaticFileMiddleware(RequestDelegate next, IHostingEnvironment hostingEnv, IOptions<StaticFileOptions> options, ILoggerFactory loggerFactory);

+        public StaticFileMiddleware(RequestDelegate next, IWebHostEnvironment hostingEnv, IOptions<StaticFileOptions> options, ILoggerFactory loggerFactory);
         public Task Invoke(HttpContext context);
     }
     public class StaticFileResponseContext {
         public StaticFileResponseContext();
+        public StaticFileResponseContext(HttpContext context, IFileInfo file);
         public HttpContext Context { get; internal set; }
         public IFileInfo File { get; internal set; }
     }
 }
```

