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
-    public static class PageDirectiveFeature {
 {
-        public static bool TryGetPageDirective(ILogger logger, RazorProjectItem projectItem, out string template);

-    }
+    public abstract class PageLoader : IPageLoader {
+        protected PageLoader();
+        public abstract Task<CompiledPageActionDescriptor> LoadAsync(PageActionDescriptor actionDescriptor);
+        CompiledPageActionDescriptor Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.IPageLoader.Load(PageActionDescriptor actionDescriptor);
+    }
     public class PageResultExecutor : ViewExecutor {
+        public PageResultExecutor(IHttpResponseStreamWriterFactory writerFactory, ICompositeViewEngine compositeViewEngine, IRazorViewEngine razorViewEngine, IRazorPageActivator razorPageActivator, DiagnosticListener diagnosticListener, HtmlEncoder htmlEncoder);
-        public PageResultExecutor(IHttpResponseStreamWriterFactory writerFactory, ICompositeViewEngine compositeViewEngine, IRazorViewEngine razorViewEngine, IRazorPageActivator razorPageActivator, DiagnosticSource diagnosticSource, HtmlEncoder htmlEncoder);

     }
     public class RazorPageAdapter : IModelTypeProvider, IRazorPage {
-        public RazorPageAdapter(RazorPageBase page);

     }
 }
```

