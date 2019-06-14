# Microsoft.AspNetCore.Mvc.RazorPages.Internal

``` diff
-namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal {
 {
-    public class AuthorizationPageApplicationModelProvider : IPageApplicationModelProvider {
 {
-        public AuthorizationPageApplicationModelProvider(IAuthorizationPolicyProvider policyProvider);

-        public int Order { get; }

-        public void OnProvidersExecuted(PageApplicationModelProviderContext context);

-        public void OnProvidersExecuting(PageApplicationModelProviderContext context);

-    }
-    public static class CompiledPageActionDescriptorBuilder {
 {
-        public static CompiledPageActionDescriptor Build(PageApplicationModel applicationModel, FilterCollection globalFilters);

-    }
-    public class CompiledPageRouteModelProvider : IPageRouteModelProvider {
 {
-        public CompiledPageRouteModelProvider(ApplicationPartManager applicationManager, IOptions<RazorPagesOptions> pagesOptionsAccessor, RazorProjectEngine razorProjectEngine, ILogger<CompiledPageRouteModelProvider> logger);

-        public int Order { get; }

-        protected virtual ViewsFeature GetViewFeature(ApplicationPartManager applicationManager);

-        public void OnProvidersExecuted(PageRouteModelProviderContext context);

-        public void OnProvidersExecuting(PageRouteModelProviderContext context);

-    }
-    public class DefaultPageArgumentBinder : PageArgumentBinder {
 {
-        public DefaultPageArgumentBinder(ParameterBinder binder);

-        protected override Task<ModelBindingResult> BindAsync(PageContext pageContext, object value, string name, Type type);

-    }
-    public class DefaultPageLoader : IPageLoader {
 {
-        public DefaultPageLoader(IEnumerable<IPageApplicationModelProvider> applicationModelProviders, IViewCompilerProvider viewCompilerProvider, IOptions<RazorPagesOptions> pageOptions, IOptions<MvcOptions> mvcOptions);

-        public CompiledPageActionDescriptor Load(PageActionDescriptor actionDescriptor);

-    }
-    public static class ExecutorFactory {
 {
-        public static PageHandlerExecutorDelegate CreateExecutor(HandlerMethodDescriptor handlerDescriptor);

-    }
-    public class PageActionDescriptorChangeProvider : IActionDescriptorChangeProvider {
 {
-        public PageActionDescriptorChangeProvider(RazorTemplateEngine templateEngine, IRazorViewEngineFileProviderAccessor fileProviderAccessor, IOptions<RazorPagesOptions> razorPagesOptions, IOptions<RazorViewEngineOptions> razorViewEngineOptions);

-        public IChangeToken GetChangeToken();

-    }
-    public class PageActionInvoker : ResourceInvoker, IActionInvoker {
 {
-        public PageActionInvoker(IPageHandlerMethodSelector handlerMethodSelector, DiagnosticSource diagnosticSource, ILogger logger, IActionResultTypeMapper mapper, PageContext pageContext, IFilterMetadata[] filterMetadata, PageActionInvokerCacheEntry cacheEntry, ParameterBinder parameterBinder, ITempDataDictionaryFactory tempDataFactory, HtmlHelperOptions htmlHelperOptions);

-        protected override Task InvokeInnerFilterAsync();

-        protected override Task InvokeResultAsync(IActionResult result);

-        protected override void ReleaseResources();

-    }
-    public class PageActionInvokerCacheEntry {
 {
-        public PageActionInvokerCacheEntry(CompiledPageActionDescriptor actionDescriptor, Func<IModelMetadataProvider, ModelStateDictionary, ViewDataDictionary> viewDataFactory, Func<PageContext, ViewContext, object> pageFactory, Action<PageContext, ViewContext, object> releasePage, Func<PageContext, object> modelFactory, Action<PageContext, object> releaseModel, Func<PageContext, object, Task> propertyBinder, PageHandlerExecutorDelegate[] handlerExecutors, PageHandlerBinderDelegate[] handlerBinders, IReadOnlyList<Func<IRazorPage>> viewStartFactories, FilterItem[] cacheableFilters);

-        public CompiledPageActionDescriptor ActionDescriptor { get; }

-        public FilterItem[] CacheableFilters { get; }

-        public PageHandlerBinderDelegate[] HandlerBinders { get; }

-        public PageHandlerExecutorDelegate[] HandlerExecutors { get; }

-        public Func<PageContext, object> ModelFactory { get; }

-        public Func<PageContext, ViewContext, object> PageFactory { get; }

-        public Func<PageContext, object, Task> PropertyBinder { get; }

-        public Action<PageContext, object> ReleaseModel { get; }

-        public Action<PageContext, ViewContext, object> ReleasePage { get; }

-        public Func<IModelMetadataProvider, ModelStateDictionary, ViewDataDictionary> ViewDataFactory { get; }

-        public IReadOnlyList<Func<IRazorPage>> ViewStartFactories { get; }

-    }
-    public class PageActionInvokerProvider : IActionInvokerProvider {
 {
-        public PageActionInvokerProvider(IPageLoader loader, IPageFactoryProvider pageFactoryProvider, IPageModelFactoryProvider modelFactoryProvider, IRazorPageFactoryProvider razorPageFactoryProvider, IActionDescriptorCollectionProvider collectionProvider, IEnumerable<IFilterProvider> filterProviders, ParameterBinder parameterBinder, IModelMetadataProvider modelMetadataProvider, IModelBinderFactory modelBinderFactory, ITempDataDictionaryFactory tempDataFactory, IOptions<MvcOptions> mvcOptions, IOptions<HtmlHelperOptions> htmlHelperOptions, IPageHandlerMethodSelector selector, RazorProjectFileSystem razorFileSystem, DiagnosticSource diagnosticSource, ILoggerFactory loggerFactory, IActionResultTypeMapper mapper);

-        public int Order { get; }

-        public void OnProvidersExecuted(ActionInvokerProviderContext context);

-        public void OnProvidersExecuting(ActionInvokerProviderContext context);

-    }
-    public delegate Task PageHandlerBinderDelegate(PageContext pageContext, IDictionary<string, object> arguments);

-    public delegate Task<IActionResult> PageHandlerExecutorDelegate(object handler, object[] arguments);

-    public class PageHandlerPageFilter : IAsyncPageFilter, IFilterMetadata, IOrderedFilter {
 {
-        public PageHandlerPageFilter();

-        public int Order { get; }

-        public Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next);

-        public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context);

-    }
-    public class PageHandlerResultFilter : IAsyncResultFilter, IFilterMetadata, IOrderedFilter {
 {
-        public PageHandlerResultFilter();

-        public int Order { get; }

-        public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next);

-    }
-    public class RazorPagesRazorViewEngineOptionsSetup : IConfigureOptions<RazorViewEngineOptions> {
 {
-        public RazorPagesRazorViewEngineOptionsSetup(IOptions<RazorPagesOptions> pagesOptions);

-        public void Configure(RazorViewEngineOptions options);

-    }
-    public class RazorProjectPageRouteModelProvider : IPageRouteModelProvider {
 {
-        public RazorProjectPageRouteModelProvider(RazorProjectFileSystem razorFileSystem, IOptions<RazorPagesOptions> pagesOptionsAccessor, ILoggerFactory loggerFactory);

-        public int Order { get; }

-        public void OnProvidersExecuted(PageRouteModelProviderContext context);

-        public void OnProvidersExecuting(PageRouteModelProviderContext context);

-    }
-    public class ResponseCacheFilter : IFilterMetadata, IPageFilter, IResponseCacheFilter {
 {
-        public ResponseCacheFilter(CacheProfile cacheProfile, ILoggerFactory loggerFactory);

-        public int Duration { get; set; }

-        public ResponseCacheLocation Location { get; set; }

-        public bool NoStore { get; set; }

-        public string VaryByHeader { get; set; }

-        public string[] VaryByQueryKeys { get; set; }

-        public void OnPageHandlerExecuted(PageHandlerExecutedContext context);

-        public void OnPageHandlerExecuting(PageHandlerExecutingContext context);

-        public void OnPageHandlerSelected(PageHandlerSelectedContext context);

-    }
-    public class ResponseCacheFilterApplicationModelProvider : IPageApplicationModelProvider {
 {
-        public ResponseCacheFilterApplicationModelProvider(IOptions<MvcOptions> mvcOptionsAccessor, ILoggerFactory loggerFactory);

-        public int Order { get; }

-        public void OnProvidersExecuted(PageApplicationModelProviderContext context);

-        public void OnProvidersExecuting(PageApplicationModelProviderContext context);

-    }
-}
```

