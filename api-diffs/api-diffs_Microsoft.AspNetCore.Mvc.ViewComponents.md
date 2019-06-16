# Microsoft.AspNetCore.Mvc.ViewComponents

``` diff
 namespace Microsoft.AspNetCore.Mvc.ViewComponents {
     public class ContentViewComponentResult : IViewComponentResult {
         public ContentViewComponentResult(string content);
         public string Content { get; }
         public void Execute(ViewComponentContext context);
         public Task ExecuteAsync(ViewComponentContext context);
     }
-    public class DefaultViewComponentActivator : IViewComponentActivator {
 {
-        public DefaultViewComponentActivator(ITypeActivatorCache typeActivatorCache);

-        public virtual object Create(ViewComponentContext context);

-        public virtual void Release(ViewComponentContext context, object viewComponent);

-    }
     public class DefaultViewComponentDescriptorCollectionProvider : IViewComponentDescriptorCollectionProvider {
         public DefaultViewComponentDescriptorCollectionProvider(IViewComponentDescriptorProvider descriptorProvider);
         public ViewComponentDescriptorCollection ViewComponents { get; }
     }
     public class DefaultViewComponentDescriptorProvider : IViewComponentDescriptorProvider {
         public DefaultViewComponentDescriptorProvider(ApplicationPartManager partManager);
         protected virtual IEnumerable<TypeInfo> GetCandidateTypes();
         public virtual IEnumerable<ViewComponentDescriptor> GetViewComponents();
     }
     public class DefaultViewComponentFactory : IViewComponentFactory {
         public DefaultViewComponentFactory(IViewComponentActivator activator);
         public object CreateViewComponent(ViewComponentContext context);
         public void ReleaseViewComponent(ViewComponentContext context, object component);
     }
     public class DefaultViewComponentHelper : IViewComponentHelper, IViewContextAware {
+        public DefaultViewComponentHelper(IViewComponentDescriptorCollectionProvider descriptorProvider, HtmlEncoder htmlEncoder, IViewComponentSelector selector, IViewComponentInvokerFactory invokerFactory, IViewBufferScope viewBufferScope);
-        public DefaultViewComponentHelper(IViewComponentDescriptorCollectionProvider descriptorProvider, HtmlEncoder htmlEncoder, IViewComponentSelector selector, IViewComponentInvokerFactory invokerFactory, IViewBufferScope viewBufferScope);

         public void Contextualize(ViewContext viewContext);
         public Task<IHtmlContent> InvokeAsync(string name, object arguments);
         public Task<IHtmlContent> InvokeAsync(Type componentType, object arguments);
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
     public class DefaultViewComponentSelector : IViewComponentSelector {
         public DefaultViewComponentSelector(IViewComponentDescriptorCollectionProvider descriptorProvider);
         public ViewComponentDescriptor SelectComponent(string componentName);
     }
     public class HtmlContentViewComponentResult : IViewComponentResult {
         public HtmlContentViewComponentResult(IHtmlContent encodedContent);
         public IHtmlContent EncodedContent { get; }
         public void Execute(ViewComponentContext context);
         public Task ExecuteAsync(ViewComponentContext context);
     }
     public interface IViewComponentActivator {
         object Create(ViewComponentContext context);
         void Release(ViewComponentContext context, object viewComponent);
     }
     public interface IViewComponentDescriptorCollectionProvider {
         ViewComponentDescriptorCollection ViewComponents { get; }
     }
     public interface IViewComponentDescriptorProvider {
         IEnumerable<ViewComponentDescriptor> GetViewComponents();
     }
     public interface IViewComponentFactory {
         object CreateViewComponent(ViewComponentContext context);
         void ReleaseViewComponent(ViewComponentContext context, object component);
     }
     public interface IViewComponentInvoker {
         Task InvokeAsync(ViewComponentContext context);
     }
     public interface IViewComponentInvokerFactory {
         IViewComponentInvoker CreateInstance(ViewComponentContext context);
     }
     public interface IViewComponentSelector {
         ViewComponentDescriptor SelectComponent(string componentName);
     }
     public class ServiceBasedViewComponentActivator : IViewComponentActivator {
         public ServiceBasedViewComponentActivator();
         public object Create(ViewComponentContext context);
         public virtual void Release(ViewComponentContext context, object viewComponent);
     }
     public class ViewComponentContext {
         public ViewComponentContext();
         public ViewComponentContext(ViewComponentDescriptor viewComponentDescriptor, IDictionary<string, object> arguments, HtmlEncoder htmlEncoder, ViewContext viewContext, TextWriter writer);
         public IDictionary<string, object> Arguments { get; set; }
         public HtmlEncoder HtmlEncoder { get; set; }
         public ITempDataDictionary TempData { get; }
         public ViewComponentDescriptor ViewComponentDescriptor { get; set; }
         public ViewContext ViewContext { get; set; }
         public ViewDataDictionary ViewData { get; }
         public TextWriter Writer { get; }
     }
     public class ViewComponentContextAttribute : Attribute {
         public ViewComponentContextAttribute();
     }
     public static class ViewComponentConventions {
         public static readonly string ViewComponentSuffix;
         public static string GetComponentFullName(TypeInfo componentType);
         public static string GetComponentName(TypeInfo componentType);
         public static bool IsComponent(TypeInfo typeInfo);
     }
     public class ViewComponentDescriptor {
         public ViewComponentDescriptor();
         public string DisplayName { get; set; }
         public string FullName { get; set; }
         public string Id { get; set; }
         public MethodInfo MethodInfo { get; set; }
         public IReadOnlyList<ParameterInfo> Parameters { get; set; }
         public string ShortName { get; set; }
         public TypeInfo TypeInfo { get; set; }
     }
     public class ViewComponentDescriptorCollection {
         public ViewComponentDescriptorCollection(IEnumerable<ViewComponentDescriptor> items, int version);
         public IReadOnlyList<ViewComponentDescriptor> Items { get; }
         public int Version { get; }
     }
     public class ViewComponentFeature {
         public ViewComponentFeature();
         public IList<TypeInfo> ViewComponents { get; }
     }
     public class ViewComponentFeatureProvider : IApplicationFeatureProvider, IApplicationFeatureProvider<ViewComponentFeature> {
         public ViewComponentFeatureProvider();
         public void PopulateFeature(IEnumerable<ApplicationPart> parts, ViewComponentFeature feature);
     }
     public class ViewViewComponentResult : IViewComponentResult {
         public ViewViewComponentResult();
         public ITempDataDictionary TempData { get; set; }
         public ViewDataDictionary ViewData { get; set; }
         public IViewEngine ViewEngine { get; set; }
         public string ViewName { get; set; }
         public void Execute(ViewComponentContext context);
         public Task ExecuteAsync(ViewComponentContext context);
     }
 }
```

