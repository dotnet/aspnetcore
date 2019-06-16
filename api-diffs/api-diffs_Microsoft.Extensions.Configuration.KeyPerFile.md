# Microsoft.Extensions.Configuration.KeyPerFile

``` diff
 namespace Microsoft.Extensions.Configuration.KeyPerFile {
     public class KeyPerFileConfigurationProvider : ConfigurationProvider {
         public KeyPerFileConfigurationProvider(KeyPerFileConfigurationSource source);
         public override void Load();
+        public override string ToString();
     }
     public class KeyPerFileConfigurationSource : IConfigurationSource {
         public KeyPerFileConfigurationSource();
         public IFileProvider FileProvider { get; set; }
         public Func<string, bool> IgnoreCondition { get; set; }
         public string IgnorePrefix { get; set; }
         public bool Optional { get; set; }
         public IConfigurationProvider Build(IConfigurationBuilder builder);
     }
 }
```

