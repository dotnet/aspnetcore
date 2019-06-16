# Microsoft.AspNetCore.Cors

``` diff
 namespace Microsoft.AspNetCore.Cors {
+    public class CorsPolicyMetadata : ICorsMetadata, ICorsPolicyMetadata {
+        public CorsPolicyMetadata(CorsPolicy policy);
+        public CorsPolicy Policy { get; }
+    }
-    public class DisableCorsAttribute : Attribute, IDisableCorsAttribute {
+    public class DisableCorsAttribute : Attribute, ICorsMetadata, IDisableCorsAttribute {
         public DisableCorsAttribute();
     }
-    public class EnableCorsAttribute : Attribute, IEnableCorsAttribute {
+    public class EnableCorsAttribute : Attribute, ICorsMetadata, IEnableCorsAttribute {
         public EnableCorsAttribute();
         public EnableCorsAttribute(string policyName);
         public string PolicyName { get; set; }
     }
 }
```

