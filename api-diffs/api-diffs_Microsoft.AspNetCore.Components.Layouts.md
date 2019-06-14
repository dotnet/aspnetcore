# Microsoft.AspNetCore.Components.Layouts

``` diff
+namespace Microsoft.AspNetCore.Components.Layouts {
+    public class LayoutAttribute : Attribute {
+        public LayoutAttribute(Type layoutType);
+        public Type LayoutType { get; }
+    }
+    public abstract class LayoutComponentBase : ComponentBase {
+        protected LayoutComponentBase();
+        protected RenderFragment Body { get; }
+    }
+    public class PageDisplay : IComponent {
+        public PageDisplay();
+        public RenderFragment AuthorizingContent { get; private set; }
+        public RenderFragment<AuthenticationState> NotAuthorizedContent { get; private set; }
+        public Type Page { get; private set; }
+        public IDictionary<string, object> PageParameters { get; private set; }
+        public void Configure(RenderHandle renderHandle);
+        public Task SetParametersAsync(ParameterCollection parameters);
+    }
+}
```

