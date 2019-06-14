# Microsoft.Extensions.Configuration.Xml

``` diff
 namespace Microsoft.Extensions.Configuration.Xml {
+    public class XmlStreamConfigurationProvider : StreamConfigurationProvider {
+        public XmlStreamConfigurationProvider(XmlStreamConfigurationSource source);
+        public override void Load(Stream stream);
+        public static IDictionary<string, string> Read(Stream stream, XmlDocumentDecryptor decryptor);
+    }
+    public class XmlStreamConfigurationSource : StreamConfigurationSource {
+        public XmlStreamConfigurationSource();
+        public override IConfigurationProvider Build(IConfigurationBuilder builder);
+    }
 }
```

