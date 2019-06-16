# Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure

``` diff
 namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure {
-    public class DefaultPageActivatorProvider : IPageActivatorProvider {
 {
-        public DefaultPageActivatorProvider();

-        public virtual Func<PageContext, ViewContext, object> CreateActivator(CompiledPageActionDescriptor actionDescriptor);

-        public virtual Action<PageContext, ViewContext, object> CreateReleaser(CompiledPageActionDescriptor actionDescriptor);

-    }
-    public class DefaultPageFactoryProvider : IPageFactoryProvider {
 {
-        public DefaultPageFactoryProvider(IPageActivatorProvider pageActivator, IModelMetadataProvider metadataProvider, IUrlHelperFactory urlHelperFactory, IJsonHelper jsonHelper, DiagnosticSource diagnosticSource, HtmlEncoder htmlEncoder, IModelExpressionProvider modelExpressionProvider);

-        public virtual Action<PageContext, ViewContext, object> CreatePageDisposer(CompiledPageActionDescriptor descriptor);

-        public virtual Func<PageContext, ViewContext, object> CreatePageFactory(CompiledPageActionDescriptor actionDescriptor);

-    }
-    public class DefaultPageHandlerMethodSelector : IPageHandlerMethodSelector {
 {
-        public DefaultPageHandlerMethodSelector();

-        public DefaultPageHandlerMethodSelector(IOptions<RazorPagesOptions> options);

-        public HandlerMethodDescriptor Select(PageContext context);

-    }
-    public class DefaultPageModelActivatorProvider : IPageModelActivatorProvider {
 {
-        public DefaultPageModelActivatorProvider();

-        public virtual Func<PageContext, object> CreateActivator(CompiledPageActionDescriptor actionDescriptor);

-        public virtual Action<PageContext, object> CreateReleaser(CompiledPageActionDescriptor actionDescriptor);

-    }
-    public class DefaultPageModelFactoryProvider : IPageModelFactoryProvider {
 {
-        public DefaultPageModelFactoryProvider(IPageModelActivatorProvider modelActivator);

-        public virtual Action<PageContext, object> CreateModelDisposer(CompiledPageActionDescriptor descriptor);

-        public virtual Func<PageContext, object> CreateModelFactory(CompiledPageActionDescriptor descriptor);

-    }
     public class HandlerMethodDescriptor {
         public HandlerMethodDescriptor();
         public string HttpMethod { get; set; }
         public MethodInfo MethodInfo { get; set; }
         public string Name { get; set; }
         public IList<HandlerParameterDescriptor> Parameters { get; set; }
     }
     public class HandlerParameterDescriptor : ParameterDescriptor, IParameterInfoParameterDescriptor {
         public HandlerParameterDescriptor();
         public ParameterInfo ParameterInfo { get; set; }
     }
     public interface IPageHandlerMethodSelector {
         HandlerMethodDescriptor Select(PageContext context);
     }
     public interface IPageLoader {
         CompiledPageActionDescriptor Load(PageActionDescriptor actionDescriptor);
     }
     public class PageActionDescriptorProvider : IActionDescriptorProvider {
         public PageActionDescriptorProvider(IEnumerable<IPageRouteModelProvider> pageRouteModelProviders, IOptions<MvcOptions> mvcOptionsAccessor, IOptions<RazorPagesOptions> pagesOptionsAccessor);
         public int Order { get; set; }
         protected IList<PageRouteModel> BuildModel();
         public void OnProvidersExecuted(ActionDescriptorProviderContext context);
         public void OnProvidersExecuting(ActionDescriptorProviderContext context);
     }
-    public abstract class PageArgumentBinder {
 {
-        protected PageArgumentBinder();

-        protected abstract Task<ModelBindingResult> BindAsync(PageContext context, object value, string name, Type type);

-        public Task<object> BindModelAsync(PageContext context, Type type, object @default, string name);

-        public Task<TModel> BindModelAsync<TModel>(PageContext context, string name);

-        public Task<TModel> BindModelAsync<TModel>(PageContext context, TModel @default, string name);

-        public Task<bool> TryUpdateModelAsync<TModel>(PageContext context, TModel value);

-        public Task<bool> TryUpdateModelAsync<TModel>(PageContext context, TModel value, string name);

-    }
     public class PageBoundPropertyDescriptor : ParameterDescriptor, IPropertyInfoParameterDescriptor {
         public PageBoundPropertyDescriptor();
         PropertyInfo Microsoft.AspNetCore.Mvc.Infrastructure.IPropertyInfoParameterDescriptor.PropertyInfo { get; }
         public PropertyInfo Property { get; set; }
     }
-    public static class PageDirectiveFeature {
 {
-        public static bool TryGetPageDirective(ILogger logger, RazorProjectItem projectItem, out string template);

-    }
+    public abstract class PageLoader : IPageLoader {
+        protected PageLoader();
+        public abstract Task<CompiledPageActionDescriptor> LoadAsync(PageActionDescriptor actionDescriptor);
+        CompiledPageActionDescriptor Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.IPageLoader.Load(PageActionDescriptor actionDescriptor);
+    }
     public class PageModelAttribute : Attribute {
         public PageModelAttribute();
     }
     public class PageResultExecutor : ViewExecutor {
+        public PageResultExecutor(IHttpResponseStreamWriterFactory writerFactory, ICompositeViewEngine compositeViewEngine, IRazorViewEngine razorViewEngine, IRazorPageActivator razorPageActivator, DiagnosticListener diagnosticListener, HtmlEncoder htmlEncoder);
-        public PageResultExecutor(IHttpResponseStreamWriterFactory writerFactory, ICompositeViewEngine compositeViewEngine, IRazorViewEngine razorViewEngine, IRazorPageActivator razorPageActivator, DiagnosticSource diagnosticSource, HtmlEncoder htmlEncoder);

         public virtual Task ExecuteAsync(PageContext pageContext, PageResult result);
     }
     public class PageViewLocationExpander : IViewLocationExpander {
         public PageViewLocationExpander();
         public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations);
         public void PopulateValues(ViewLocationExpanderContext context);
     }
     public class RazorPageAdapter : IModelTypeProvider, IRazorPage {
-        public RazorPageAdapter(RazorPageBase page);

         public RazorPageAdapter(RazorPageBase page, Type modelType);
         public IHtmlContent BodyContent { get; set; }
         public bool IsLayoutBeingRendered { get; set; }
         public string Layout { get; set; }
         public string Path { get; set; }
         public IDictionary<string, RenderAsyncDelegate> PreviousSectionWriters { get; set; }
         public IDictionary<string, RenderAsyncDelegate> SectionWriters { get; }
         public ViewContext ViewContext { get; set; }
         public void EnsureRenderedBodyOrSections();
         public Task ExecuteAsync();
     }
     public class RazorPageAttribute : RazorViewAttribute {
         public RazorPageAttribute(string path, Type viewType, string routeTemplate);
         public string RouteTemplate { get; }
     }
     public class ServiceBasedPageModelActivatorProvider : IPageModelActivatorProvider {
         public ServiceBasedPageModelActivatorProvider();
         public Func<PageContext, object> CreateActivator(CompiledPageActionDescriptor descriptor);
         public Action<PageContext, object> CreateReleaser(CompiledPageActionDescriptor descriptor);
     }
 }
```

