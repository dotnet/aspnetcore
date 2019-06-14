# Microsoft.AspNetCore.Components.Routing

``` diff
+namespace Microsoft.AspNetCore.Components.Routing {
+    public interface INavigationInterception {
+        Task EnableNavigationInterceptionAsync();
+    }
+    public readonly struct LocationChangedEventArgs {
+        public LocationChangedEventArgs(string location, bool isNavigationIntercepted);
+        public bool IsNavigationIntercepted { get; }
+        public string Location { get; }
+    }
+    public class NavLink : IComponent, IDisposable {
+        public NavLink();
+        public string ActiveClass { get; private set; }
+        public NavLinkMatch Match { get; private set; }
+        public void Configure(RenderHandle renderHandle);
+        public void Dispose();
+        public Task SetParametersAsync(ParameterCollection parameters);
+    }
+    public enum NavLinkMatch {
+        All = 1,
+        Prefix = 0,
+    }
+    public class Router : IComponent, IDisposable {
+        public Router();
+        public Assembly AppAssembly { get; private set; }
+        public RenderFragment NotFoundContent { get; private set; }
+        public void Configure(RenderHandle renderHandle);
+        public void Dispose();
+        protected virtual void Render(RenderTreeBuilder builder, Type handler, IDictionary<string, object> parameters);
+        public Task SetParametersAsync(ParameterCollection parameters);
+    }
+}
```

