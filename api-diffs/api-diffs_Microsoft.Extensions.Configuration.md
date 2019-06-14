# Microsoft.Extensions.Configuration

``` diff
 namespace Microsoft.Extensions.Configuration {
     public static class ChainedBuilderExtensions {
+        public static IConfigurationBuilder AddConfiguration(this IConfigurationBuilder configurationBuilder, IConfiguration config, bool shouldDisposeConfiguration);
     }
-    public class ChainedConfigurationProvider : IConfigurationProvider {
+    public class ChainedConfigurationProvider : IConfigurationProvider, IDisposable {
+        public void Dispose();
     }
     public class ChainedConfigurationSource : IConfigurationSource {
+        public bool ShouldDisposeConfiguration { get; set; }
     }
     public abstract class ConfigurationProvider : IConfigurationProvider {
+        public override string ToString();
     }
-    public class ConfigurationRoot : IConfiguration, IConfigurationRoot {
+    public class ConfigurationRoot : IConfiguration, IConfigurationRoot, IDisposable {
+        public void Dispose();
     }
+    public static class ConfigurationRootExtensions {
+        public static string GetDebugView(this IConfigurationRoot root);
+    }
     public class ConfigurationSection : IConfiguration, IConfigurationSection {
-        public ConfigurationSection(ConfigurationRoot root, string path);

+        public ConfigurationSection(IConfigurationRoot root, string path);
     }
-    public abstract class FileConfigurationProvider : ConfigurationProvider {
+    public abstract class FileConfigurationProvider : ConfigurationProvider, IDisposable {
+        public void Dispose();
+        protected virtual void Dispose(bool disposing);
+        public override string ToString();
     }
     public static class IniConfigurationExtensions {
+        public static IConfigurationBuilder AddIniStream(this IConfigurationBuilder builder, Stream stream);
     }
     public static class JsonConfigurationExtensions {
+        public static IConfigurationBuilder AddJsonStream(this IConfigurationBuilder builder, Stream stream);
     }
+    public abstract class StreamConfigurationProvider : ConfigurationProvider {
+        public StreamConfigurationProvider(StreamConfigurationSource source);
+        public StreamConfigurationSource Source { get; }
+        public override void Load();
+        public abstract void Load(Stream stream);
+    }
+    public abstract class StreamConfigurationSource : IConfigurationSource {
+        protected StreamConfigurationSource();
+        public Stream Stream { get; set; }
+        public abstract IConfigurationProvider Build(IConfigurationBuilder builder);
+    }
     public static class UserSecretsConfigurationExtensions {
+        public static IConfigurationBuilder AddUserSecrets(this IConfigurationBuilder configuration, Assembly assembly, bool optional, bool reloadOnChange);
+        public static IConfigurationBuilder AddUserSecrets(this IConfigurationBuilder configuration, string userSecretsId, bool reloadOnChange);
+        public static IConfigurationBuilder AddUserSecrets<T>(this IConfigurationBuilder configuration, bool optional, bool reloadOnChange) where T : class;
     }
     public static class XmlConfigurationExtensions {
+        public static IConfigurationBuilder AddXmlStream(this IConfigurationBuilder builder, Stream stream);
     }
 }
```

