# Microsoft.AspNetCore.Mvc.ApplicationModels

``` diff
 namespace Microsoft.AspNetCore.Mvc.ApplicationModels {
     public class ActionModel : IApiExplorerModel, ICommonModel, IFilterModel, IPropertyModel {
         public ActionModel(ActionModel other);
         public ActionModel(MethodInfo actionMethod, IReadOnlyList<object> attributes);
         public MethodInfo ActionMethod { get; }
         public string ActionName { get; set; }
         public ApiExplorerModel ApiExplorer { get; set; }
         public IReadOnlyList<object> Attributes { get; }
         public ControllerModel Controller { get; set; }
         public string DisplayName { get; }
         public IList<IFilterMetadata> Filters { get; }
         MemberInfo Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.MemberInfo { get; }
         string Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.Name { get; }
         public IList<ParameterModel> Parameters { get; }
         public IDictionary<object, object> Properties { get; }
+        public IOutboundParameterTransformer RouteParameterTransformer { get; set; }
         public IDictionary<string, string> RouteValues { get; }
         public IList<SelectorModel> Selectors { get; }
     }
     public class ApiConventionApplicationModelConvention : IActionModelConvention {
         public ApiConventionApplicationModelConvention(ProducesErrorResponseTypeAttribute defaultErrorResponseType);
         public ProducesErrorResponseTypeAttribute DefaultErrorResponseType { get; }
         public void Apply(ActionModel action);
         protected virtual bool ShouldApply(ActionModel action);
     }
     public class ApiExplorerModel {
         public ApiExplorerModel();
         public ApiExplorerModel(ApiExplorerModel other);
         public string GroupName { get; set; }
         public Nullable<bool> IsVisible { get; set; }
     }
     public class ApiVisibilityConvention : IActionModelConvention {
         public ApiVisibilityConvention();
         public void Apply(ActionModel action);
         protected virtual bool ShouldApply(ActionModel action);
     }
     public class ApplicationModel : IApiExplorerModel, IFilterModel, IPropertyModel {
         public ApplicationModel();
         public ApiExplorerModel ApiExplorer { get; set; }
         public IList<ControllerModel> Controllers { get; }
         public IList<IFilterMetadata> Filters { get; }
         public IDictionary<object, object> Properties { get; }
     }
     public class ApplicationModelProviderContext {
         public ApplicationModelProviderContext(IEnumerable<TypeInfo> controllerTypes);
         public IEnumerable<TypeInfo> ControllerTypes { get; }
         public ApplicationModel Result { get; }
     }
     public class AttributeRouteModel {
         public AttributeRouteModel();
         public AttributeRouteModel(AttributeRouteModel other);
         public AttributeRouteModel(IRouteTemplateProvider templateProvider);
         public IRouteTemplateProvider Attribute { get; }
         public bool IsAbsoluteTemplate { get; }
         public string Name { get; set; }
         public Nullable<int> Order { get; set; }
         public bool SuppressLinkGeneration { get; set; }
         public bool SuppressPathMatching { get; set; }
         public string Template { get; set; }
         public static AttributeRouteModel CombineAttributeRouteModel(AttributeRouteModel left, AttributeRouteModel right);
         public static string CombineTemplates(string prefix, string template);
         public static bool IsOverridePattern(string template);
         public static string ReplaceTokens(string template, IDictionary<string, string> values);
         public static string ReplaceTokens(string template, IDictionary<string, string> values, IOutboundParameterTransformer routeTokenTransformer);
     }
     public class ClientErrorResultFilterConvention : IActionModelConvention {
         public ClientErrorResultFilterConvention();
         public void Apply(ActionModel action);
         protected virtual bool ShouldApply(ActionModel action);
     }
     public class ConsumesConstraintForFormFileParameterConvention : IActionModelConvention {
         public ConsumesConstraintForFormFileParameterConvention();
         public void Apply(ActionModel action);
         protected virtual bool ShouldApply(ActionModel action);
     }
     public class ControllerModel : IApiExplorerModel, ICommonModel, IFilterModel, IPropertyModel {
         public ControllerModel(ControllerModel other);
         public ControllerModel(TypeInfo controllerType, IReadOnlyList<object> attributes);
         public IList<ActionModel> Actions { get; }
         public ApiExplorerModel ApiExplorer { get; set; }
         public ApplicationModel Application { get; set; }
         public IReadOnlyList<object> Attributes { get; }
         public string ControllerName { get; set; }
         public IList<PropertyModel> ControllerProperties { get; }
         public TypeInfo ControllerType { get; }
         public string DisplayName { get; }
         public IList<IFilterMetadata> Filters { get; }
         MemberInfo Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.MemberInfo { get; }
         string Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.Name { get; }
         public IDictionary<object, object> Properties { get; }
         public IDictionary<string, string> RouteValues { get; }
         public IList<SelectorModel> Selectors { get; }
     }
     public interface IActionModelConvention {
         void Apply(ActionModel action);
     }
     public interface IApiExplorerModel {
         ApiExplorerModel ApiExplorer { get; set; }
     }
     public interface IApplicationModelConvention {
         void Apply(ApplicationModel application);
     }
     public interface IApplicationModelProvider {
         int Order { get; }
         void OnProvidersExecuted(ApplicationModelProviderContext context);
         void OnProvidersExecuting(ApplicationModelProviderContext context);
     }
     public interface IBindingModel {
         BindingInfo BindingInfo { get; set; }
     }
     public interface ICommonModel : IPropertyModel {
         IReadOnlyList<object> Attributes { get; }
         MemberInfo MemberInfo { get; }
         string Name { get; }
     }
     public interface IControllerModelConvention {
         void Apply(ControllerModel controller);
     }
     public interface IFilterModel {
         IList<IFilterMetadata> Filters { get; }
     }
     public class InferParameterBindingInfoConvention : IActionModelConvention {
         public InferParameterBindingInfoConvention(IModelMetadataProvider modelMetadataProvider);
         public void Apply(ActionModel action);
         protected virtual bool ShouldApply(ActionModel action);
     }
     public class InvalidModelStateFilterConvention : IActionModelConvention {
         public InvalidModelStateFilterConvention();
         public void Apply(ActionModel action);
         protected virtual bool ShouldApply(ActionModel action);
     }
     public interface IPageApplicationModelConvention : IPageConvention {
         void Apply(PageApplicationModel model);
     }
+    public interface IPageApplicationModelPartsProvider {
+        PageHandlerModel CreateHandlerModel(MethodInfo method);
+        PageParameterModel CreateParameterModel(ParameterInfo parameter);
+        PagePropertyModel CreatePropertyModel(PropertyInfo property);
+        bool IsHandler(MethodInfo methodInfo);
+    }
     public interface IPageApplicationModelProvider {
         int Order { get; }
         void OnProvidersExecuted(PageApplicationModelProviderContext context);
         void OnProvidersExecuting(PageApplicationModelProviderContext context);
     }
     public interface IPageConvention
     public interface IPageHandlerModelConvention : IPageConvention {
         void Apply(PageHandlerModel model);
     }
     public interface IPageRouteModelConvention : IPageConvention {
         void Apply(PageRouteModel model);
     }
     public interface IPageRouteModelProvider {
         int Order { get; }
         void OnProvidersExecuted(PageRouteModelProviderContext context);
         void OnProvidersExecuting(PageRouteModelProviderContext context);
     }
     public interface IParameterModelBaseConvention {
         void Apply(ParameterModelBase parameter);
     }
     public interface IParameterModelConvention {
         void Apply(ParameterModel parameter);
     }
     public interface IPropertyModel {
         IDictionary<object, object> Properties { get; }
     }
     public class PageApplicationModel {
         public PageApplicationModel(PageApplicationModel other);
         public PageApplicationModel(PageActionDescriptor actionDescriptor, TypeInfo handlerType, IReadOnlyList<object> handlerAttributes);
         public PageApplicationModel(PageActionDescriptor actionDescriptor, TypeInfo declaredModelType, TypeInfo handlerType, IReadOnlyList<object> handlerAttributes);
         public PageActionDescriptor ActionDescriptor { get; }
         public string AreaName { get; }
         public TypeInfo DeclaredModelType { get; }
+        public IList<object> EndpointMetadata { get; }
         public IList<IFilterMetadata> Filters { get; }
         public IList<PageHandlerModel> HandlerMethods { get; }
         public IList<PagePropertyModel> HandlerProperties { get; }
         public TypeInfo HandlerType { get; }
         public IReadOnlyList<object> HandlerTypeAttributes { get; }
         public TypeInfo ModelType { get; set; }
         public TypeInfo PageType { get; set; }
         public IDictionary<object, object> Properties { get; }
         public string RelativePath { get; }
         public string RouteTemplate { get; }
         public string ViewEnginePath { get; }
     }
     public class PageApplicationModelProviderContext {
         public PageApplicationModelProviderContext(PageActionDescriptor descriptor, TypeInfo pageTypeInfo);
         public PageActionDescriptor ActionDescriptor { get; }
         public PageApplicationModel PageApplicationModel { get; set; }
         public TypeInfo PageType { get; }
     }
     public class PageConventionCollection : Collection<IPageConvention> {
         public PageConventionCollection();
         public PageConventionCollection(IList<IPageConvention> conventions);
         public IPageApplicationModelConvention AddAreaFolderApplicationModelConvention(string areaName, string folderPath, Action<PageApplicationModel> action);
         public IPageRouteModelConvention AddAreaFolderRouteModelConvention(string areaName, string folderPath, Action<PageRouteModel> action);
         public IPageApplicationModelConvention AddAreaPageApplicationModelConvention(string areaName, string pageName, Action<PageApplicationModel> action);
         public IPageRouteModelConvention AddAreaPageRouteModelConvention(string areaName, string pageName, Action<PageRouteModel> action);
         public IPageApplicationModelConvention AddFolderApplicationModelConvention(string folderPath, Action<PageApplicationModel> action);
         public IPageRouteModelConvention AddFolderRouteModelConvention(string folderPath, Action<PageRouteModel> action);
         public IPageApplicationModelConvention AddPageApplicationModelConvention(string pageName, Action<PageApplicationModel> action);
         public IPageRouteModelConvention AddPageRouteModelConvention(string pageName, Action<PageRouteModel> action);
         public void RemoveType(Type pageConventionType);
         public void RemoveType<TPageConvention>() where TPageConvention : IPageConvention;
     }
     public class PageHandlerModel : ICommonModel, IPropertyModel {
         public PageHandlerModel(PageHandlerModel other);
         public PageHandlerModel(MethodInfo handlerMethod, IReadOnlyList<object> attributes);
         public IReadOnlyList<object> Attributes { get; }
         public string HandlerName { get; set; }
         public string HttpMethod { get; set; }
         public MethodInfo MethodInfo { get; }
         MemberInfo Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.MemberInfo { get; }
         public string Name { get; set; }
         public PageApplicationModel Page { get; set; }
         public IList<PageParameterModel> Parameters { get; }
         public IDictionary<object, object> Properties { get; }
     }
     public class PageParameterModel : ParameterModelBase, IBindingModel, ICommonModel, IPropertyModel {
         public PageParameterModel(PageParameterModel other);
         public PageParameterModel(ParameterInfo parameterInfo, IReadOnlyList<object> attributes);
         public PageHandlerModel Handler { get; set; }
+        IReadOnlyList<object> Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.Attributes { get; }
         MemberInfo Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.MemberInfo { get; }
+        IDictionary<object, object> Microsoft.AspNetCore.Mvc.ApplicationModels.IPropertyModel.Properties { get; }
         public ParameterInfo ParameterInfo { get; }
         public string ParameterName { get; set; }
-        IReadOnlyList<object> Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.get_Attributes();
+        get;
-        IDictionary<object, object> Microsoft.AspNetCore.Mvc.ApplicationModels.IPropertyModel.get_Properties();
+        get;
     }
     public class PagePropertyModel : ParameterModelBase, ICommonModel, IPropertyModel {
         public PagePropertyModel(PagePropertyModel other);
         public PagePropertyModel(PropertyInfo propertyInfo, IReadOnlyList<object> attributes);
+        IReadOnlyList<object> Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.Attributes { get; }
         MemberInfo Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.MemberInfo { get; }
+        IDictionary<object, object> Microsoft.AspNetCore.Mvc.ApplicationModels.IPropertyModel.Properties { get; }
         public PageApplicationModel Page { get; set; }
         public PropertyInfo PropertyInfo { get; }
         public string PropertyName { get; set; }
-        IReadOnlyList<object> Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.get_Attributes();
+        get;
-        IDictionary<object, object> Microsoft.AspNetCore.Mvc.ApplicationModels.IPropertyModel.get_Properties();
+        get;
     }
+    public sealed class PageRouteMetadata {
+        public PageRouteMetadata(string pageRoute, string routeTemplate);
+        public string PageRoute { get; }
+        public string RouteTemplate { get; }
+    }
     public class PageRouteModel {
         public PageRouteModel(PageRouteModel other);
         public PageRouteModel(string relativePath, string viewEnginePath);
         public PageRouteModel(string relativePath, string viewEnginePath, string areaName);
         public string AreaName { get; }
         public IDictionary<object, object> Properties { get; }
         public string RelativePath { get; }
+        public IOutboundParameterTransformer RouteParameterTransformer { get; set; }
         public IDictionary<string, string> RouteValues { get; }
         public IList<SelectorModel> Selectors { get; }
         public string ViewEnginePath { get; }
     }
     public class PageRouteModelProviderContext {
         public PageRouteModelProviderContext();
         public IList<PageRouteModel> RouteModels { get; }
     }
     public class PageRouteTransformerConvention : IPageConvention, IPageRouteModelConvention {
         public PageRouteTransformerConvention(IOutboundParameterTransformer parameterTransformer);
         public void Apply(PageRouteModel model);
         protected virtual bool ShouldApply(PageRouteModel action);
     }
     public class ParameterModel : ParameterModelBase, ICommonModel, IPropertyModel {
         public ParameterModel(ParameterModel other);
         public ParameterModel(ParameterInfo parameterInfo, IReadOnlyList<object> attributes);
         public ActionModel Action { get; set; }
         public new IReadOnlyList<object> Attributes { get; }
         public string DisplayName { get; }
         MemberInfo Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.MemberInfo { get; }
         public ParameterInfo ParameterInfo { get; }
         public string ParameterName { get; set; }
         public new IDictionary<object, object> Properties { get; }
     }
     public abstract class ParameterModelBase : IBindingModel {
         protected ParameterModelBase(ParameterModelBase other);
         protected ParameterModelBase(Type parameterType, IReadOnlyList<object> attributes);
         public IReadOnlyList<object> Attributes { get; }
         public BindingInfo BindingInfo { get; set; }
         public string Name { get; protected set; }
         public Type ParameterType { get; }
         public IDictionary<object, object> Properties { get; }
     }
     public class PropertyModel : ParameterModelBase, IBindingModel, ICommonModel, IPropertyModel {
         public PropertyModel(PropertyModel other);
         public PropertyModel(PropertyInfo propertyInfo, IReadOnlyList<object> attributes);
         public new IReadOnlyList<object> Attributes { get; }
         public ControllerModel Controller { get; set; }
         MemberInfo Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.MemberInfo { get; }
         public new IDictionary<object, object> Properties { get; }
         public PropertyInfo PropertyInfo { get; }
         public string PropertyName { get; set; }
     }
     public class RouteTokenTransformerConvention : IActionModelConvention {
         public RouteTokenTransformerConvention(IOutboundParameterTransformer parameterTransformer);
         public void Apply(ActionModel action);
         protected virtual bool ShouldApply(ActionModel action);
     }
     public class SelectorModel {
         public SelectorModel();
         public SelectorModel(SelectorModel other);
         public IList<IActionConstraintMetadata> ActionConstraints { get; }
         public AttributeRouteModel AttributeRouteModel { get; set; }
         public IList<object> EndpointMetadata { get; }
     }
 }
```

