# Microsoft.AspNetCore.Mvc.ViewComponents

``` diff
 namespace Microsoft.AspNetCore.Mvc.ViewComponents {
-    public class DefaultViewComponentActivator : IViewComponentActivator {
 {
-        public DefaultViewComponentActivator(ITypeActivatorCache typeActivatorCache);

-        public virtual object Create(ViewComponentContext context);

-        public virtual void Release(ViewComponentContext context, object viewComponent);

-    }
     public class DefaultViewComponentHelper : IViewComponentHelper, IViewContextAware {
+        public DefaultViewComponentHelper(IViewComponentDescriptorCollectionProvider descriptorProvider, HtmlEncoder htmlEncoder, IViewComponentSelector selector, IViewComponentInvokerFactory invokerFactory, IViewBufferScope viewBufferScope);
-        public DefaultViewComponentHelper(IViewComponentDescriptorCollectionProvider descriptorProvider, HtmlEncoder htmlEncoder, IViewComponentSelector selector, IViewComponentInvokerFactory invokerFactory, IViewBufferScope viewBufferScope);

     }
-    public class DefaultViewComponentInvoker : IViewComponentInvoker {
 {
-        public DefaultViewComponentInvoker(IViewComponentFactory viewComponentFactory, ViewComponentInvokerCache viewComponentInvokerCache, DiagnosticSource diagnosticSource, ILogger logger);

-        public Task InvokeAsync(ViewComponentContext context);

-    }
-    public class DefaultViewComponentInvokerFactory : IViewComponentInvokerFactory {
 {
-        public DefaultViewComponentInvokerFactory(IViewComponentFactory viewComponentFactory, ViewComponentInvokerCache viewComponentInvokerCache, DiagnosticSource diagnosticSource, ILoggerFactory loggerFactory);

-        public IViewComponentInvoker CreateInstance(ViewComponentContext context);

-    }
 }
```

