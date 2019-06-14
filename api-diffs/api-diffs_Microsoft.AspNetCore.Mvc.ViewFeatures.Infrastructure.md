# Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure

``` diff
+namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure {
+    public abstract class TempDataSerializer {
+        protected TempDataSerializer();
+        public virtual bool CanSerializeType(Type type);
+        public abstract IDictionary<string, object> Deserialize(byte[] unprotectedData);
+        public abstract byte[] Serialize(IDictionary<string, object> values);
+    }
+}
```

