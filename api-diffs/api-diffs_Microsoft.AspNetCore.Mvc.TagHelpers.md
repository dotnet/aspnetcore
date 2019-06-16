# Microsoft.AspNetCore.Mvc.TagHelpers

``` diff
 namespace Microsoft.AspNetCore.Mvc.TagHelpers {
     public class AnchorTagHelper : TagHelper {
         public AnchorTagHelper(IHtmlGenerator generator);
         public string Action { get; set; }
         public string Area { get; set; }
         public string Controller { get; set; }
         public string Fragment { get; set; }
         protected IHtmlGenerator Generator { get; }
         public string Host { get; set; }
         public override int Order { get; }
         public string Page { get; set; }
         public string PageHandler { get; set; }
         public string Protocol { get; set; }
         public string Route { get; set; }
         public IDictionary<string, string> RouteValues { get; set; }
         public ViewContext ViewContext { get; set; }
         public override void Process(TagHelperContext context, TagHelperOutput output);
     }
     public class CacheTagHelper : CacheTagHelperBase {
         public static readonly string CacheKeyPrefix;
+        public CacheTagHelper(CacheTagHelperMemoryCacheFactory factory, HtmlEncoder htmlEncoder);
-        public CacheTagHelper(CacheTagHelperMemoryCacheFactory factory, HtmlEncoder htmlEncoder);

         protected IMemoryCache MemoryCache { get; }
         public Nullable<CacheItemPriority> Priority { get; set; }
         public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output);
     }
     public abstract class CacheTagHelperBase : TagHelper {
         public static readonly TimeSpan DefaultExpiration;
         public CacheTagHelperBase(HtmlEncoder htmlEncoder);
         public bool Enabled { get; set; }
         public Nullable<TimeSpan> ExpiresAfter { get; set; }
         public Nullable<DateTimeOffset> ExpiresOn { get; set; }
         public Nullable<TimeSpan> ExpiresSliding { get; set; }
         protected HtmlEncoder HtmlEncoder { get; }
         public override int Order { get; }
         public string VaryBy { get; set; }
         public string VaryByCookie { get; set; }
         public bool VaryByCulture { get; set; }
         public string VaryByHeader { get; set; }
         public string VaryByQuery { get; set; }
         public string VaryByRoute { get; set; }
         public bool VaryByUser { get; set; }
         public ViewContext ViewContext { get; set; }
     }
+    public class CacheTagHelperMemoryCacheFactory {
+        public CacheTagHelperMemoryCacheFactory(IOptions<CacheTagHelperOptions> options);
+        public IMemoryCache Cache { get; }
+    }
     public class CacheTagHelperOptions {
         public CacheTagHelperOptions();
         public long SizeLimit { get; set; }
     }
     public class DistributedCacheTagHelper : CacheTagHelperBase {
         public static readonly string CacheKeyPrefix;
         public DistributedCacheTagHelper(IDistributedCacheTagHelperService distributedCacheService, HtmlEncoder htmlEncoder);
         protected IMemoryCache MemoryCache { get; }
         public string Name { get; set; }
         public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output);
     }
     public class EnvironmentTagHelper : TagHelper {
-        public EnvironmentTagHelper(IHostingEnvironment hostingEnvironment);

+        public EnvironmentTagHelper(IWebHostEnvironment hostingEnvironment);
         public string Exclude { get; set; }
-        protected IHostingEnvironment HostingEnvironment { get; }
+        protected IWebHostEnvironment HostingEnvironment { get; }
         public string Include { get; set; }
         public string Names { get; set; }
         public override int Order { get; }
         public override void Process(TagHelperContext context, TagHelperOutput output);
     }
     public class FormActionTagHelper : TagHelper {
         public FormActionTagHelper(IUrlHelperFactory urlHelperFactory);
         public string Action { get; set; }
         public string Area { get; set; }
         public string Controller { get; set; }
         public string Fragment { get; set; }
         public override int Order { get; }
         public string Page { get; set; }
         public string PageHandler { get; set; }
         public string Route { get; set; }
         public IDictionary<string, string> RouteValues { get; set; }
         protected IUrlHelperFactory UrlHelperFactory { get; }
         public ViewContext ViewContext { get; set; }
         public override void Process(TagHelperContext context, TagHelperOutput output);
     }
     public class FormTagHelper : TagHelper {
         public FormTagHelper(IHtmlGenerator generator);
         public string Action { get; set; }
         public Nullable<bool> Antiforgery { get; set; }
         public string Area { get; set; }
         public string Controller { get; set; }
         public string Fragment { get; set; }
         protected IHtmlGenerator Generator { get; }
         public string Method { get; set; }
         public override int Order { get; }
         public string Page { get; set; }
         public string PageHandler { get; set; }
         public string Route { get; set; }
         public IDictionary<string, string> RouteValues { get; set; }
         public ViewContext ViewContext { get; set; }
         public override void Process(TagHelperContext context, TagHelperOutput output);
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
         public bool AppendVersion { get; set; }
         protected internal IMemoryCache Cache { get; }
-        protected internal IHostingEnvironment HostingEnvironment { get; }
+        protected internal IWebHostEnvironment HostingEnvironment { get; }
         public override int Order { get; }
         public string Src { get; set; }
         public override void Process(TagHelperContext context, TagHelperOutput output);
     }
     public class InputTagHelper : TagHelper {
         public InputTagHelper(IHtmlGenerator generator);
         public ModelExpression For { get; set; }
         public string Format { get; set; }
         protected IHtmlGenerator Generator { get; }
         public string InputTypeName { get; set; }
         public string Name { get; set; }
         public override int Order { get; }
         public string Value { get; set; }
         public ViewContext ViewContext { get; set; }
         protected string GetInputType(ModelExplorer modelExplorer, out string inputTypeHint);
         public override void Process(TagHelperContext context, TagHelperOutput output);
     }
     public class LabelTagHelper : TagHelper {
         public LabelTagHelper(IHtmlGenerator generator);
         public ModelExpression For { get; set; }
         protected IHtmlGenerator Generator { get; }
         public override int Order { get; }
         public ViewContext ViewContext { get; set; }
         public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output);
     }
     public class LinkTagHelper : UrlResolutionTagHelper {
-        public LinkTagHelper(IHostingEnvironment hostingEnvironment, TagHelperMemoryCacheProvider cacheProvider, IFileVersionProvider fileVersionProvider, HtmlEncoder htmlEncoder, JavaScriptEncoder javaScriptEncoder, IUrlHelperFactory urlHelperFactory);

-        public LinkTagHelper(IHostingEnvironment hostingEnvironment, IMemoryCache cache, HtmlEncoder htmlEncoder, JavaScriptEncoder javaScriptEncoder, IUrlHelperFactory urlHelperFactory);

+        public LinkTagHelper(IWebHostEnvironment hostingEnvironment, TagHelperMemoryCacheProvider cacheProvider, IFileVersionProvider fileVersionProvider, HtmlEncoder htmlEncoder, JavaScriptEncoder javaScriptEncoder, IUrlHelperFactory urlHelperFactory);
         public Nullable<bool> AppendVersion { get; set; }
         protected internal IMemoryCache Cache { get; }
         public string FallbackHref { get; set; }
         public string FallbackHrefExclude { get; set; }
         public string FallbackHrefInclude { get; set; }
         public string FallbackTestClass { get; set; }
         public string FallbackTestProperty { get; set; }
         public string FallbackTestValue { get; set; }
         protected internal GlobbingUrlBuilder GlobbingUrlBuilder { get; set; }
-        protected internal IHostingEnvironment HostingEnvironment { get; }
+        protected internal IWebHostEnvironment HostingEnvironment { get; }
         public string Href { get; set; }
         public string HrefExclude { get; set; }
         public string HrefInclude { get; set; }
         protected JavaScriptEncoder JavaScriptEncoder { get; }
         public override int Order { get; }
         public bool SuppressFallbackIntegrity { get; set; }
         public override void Process(TagHelperContext context, TagHelperOutput output);
     }
     public class OptionTagHelper : TagHelper {
         public OptionTagHelper(IHtmlGenerator generator);
         protected IHtmlGenerator Generator { get; }
         public override int Order { get; }
         public string Value { get; set; }
         public ViewContext ViewContext { get; set; }
         public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output);
     }
     public class PartialTagHelper : TagHelper {
+        public PartialTagHelper(ICompositeViewEngine viewEngine, IViewBufferScope viewBufferScope);
-        public PartialTagHelper(ICompositeViewEngine viewEngine, IViewBufferScope viewBufferScope);

         public string FallbackName { get; set; }
         public ModelExpression For { get; set; }
         public object Model { get; set; }
         public string Name { get; set; }
         public bool Optional { get; set; }
         public ViewContext ViewContext { get; set; }
         public ViewDataDictionary ViewData { get; set; }
         public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output);
     }
     public class RenderAtEndOfFormTagHelper : TagHelper {
         public RenderAtEndOfFormTagHelper();
         public override int Order { get; }
         public ViewContext ViewContext { get; set; }
         public override void Init(TagHelperContext context);
         public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output);
     }
     public class ScriptTagHelper : UrlResolutionTagHelper {
-        public ScriptTagHelper(IHostingEnvironment hostingEnvironment, TagHelperMemoryCacheProvider cacheProvider, IFileVersionProvider fileVersionProvider, HtmlEncoder htmlEncoder, JavaScriptEncoder javaScriptEncoder, IUrlHelperFactory urlHelperFactory);

-        public ScriptTagHelper(IHostingEnvironment hostingEnvironment, IMemoryCache cache, HtmlEncoder htmlEncoder, JavaScriptEncoder javaScriptEncoder, IUrlHelperFactory urlHelperFactory);

+        public ScriptTagHelper(IWebHostEnvironment hostingEnvironment, TagHelperMemoryCacheProvider cacheProvider, IFileVersionProvider fileVersionProvider, HtmlEncoder htmlEncoder, JavaScriptEncoder javaScriptEncoder, IUrlHelperFactory urlHelperFactory);
         public Nullable<bool> AppendVersion { get; set; }
         protected internal IMemoryCache Cache { get; private set; }
         public string FallbackSrc { get; set; }
         public string FallbackSrcExclude { get; set; }
         public string FallbackSrcInclude { get; set; }
         public string FallbackTestExpression { get; set; }
         protected internal GlobbingUrlBuilder GlobbingUrlBuilder { get; set; }
-        protected internal IHostingEnvironment HostingEnvironment { get; }
+        protected internal IWebHostEnvironment HostingEnvironment { get; }
         protected JavaScriptEncoder JavaScriptEncoder { get; }
         public override int Order { get; }
         public string Src { get; set; }
         public string SrcExclude { get; set; }
         public string SrcInclude { get; set; }
         public bool SuppressFallbackIntegrity { get; set; }
         public override void Process(TagHelperContext context, TagHelperOutput output);
     }
     public class SelectTagHelper : TagHelper {
         public SelectTagHelper(IHtmlGenerator generator);
         public ModelExpression For { get; set; }
         protected IHtmlGenerator Generator { get; }
         public IEnumerable<SelectListItem> Items { get; set; }
         public string Name { get; set; }
         public override int Order { get; }
         public ViewContext ViewContext { get; set; }
         public override void Init(TagHelperContext context);
         public override void Process(TagHelperContext context, TagHelperOutput output);
     }
     public static class TagHelperOutputExtensions {
         public static void AddClass(this TagHelperOutput tagHelperOutput, string classValue, HtmlEncoder htmlEncoder);
         public static void CopyHtmlAttribute(this TagHelperOutput tagHelperOutput, string attributeName, TagHelperContext context);
         public static void MergeAttributes(this TagHelperOutput tagHelperOutput, TagBuilder tagBuilder);
         public static void RemoveClass(this TagHelperOutput tagHelperOutput, string classValue, HtmlEncoder htmlEncoder);
         public static void RemoveRange(this TagHelperOutput tagHelperOutput, IEnumerable<TagHelperAttribute> attributes);
     }
     public class TextAreaTagHelper : TagHelper {
         public TextAreaTagHelper(IHtmlGenerator generator);
         public ModelExpression For { get; set; }
         protected IHtmlGenerator Generator { get; }
         public string Name { get; set; }
         public override int Order { get; }
         public ViewContext ViewContext { get; set; }
         public override void Process(TagHelperContext context, TagHelperOutput output);
     }
     public class ValidationMessageTagHelper : TagHelper {
         public ValidationMessageTagHelper(IHtmlGenerator generator);
         public ModelExpression For { get; set; }
         protected IHtmlGenerator Generator { get; }
         public override int Order { get; }
         public ViewContext ViewContext { get; set; }
         public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output);
     }
     public class ValidationSummaryTagHelper : TagHelper {
         public ValidationSummaryTagHelper(IHtmlGenerator generator);
         protected IHtmlGenerator Generator { get; }
         public override int Order { get; }
         public ValidationSummary ValidationSummary { get; set; }
         public ViewContext ViewContext { get; set; }
         public override void Process(TagHelperContext context, TagHelperOutput output);
     }
 }
```

