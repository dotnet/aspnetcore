# Microsoft.AspNetCore.Mvc.Razor.Internal

``` diff
 namespace Microsoft.AspNetCore.Mvc.Razor.Internal {
-    public static class ChecksumValidator {
 {
-        public static bool IsItemValid(RazorProjectFileSystem fileSystem, RazorCompiledItem item);

-        public static bool IsRecompilationSupported(RazorCompiledItem item);

-    }
-    public class CSharpCompiler {
 {
-        public CSharpCompiler(RazorReferenceManager manager, IHostingEnvironment hostingEnvironment);

-        public virtual CSharpCompilationOptions CSharpCompilationOptions { get; }

-        public virtual EmitOptions EmitOptions { get; }

-        public virtual bool EmitPdb { get; }

-        public virtual CSharpParseOptions ParseOptions { get; }

-        public CSharpCompilation CreateCompilation(string assemblyName);

-        public SyntaxTree CreateSyntaxTree(SourceText sourceText);

-        protected internal virtual CompilationOptions GetDependencyContextCompilationOptions();

-    }
-    public class DefaultRazorPageFactoryProvider : IRazorPageFactoryProvider {
 {
-        public DefaultRazorPageFactoryProvider(IViewCompilerProvider viewCompilerProvider);

-        public RazorPageFactoryResult CreateFactory(string relativePath);

-    }
-    public class DefaultRazorReferenceManager : RazorReferenceManager {
 {
-        public DefaultRazorReferenceManager(ApplicationPartManager partManager, IOptions<RazorViewEngineOptions> optionsAccessor);

-        public override IReadOnlyList<MetadataReference> CompilationReferences { get; }

-    }
-    public class DefaultRazorViewEngineFileProviderAccessor : IRazorViewEngineFileProviderAccessor {
 {
-        public DefaultRazorViewEngineFileProviderAccessor(IOptions<RazorViewEngineOptions> optionsAccessor);

-        public IFileProvider FileProvider { get; }

-    }
-    public class DefaultTagHelperActivator : ITagHelperActivator {
 {
-        public DefaultTagHelperActivator(ITypeActivatorCache typeActivatorCache);

-        public TTagHelper Create<TTagHelper>(ViewContext context) where TTagHelper : ITagHelper;

-    }
-    public class DefaultTagHelperFactory : ITagHelperFactory {
 {
-        public DefaultTagHelperFactory(ITagHelperActivator activator);

-        public TTagHelper CreateTagHelper<TTagHelper>(ViewContext context) where TTagHelper : ITagHelper;

-    }
-    public class ExpressionRewriter : CSharpSyntaxRewriter {
 {
-        public ExpressionRewriter(SemanticModel semanticModel);

-        public static CSharpCompilation Rewrite(CSharpCompilation compilation);

-        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node);

-        public override SyntaxNode VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node);

-    }
-    public class FileProviderRazorProjectFileSystem : RazorProjectFileSystem {
 {
-        public FileProviderRazorProjectFileSystem(IRazorViewEngineFileProviderAccessor accessor, IHostingEnvironment hostingEnvironment);

-        public override IEnumerable<RazorProjectItem> EnumerateItems(string path);

-        public override RazorProjectItem GetItem(string path);

-    }
-    public class FileProviderRazorProjectItem : RazorProjectItem {
 {
-        public FileProviderRazorProjectItem(IFileInfo fileInfo, string basePath, string filePath, string root);

-        public override string BasePath { get; }

-        public override bool Exists { get; }

-        public IFileInfo FileInfo { get; }

-        public override string FilePath { get; }

-        public override string PhysicalPath { get; }

-        public override string RelativePhysicalPath { get; }

-        public override Stream Read();

-    }
-    public interface IRazorViewEngineFileProviderAccessor {
 {
-        IFileProvider FileProvider { get; }

-    }
-    public class LazyMetadataReferenceFeature : IMetadataReferenceFeature, IRazorEngineFeature, IRazorFeature {
 {
-        public LazyMetadataReferenceFeature(RazorReferenceManager referenceManager);

-        public RazorEngine Engine { get; set; }

-        public IReadOnlyList<MetadataReference> References { get; }

-    }
-    public static class MvcRazorDiagnosticSourceExtensions {
 {
-        public static void AfterViewPage(this DiagnosticSource diagnosticSource, IRazorPage page, ViewContext viewContext);

-        public static void BeforeViewPage(this DiagnosticSource diagnosticSource, IRazorPage page, ViewContext viewContext);

-    }
-    public static class MvcRazorLoggerExtensions {
 {
-        public static void GeneratedCodeToAssemblyCompilationEnd(this ILogger logger, string filePath, long startTimestamp);

-        public static void GeneratedCodeToAssemblyCompilationStart(this ILogger logger, string filePath);

-        public static void PrecompiledViewFound(this ILogger logger, string relativePath);

-        public static void TagHelperComponentInitialized(this ILogger logger, string componentName);

-        public static void TagHelperComponentProcessed(this ILogger logger, string componentName);

-        public static void ViewCompilerCouldNotFindFileAtPath(this ILogger logger, string path);

-        public static void ViewCompilerEndCodeGeneration(this ILogger logger, string filePath, long startTimestamp);

-        public static void ViewCompilerFoundFileToCompile(this ILogger logger, string path);

-        public static void ViewCompilerInvalidingCompiledFile(this ILogger logger, string path);

-        public static void ViewCompilerLocatedCompiledView(this ILogger logger, string view);

-        public static void ViewCompilerLocatedCompiledViewForPath(this ILogger logger, string path);

-        public static void ViewCompilerNoCompiledViewsFound(this ILogger logger);

-        public static void ViewCompilerStartCodeGeneration(this ILogger logger, string filePath);

-        public static void ViewLookupCacheHit(this ILogger logger, string viewName, string controllerName);

-        public static void ViewLookupCacheMiss(this ILogger logger, string viewName, string controllerName);

-    }
-    public class MvcRazorMvcViewOptionsSetup : IConfigureOptions<MvcViewOptions> {
 {
-        public MvcRazorMvcViewOptionsSetup(IRazorViewEngine razorViewEngine);

-        public void Configure(MvcViewOptions options);

-    }
-    public class RazorPagePropertyActivator {
 {
-        public RazorPagePropertyActivator(Type pageType, Type declaredModelType, IModelMetadataProvider metadataProvider, RazorPagePropertyActivator.PropertyValueAccessors propertyValueAccessors);

-        public void Activate(object page, ViewContext context);

-        public class PropertyValueAccessors {
 {
-            public PropertyValueAccessors();

-            public Func<ViewContext, object> DiagnosticSourceAccessor { get; set; }

-            public Func<ViewContext, object> HtmlEncoderAccessor { get; set; }

-            public Func<ViewContext, object> JsonHelperAccessor { get; set; }

-            public Func<ViewContext, object> ModelExpressionProviderAccessor { get; set; }

-            public Func<ViewContext, object> UrlHelperAccessor { get; set; }

-        }
-    }
-    public class RazorViewCompiler : IViewCompiler {
 {
-        public RazorViewCompiler(IFileProvider fileProvider, RazorProjectEngine projectEngine, CSharpCompiler csharpCompiler, Action<RoslynCompilationContext> compilationCallback, IList<CompiledViewDescriptor> precompiledViews, IMemoryCache cache, ILogger logger);

-        public bool AllowRecompilingViewsOnFileChange { get; set; }

-        protected virtual CompiledViewDescriptor CompileAndEmit(string relativePath);

-        public Task<CompiledViewDescriptor> CompileAsync(string relativePath);

-    }
-    public class RazorViewCompilerProvider : IViewCompilerProvider {
 {
-        public RazorViewCompilerProvider(ApplicationPartManager applicationPartManager, RazorProjectEngine razorProjectEngine, IRazorViewEngineFileProviderAccessor fileProviderAccessor, CSharpCompiler csharpCompiler, IOptions<RazorViewEngineOptions> viewEngineOptionsAccessor, IViewCompilationMemoryCacheProvider compilationMemoryCacheProvider, ILoggerFactory loggerFactory);

-        public IViewCompiler GetCompiler();

-    }
-    public class ServiceBasedTagHelperActivator : ITagHelperActivator {
 {
-        public ServiceBasedTagHelperActivator();

-        public TTagHelper Create<TTagHelper>(ViewContext context) where TTagHelper : ITagHelper;

-    }
-    public class TagHelperComponentManager : ITagHelperComponentManager {
 {
-        public TagHelperComponentManager(IEnumerable<ITagHelperComponent> tagHelperComponents);

-        public ICollection<ITagHelperComponent> Components { get; }

-    }
-    public static class TagHelpersAsServices {
 {
-        public static void AddTagHelpersAsServices(ApplicationPartManager manager, IServiceCollection services);

-    }
-    public readonly struct ViewLocationCacheItem {
 {
-        public ViewLocationCacheItem(Func<IRazorPage> razorPageFactory, string location);

-        public string Location { get; }

-        public Func<IRazorPage> PageFactory { get; }

-    }
-    public readonly struct ViewLocationCacheKey : IEquatable<ViewLocationCacheKey> {
 {
-        public ViewLocationCacheKey(string viewName, bool isMainPage);

-        public ViewLocationCacheKey(string viewName, string controllerName, string areaName, string pageName, bool isMainPage, IReadOnlyDictionary<string, string> values);

-        public string AreaName { get; }

-        public string ControllerName { get; }

-        public bool IsMainPage { get; }

-        public string PageName { get; }

-        public IReadOnlyDictionary<string, string> ViewLocationExpanderValues { get; }

-        public string ViewName { get; }

-        public bool Equals(ViewLocationCacheKey y);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-    }
-    public class ViewLocationCacheResult {
 {
-        public ViewLocationCacheResult(ViewLocationCacheItem view, IReadOnlyList<ViewLocationCacheItem> viewStarts);

-        public ViewLocationCacheResult(IEnumerable<string> searchedLocations);

-        public IEnumerable<string> SearchedLocations { get; }

-        public bool Success { get; }

-        public ViewLocationCacheItem ViewEntry { get; }

-        public IReadOnlyList<ViewLocationCacheItem> ViewStartEntries { get; }

-    }
-    public static class ViewPath {
 {
-        public static string NormalizePath(string path);

-    }
 }
```

