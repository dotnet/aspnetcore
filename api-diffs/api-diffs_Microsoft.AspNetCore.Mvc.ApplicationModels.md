# Microsoft.AspNetCore.Mvc.ApplicationModels

``` diff
 namespace Microsoft.AspNetCore.Mvc.ApplicationModels {
     public class ActionModel : IApiExplorerModel, ICommonModel, IFilterModel, IPropertyModel {
+        public IOutboundParameterTransformer RouteParameterTransformer { get; set; }
     }
+    public interface IPageApplicationModelPartsProvider {
+        PageHandlerModel CreateHandlerModel(MethodInfo method);
+        PageParameterModel CreateParameterModel(ParameterInfo parameter);
+        PagePropertyModel CreatePropertyModel(PropertyInfo property);
+        bool IsHandler(MethodInfo methodInfo);
+    }
     public class PageApplicationModel {
+        public IList<object> EndpointMetadata { get; }
     }
     public class PageParameterModel : ParameterModelBase, IBindingModel, ICommonModel, IPropertyModel {
+        IReadOnlyList<object> Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.Attributes { get; }
+        IDictionary<object, object> Microsoft.AspNetCore.Mvc.ApplicationModels.IPropertyModel.Properties { get; }
-        IReadOnlyList<object> Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.get_Attributes();
+        get;
-        IDictionary<object, object> Microsoft.AspNetCore.Mvc.ApplicationModels.IPropertyModel.get_Properties();
+        get;
     }
     public class PagePropertyModel : ParameterModelBase, ICommonModel, IPropertyModel {
+        IReadOnlyList<object> Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.Attributes { get; }
+        IDictionary<object, object> Microsoft.AspNetCore.Mvc.ApplicationModels.IPropertyModel.Properties { get; }
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
+        public IOutboundParameterTransformer RouteParameterTransformer { get; set; }
     }
 }
```

