# Microsoft.Extensions.Configuration.Json

``` diff
 namespace Microsoft.Extensions.Configuration.Json {
+    public class JsonStreamConfigurationProvider : StreamConfigurationProvider {
+        public JsonStreamConfigurationProvider(JsonStreamConfigurationSource source);
+        public override void Load(Stream stream);
+    }
+    public class JsonStreamConfigurationSource : StreamConfigurationSource {
+        public JsonStreamConfigurationSource();
+        public override IConfigurationProvider Build(IConfigurationBuilder builder);
+    }
 }
```

