# Microsoft.AspNetCore.Mvc.Razor

``` diff
 namespace Microsoft.AspNetCore.Mvc.Razor {
     public class HelperResult : IHtmlContent {
         public HelperResult(Func<TextWriter, Task> asyncAction);
         public Func<TextWriter, Task> WriteAction { get; }
         public virtual void WriteTo(TextWriter writer, HtmlEncoder encoder);
     }
     public interface IRazorPage {
         IHtmlContent BodyContent { get; set; }
         bool IsLayoutBeingRendered { get; set; }
         string Layout { get; set; }
         string Path { get; set; }
         IDictionary<string, RenderAsyncDelegate> PreviousSectionWriters { get; set; }
         IDictionary<string, RenderAsyncDelegate> SectionWriters { get; }
         ViewContext ViewContext { get; set; }
         void EnsureRenderedBodyOrSections();
         Task ExecuteAsync();
     }
     public interface IRazorPageActivator {
         void Activate(IRazorPage page, ViewContext context);
     }
     public interface IRazorPageFactoryProvider {
         RazorPageFactoryResult CreateFactory(string relativePath);
     }
     public interface IRazorViewEngine : IViewEngine {
         RazorPageResult FindPage(ActionContext context, string pageName);
         string GetAbsolutePath(string executingFilePath, string pagePath);
         RazorPageResult GetPage(string executingFilePath, string pagePath);
     }
     public interface ITagHelperActivator {
         TTagHelper Create<TTagHelper>(ViewContext context) where TTagHelper : ITagHelper;
     }
     public interface ITagHelperFactory {
         TTagHelper CreateTagHelper<TTagHelper>(ViewContext context) where TTagHelper : ITagHelper;
     }
     public interface ITagHelperInitializer<TTagHelper> where TTagHelper : ITagHelper {
         void Initialize(TTagHelper helper, ViewContext context);
     }
     public interface IViewLocationExpander {
         IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations);
         void PopulateValues(ViewLocationExpanderContext context);
     }
     public class LanguageViewLocationExpander : IViewLocationExpander {
         public LanguageViewLocationExpander();
         public LanguageViewLocationExpander(LanguageViewLocationExpanderFormat format);
         public virtual IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations);
         public void PopulateValues(ViewLocationExpanderContext context);
     }
     public enum LanguageViewLocationExpanderFormat {
         SubFolder = 0,
         Suffix = 1,
     }
     public abstract class RazorPage : RazorPageBase {
         protected RazorPage();
         public HttpContext Context { get; }
         public override void BeginContext(int position, int length, bool isLiteral);
         public override void DefineSection(string name, RenderAsyncDelegate section);
         public override void EndContext();
         public override void EnsureRenderedBodyOrSections();
         public void IgnoreBody();
         public void IgnoreSection(string sectionName);
         public bool IsSectionDefined(string name);
         protected virtual IHtmlContent RenderBody();
         public HtmlString RenderSection(string name);
         public HtmlString RenderSection(string name, bool required);
         public Task<HtmlString> RenderSectionAsync(string name);
         public Task<HtmlString> RenderSectionAsync(string name, bool required);
     }
     public abstract class RazorPage<TModel> : RazorPage {
         protected RazorPage();
         public TModel Model { get; }
         public ViewDataDictionary<TModel> ViewData { get; set; }
     }
     public class RazorPageActivator : IRazorPageActivator {
         public RazorPageActivator(IModelMetadataProvider metadataProvider, IUrlHelperFactory urlHelperFactory, IJsonHelper jsonHelper, DiagnosticSource diagnosticSource, HtmlEncoder htmlEncoder, IModelExpressionProvider modelExpressionProvider);
         public void Activate(IRazorPage page, ViewContext context);
     }
     public abstract class RazorPageBase : IRazorPage {
         protected RazorPageBase();
         public IHtmlContent BodyContent { get; set; }
         public DiagnosticSource DiagnosticSource { get; set; }
         public HtmlEncoder HtmlEncoder { get; set; }
         public bool IsLayoutBeingRendered { get; set; }
         public string Layout { get; set; }
         public virtual TextWriter Output { get; }
         public string Path { get; set; }
         public IDictionary<string, RenderAsyncDelegate> PreviousSectionWriters { get; set; }
         public IDictionary<string, RenderAsyncDelegate> SectionWriters { get; }
         public ITempDataDictionary TempData { get; }
         public virtual ClaimsPrincipal User { get; }
         public dynamic ViewBag { get; }
         public virtual ViewContext ViewContext { get; set; }
         public void AddHtmlAttributeValue(string prefix, int prefixOffset, object value, int valueOffset, int valueLength, bool isLiteral);
         public void BeginAddHtmlAttributeValues(TagHelperExecutionContext executionContext, string attributeName, int attributeValuesCount, HtmlAttributeValueStyle attributeValueStyle);
         public abstract void BeginContext(int position, int length, bool isLiteral);
         public virtual void BeginWriteAttribute(string name, string prefix, int prefixOffset, string suffix, int suffixOffset, int attributeValuesCount);
         public void BeginWriteTagHelperAttribute();
         public TTagHelper CreateTagHelper<TTagHelper>() where TTagHelper : ITagHelper;
         public virtual void DefineSection(string name, RenderAsyncDelegate section);
         protected void DefineSection(string name, Func<object, Task> section);
         public void EndAddHtmlAttributeValues(TagHelperExecutionContext executionContext);
         public abstract void EndContext();
         public TagHelperContent EndTagHelperWritingScope();
         public virtual void EndWriteAttribute();
         public string EndWriteTagHelperAttribute();
         public abstract void EnsureRenderedBodyOrSections();
         public abstract Task ExecuteAsync();
         public virtual Task<HtmlString> FlushAsync();
         public virtual string Href(string contentPath);
         public string InvalidTagHelperIndexerAssignment(string attributeName, string tagHelperTypeName, string propertyName);
         protected internal virtual TextWriter PopWriter();
         protected internal virtual void PushWriter(TextWriter writer);
         public virtual HtmlString SetAntiforgeryCookieAndHeader();
         public void StartTagHelperWritingScope(HtmlEncoder encoder);
         public virtual void Write(object value);
         public virtual void Write(string value);
         public void WriteAttributeValue(string prefix, int prefixOffset, object value, int valueOffset, int valueLength, bool isLiteral);
         public virtual void WriteLiteral(object value);
         public virtual void WriteLiteral(string value);
     }
     public readonly struct RazorPageFactoryResult {
         public RazorPageFactoryResult(CompiledViewDescriptor viewDescriptor, Func<IRazorPage> razorPageFactory);
         public Func<IRazorPage> RazorPageFactory { get; }
         public bool Success { get; }
         public CompiledViewDescriptor ViewDescriptor { get; }
     }
     public readonly struct RazorPageResult {
         public RazorPageResult(string name, IRazorPage page);
         public RazorPageResult(string name, IEnumerable<string> searchedLocations);
         public string Name { get; }
         public IRazorPage Page { get; }
         public IEnumerable<string> SearchedLocations { get; }
     }
     public class RazorView : IView {
+        public RazorView(IRazorViewEngine viewEngine, IRazorPageActivator pageActivator, IReadOnlyList<IRazorPage> viewStartPages, IRazorPage razorPage, HtmlEncoder htmlEncoder, DiagnosticListener diagnosticListener);
-        public RazorView(IRazorViewEngine viewEngine, IRazorPageActivator pageActivator, IReadOnlyList<IRazorPage> viewStartPages, IRazorPage razorPage, HtmlEncoder htmlEncoder, DiagnosticSource diagnosticSource);

         public string Path { get; }
         public IRazorPage RazorPage { get; }
         public IReadOnlyList<IRazorPage> ViewStartPages { get; }
         public virtual Task RenderAsync(ViewContext context);
     }
     public class RazorViewEngine : IRazorViewEngine, IViewEngine {
         public static readonly string ViewExtension;
-        public RazorViewEngine(IRazorPageFactoryProvider pageFactory, IRazorPageActivator pageActivator, HtmlEncoder htmlEncoder, IOptions<RazorViewEngineOptions> optionsAccessor, RazorProject razorProject, ILoggerFactory loggerFactory, DiagnosticSource diagnosticSource);

-        public RazorViewEngine(IRazorPageFactoryProvider pageFactory, IRazorPageActivator pageActivator, HtmlEncoder htmlEncoder, IOptions<RazorViewEngineOptions> optionsAccessor, RazorProjectFileSystem razorFileSystem, ILoggerFactory loggerFactory, DiagnosticSource diagnosticSource);

+        public RazorViewEngine(IRazorPageFactoryProvider pageFactory, IRazorPageActivator pageActivator, HtmlEncoder htmlEncoder, IOptions<RazorViewEngineOptions> optionsAccessor, ILoggerFactory loggerFactory, DiagnosticListener diagnosticListener);
         protected IMemoryCache ViewLookupCache { get; }
         public RazorPageResult FindPage(ActionContext context, string pageName);
         public ViewEngineResult FindView(ActionContext context, string viewName, bool isMainPage);
         public string GetAbsolutePath(string executingFilePath, string pagePath);
         public static string GetNormalizedRouteValue(ActionContext context, string key);
         public RazorPageResult GetPage(string executingFilePath, string pagePath);
         public ViewEngineResult GetView(string executingFilePath, string viewPath, bool isMainPage);
     }
-    public class RazorViewEngineOptions : IEnumerable, IEnumerable<ICompatibilitySwitch> {
+    public class RazorViewEngineOptions {
         public RazorViewEngineOptions();
-        public IList<MetadataReference> AdditionalCompilationReferences { get; }

-        public bool AllowRecompilingViewsOnFileChange { get; set; }

         public IList<string> AreaPageViewLocationFormats { get; }
         public IList<string> AreaViewLocationFormats { get; }
-        public Action<RoslynCompilationContext> CompilationCallback { get; set; }

-        public IList<IFileProvider> FileProviders { get; }

         public IList<string> PageViewLocationFormats { get; }
         public IList<IViewLocationExpander> ViewLocationExpanders { get; }
         public IList<string> ViewLocationFormats { get; }
-        IEnumerator<ICompatibilitySwitch> System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

     }
     public delegate Task RenderAsyncDelegate();
     public class TagHelperInitializer<TTagHelper> : ITagHelperInitializer<TTagHelper> where TTagHelper : ITagHelper {
         public TagHelperInitializer(Action<TTagHelper, ViewContext> action);
         public void Initialize(TTagHelper helper, ViewContext context);
     }
     public class ViewLocationExpanderContext {
         public ViewLocationExpanderContext(ActionContext actionContext, string viewName, string controllerName, string areaName, string pageName, bool isMainPage);
         public ActionContext ActionContext { get; }
         public string AreaName { get; }
         public string ControllerName { get; }
         public bool IsMainPage { get; }
         public string PageName { get; }
         public IDictionary<string, string> Values { get; set; }
         public string ViewName { get; }
     }
 }
```

