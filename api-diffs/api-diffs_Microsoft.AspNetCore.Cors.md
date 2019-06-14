# Microsoft.AspNetCore.Cors

``` diff
 namespace Microsoft.AspNetCore.Cors {
+    public class CorsPolicyMetadata : ICorsMetadata, ICorsPolicyMetadata {
+        public CorsPolicyMetadata(CorsPolicy policy);
+        public CorsPolicy Policy { get; }
+    }
-    public class DisableCorsAttribute : Attribute, IDisableCorsAttribute
+    public class DisableCorsAttribute : Attribute, ICorsMetadata, IDisableCorsAttribute
-    public class EnableCorsAttribute : Attribute, IEnableCorsAttribute
+    public class EnableCorsAttribute : Attribute, ICorsMetadata, IEnableCorsAttribute
 }
```

