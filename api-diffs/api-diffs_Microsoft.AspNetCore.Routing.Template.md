# Microsoft.AspNetCore.Routing.Template

``` diff
 namespace Microsoft.AspNetCore.Routing.Template {
+    public abstract class TemplateBinderFactory {
+        protected TemplateBinderFactory();
+        public abstract TemplateBinder Create(RoutePattern pattern);
+        public abstract TemplateBinder Create(RouteTemplate template, RouteValueDictionary defaults);
+    }
 }
```

