# Microsoft.AspNetCore.Routing.Constraints

``` diff
 namespace Microsoft.AspNetCore.Routing.Constraints {
+    public class FileNameRouteConstraint : IParameterPolicy, IRouteConstraint {
+        public FileNameRouteConstraint();
+        public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection);
+    }
+    public class NonFileNameRouteConstraint : IParameterPolicy, IRouteConstraint {
+        public NonFileNameRouteConstraint();
+        public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection);
+    }
 }
```

