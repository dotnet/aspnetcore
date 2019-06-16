# Microsoft.Extensions.Configuration.Ini

``` diff
 namespace Microsoft.Extensions.Configuration.Ini {
     public class IniConfigurationProvider : FileConfigurationProvider {
         public IniConfigurationProvider(IniConfigurationSource source);
         public override void Load(Stream stream);
     }
     public class IniConfigurationSource : FileConfigurationSource {
         public IniConfigurationSource();
         public override IConfigurationProvider Build(IConfigurationBuilder builder);
     }
+    public class IniStreamConfigurationProvider : StreamConfigurationProvider {
+        public IniStreamConfigurationProvider(IniStreamConfigurationSource source);
+        public override void Load(Stream stream);
+        public static IDictionary<string, string> Read(Stream stream);
+    }
+    public class IniStreamConfigurationSource : StreamConfigurationSource {
+        public IniStreamConfigurationSource();
+        public override IConfigurationProvider Build(IConfigurationBuilder builder);
+    }
 }
```

