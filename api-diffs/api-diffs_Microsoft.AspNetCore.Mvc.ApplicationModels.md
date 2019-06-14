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

