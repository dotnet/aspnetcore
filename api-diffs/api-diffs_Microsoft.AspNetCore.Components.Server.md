# Microsoft.AspNetCore.Components.Server

``` diff
+namespace Microsoft.AspNetCore.Components.Server {
+    public class CircuitOptions {
+        public CircuitOptions();
+        public TimeSpan DisconnectedCircuitRetentionPeriod { get; set; }
+        public int MaxRetainedDisconnectedCircuits { get; set; }
+    }
+    public sealed class ComponentEndpointConventionBuilder : IEndpointConventionBuilder, IHubEndpointConventionBuilder {
+        public void Add(Action<EndpointBuilder> convention);
+    }
+    public class ComponentPrerenderingContext {
+        public ComponentPrerenderingContext();
+        public Type ComponentType { get; set; }
+        public HttpContext Context { get; set; }
+        public ParameterCollection Parameters { get; set; }
+    }
+    public sealed class ComponentPrerenderResult {
+        public void WriteTo(TextWriter writer);
+    }
+    public interface IComponentPrerenderer {
+        Task<ComponentPrerenderResult> PrerenderComponentAsync(ComponentPrerenderingContext context);
+    }
+    public static class WasmMediaTypeNames {
+        public static class Application {
+            public const string Wasm = "application/wasm";
+        }
+    }
+}
```

