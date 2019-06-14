# Microsoft.AspNetCore.Components.Browser

``` diff
+namespace Microsoft.AspNetCore.Components.Browser {
+    public static class RendererRegistryEventDispatcher {
+        public static Task DispatchEvent(RendererRegistryEventDispatcher.BrowserEventDescriptor eventDescriptor, string eventArgsJson);
+        public class BrowserEventDescriptor {
+            public BrowserEventDescriptor();
+            public int BrowserRendererId { get; set; }
+            public string EventArgsType { get; set; }
+            public int EventHandlerId { get; set; }
+        }
+    }
+}
```

