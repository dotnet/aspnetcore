# Microsoft.AspNetCore.Mvc.Abstractions

``` diff
 namespace Microsoft.AspNetCore.Mvc.Abstractions {
     public class ActionDescriptor {
         public ActionDescriptor();
         public IList<IActionConstraintMetadata> ActionConstraints { get; set; }
         public AttributeRouteInfo AttributeRouteInfo { get; set; }
         public IList<ParameterDescriptor> BoundProperties { get; set; }
         public virtual string DisplayName { get; set; }
         public IList<object> EndpointMetadata { get; set; }
         public IList<FilterDescriptor> FilterDescriptors { get; set; }
         public string Id { get; }
         public IList<ParameterDescriptor> Parameters { get; set; }
         public IDictionary<object, object> Properties { get; set; }
         public IDictionary<string, string> RouteValues { get; set; }
     }
     public static class ActionDescriptorExtensions {
         public static T GetProperty<T>(this ActionDescriptor actionDescriptor);
         public static void SetProperty<T>(this ActionDescriptor actionDescriptor, T value);
     }
     public class ActionDescriptorProviderContext {
         public ActionDescriptorProviderContext();
         public IList<ActionDescriptor> Results { get; }
     }
     public class ActionInvokerProviderContext {
         public ActionInvokerProviderContext(ActionContext actionContext);
         public ActionContext ActionContext { get; }
         public IActionInvoker Result { get; set; }
     }
     public interface IActionDescriptorProvider {
         int Order { get; }
         void OnProvidersExecuted(ActionDescriptorProviderContext context);
         void OnProvidersExecuting(ActionDescriptorProviderContext context);
     }
     public interface IActionInvoker {
         Task InvokeAsync();
     }
     public interface IActionInvokerProvider {
         int Order { get; }
         void OnProvidersExecuted(ActionInvokerProviderContext context);
         void OnProvidersExecuting(ActionInvokerProviderContext context);
     }
     public class ParameterDescriptor {
         public ParameterDescriptor();
         public BindingInfo BindingInfo { get; set; }
         public string Name { get; set; }
         public Type ParameterType { get; set; }
     }
 }
```

