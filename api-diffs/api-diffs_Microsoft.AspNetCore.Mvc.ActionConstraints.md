# Microsoft.AspNetCore.Mvc.ActionConstraints

``` diff
 namespace Microsoft.AspNetCore.Mvc.ActionConstraints {
     public class ActionConstraintContext {
         public ActionConstraintContext();
         public IReadOnlyList<ActionSelectorCandidate> Candidates { get; set; }
         public ActionSelectorCandidate CurrentCandidate { get; set; }
         public RouteContext RouteContext { get; set; }
     }
     public class ActionConstraintItem {
         public ActionConstraintItem(IActionConstraintMetadata metadata);
         public IActionConstraint Constraint { get; set; }
         public bool IsReusable { get; set; }
         public IActionConstraintMetadata Metadata { get; }
     }
     public class ActionConstraintProviderContext {
         public ActionConstraintProviderContext(HttpContext context, ActionDescriptor action, IList<ActionConstraintItem> items);
         public ActionDescriptor Action { get; }
         public HttpContext HttpContext { get; }
         public IList<ActionConstraintItem> Results { get; }
     }
     public abstract class ActionMethodSelectorAttribute : Attribute, IActionConstraint, IActionConstraintMetadata {
         protected ActionMethodSelectorAttribute();
         public int Order { get; set; }
         public bool Accept(ActionConstraintContext context);
         public abstract bool IsValidForRequest(RouteContext routeContext, ActionDescriptor action);
     }
     public readonly struct ActionSelectorCandidate {
         public ActionSelectorCandidate(ActionDescriptor action, IReadOnlyList<IActionConstraint> constraints);
         public ActionDescriptor Action { get; }
         public IReadOnlyList<IActionConstraint> Constraints { get; }
     }
     public interface IActionConstraint : IActionConstraintMetadata {
         int Order { get; }
         bool Accept(ActionConstraintContext context);
     }
     public interface IActionConstraintFactory : IActionConstraintMetadata {
         bool IsReusable { get; }
         IActionConstraint CreateInstance(IServiceProvider services);
     }
     public interface IActionConstraintMetadata
     public interface IActionConstraintProvider {
         int Order { get; }
         void OnProvidersExecuted(ActionConstraintProviderContext context);
         void OnProvidersExecuting(ActionConstraintProviderContext context);
     }
 }
```

