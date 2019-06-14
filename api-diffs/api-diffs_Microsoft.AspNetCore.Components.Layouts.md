# Microsoft.AspNetCore.Components.Layouts

``` diff
+namespace Microsoft.AspNetCore.Components.Layouts {
+    public class LayoutAttribute : Attribute {
+        public LayoutAttribute(Type layoutType);
+        public Type LayoutType { get; private set; }
+    }
+    public abstract class LayoutComponentBase : ComponentBase {
+        protected LayoutComponentBase();
+        protected RenderFragment Body { get; private set; }
+    }
+}
```

