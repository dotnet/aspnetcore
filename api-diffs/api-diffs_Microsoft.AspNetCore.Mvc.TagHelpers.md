# Microsoft.AspNetCore.Mvc.TagHelpers

``` diff
 namespace Microsoft.AspNetCore.Mvc.TagHelpers {
     public class CacheTagHelper : CacheTagHelperBase {
+        public CacheTagHelper(CacheTagHelperMemoryCacheFactory factory, HtmlEncoder htmlEncoder);
-        public CacheTagHelper(CacheTagHelperMemoryCacheFactory factory, HtmlEncoder htmlEncoder);

     }
+    public class CacheTagHelperMemoryCacheFactory {
+        public CacheTagHelperMemoryCacheFactory(IOptions<CacheTagHelperOptions> options);
+        public IMemoryCache Cache { get; }
+    }
     public class EnvironmentTagHelper : TagHelper {
-        public EnvironmentTagHelper(IHostingEnvironment hostingEnvironment);

+        public EnvironmentTagHelper(IWebHostEnvironment hostingEnvironment);
-        protected IHostingEnvironment HostingEnvironment { get; }
+        protected IWebHostEnvironment HostingEnvironment { get; }
     }
+    public class GlobbingUrlBuilder {
+        public GlobbingUrlBuilder(IFileProvider fileProvider, IMemoryCache cache, PathString requestPathBase);
+        public IMemoryCache Cache { get; }
+        public IFileProvider FileProvider { get; }
+        public PathString RequestPathBase { get; }
+        public virtual IReadOnlyList<string> BuildUrlList(string staticUrl, string includePattern, string excludePattern);
+    }
     public class ImageTagHelper : UrlResolutionTagHelper {
-        public ImageTagHelper(IHostingEnvironment hostingEnvironment, TagHelperMemoryCacheProvider cacheProvider, IFileVersionProvider fileVersionProvider, HtmlEncoder htmlEncoder, IUrlHelperFactory urlHelperFactory);

-        public ImageTagHelper(IHostingEnvironment hostingEnvironment, IMemoryCache cache, HtmlEncoder htmlEncoder, IUrlHelperFactory urlHelperFactory);

+        public ImageTagHelper(IWebHostEnvironment hostingEnvironment, TagHelperMemoryCacheProvider cacheProvider, IFileVersionProvider fileVersionProvider, HtmlEncoder htmlEncoder, IUrlHelperFactory urlHelperFactory);
+        public ImageTagHelper(IFileVersionProvider fileVersionProvider, HtmlEncoder htmlEncoder, IUrlHelperFactory urlHelperFactory);
-        protected internal IHostingEnvironment HostingEnvironment { get; }
+        protected internal IWebHostEnvironment HostingEnvironment { get; }
     }
     public class LinkTagHelper : UrlResolutionTagHelper {
-        public LinkTagHelper(IHostingEnvironment hostingEnvironment, TagHelperMemoryCacheProvider cacheProvider, IFileVersionProvider fileVersionProvider, HtmlEncoder htmlEncoder, JavaScriptEncoder javaScriptEncoder, IUrlHelperFactory urlHelperFactory);

-        public LinkTagHelper(IHostingEnvironment hostingEnvironment, IMemoryCache cache, HtmlEncoder htmlEncoder, JavaScriptEncoder javaScriptEncoder, IUrlHelperFactory urlHelperFactory);

+        public LinkTagHelper(IWebHostEnvironment hostingEnvironment, TagHelperMemoryCacheProvider cacheProvider, IFileVersionProvider fileVersionProvider, HtmlEncoder htmlEncoder, JavaScriptEncoder javaScriptEncoder, IUrlHelperFactory urlHelperFactory);
-        protected internal IHostingEnvironment HostingEnvironment { get; }
+        protected internal IWebHostEnvironment HostingEnvironment { get; }
     }
     public class PartialTagHelper : TagHelper {
+        public PartialTagHelper(ICompositeViewEngine viewEngine, IViewBufferScope viewBufferScope);
-        public PartialTagHelper(ICompositeViewEngine viewEngine, IViewBufferScope viewBufferScope);

     }
     public class ScriptTagHelper : UrlResolutionTagHelper {
-        public ScriptTagHelper(IHostingEnvironment hostingEnvironment, TagHelperMemoryCacheProvider cacheProvider, IFileVersionProvider fileVersionProvider, HtmlEncoder htmlEncoder, JavaScriptEncoder javaScriptEncoder, IUrlHelperFactory urlHelperFactory);

-        public ScriptTagHelper(IHostingEnvironment hostingEnvironment, IMemoryCache cache, HtmlEncoder htmlEncoder, JavaScriptEncoder javaScriptEncoder, IUrlHelperFactory urlHelperFactory);

+        public ScriptTagHelper(IWebHostEnvironment hostingEnvironment, TagHelperMemoryCacheProvider cacheProvider, IFileVersionProvider fileVersionProvider, HtmlEncoder htmlEncoder, JavaScriptEncoder javaScriptEncoder, IUrlHelperFactory urlHelperFactory);
-        protected internal IHostingEnvironment HostingEnvironment { get; }
+        protected internal IWebHostEnvironment HostingEnvironment { get; }
     }
 }
```

