# Microsoft.AspNetCore.Routing.Patterns

``` diff
 namespace Microsoft.AspNetCore.Routing.Patterns {
     public sealed class RoutePattern {
+        public static readonly object RequiredValueAny;
+        public IReadOnlyDictionary<string, object> RequiredValues { get; }
     }
     public static class RoutePatternFactory {
+        public static RoutePattern Parse(string pattern, object defaults, object parameterPolicies, object requiredValues);
     }
+    public abstract class RoutePatternTransformer {
+        protected RoutePatternTransformer();
+        public abstract RoutePattern SubstituteRequiredValues(RoutePattern original, object requiredValues);
+    }
 }
```

