# Microsoft.Extensions.Configuration.Xml

``` diff
 namespace Microsoft.Extensions.Configuration.Xml {
     public class XmlConfigurationProvider : FileConfigurationProvider {
         public XmlConfigurationProvider(XmlConfigurationSource source);
         public override void Load(Stream stream);
     }
     public class XmlConfigurationSource : FileConfigurationSource {
         public XmlConfigurationSource();
         public override IConfigurationProvider Build(IConfigurationBuilder builder);
     }
     public class XmlDocumentDecryptor {
         public static readonly XmlDocumentDecryptor Instance;
         protected XmlDocumentDecryptor();
         public XmlReader CreateDecryptingXmlReader(Stream input, XmlReaderSettings settings);
         protected virtual XmlReader DecryptDocumentAndCreateXmlReader(XmlDocument document);
     }
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

