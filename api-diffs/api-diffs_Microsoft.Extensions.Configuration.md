# Microsoft.Extensions.Configuration

``` diff
 namespace Microsoft.Extensions.Configuration {
     public class BinderOptions {
         public BinderOptions();
         public bool BindNonPublicProperties { get; set; }
     }
     public static class ChainedBuilderExtensions {
         public static IConfigurationBuilder AddConfiguration(this IConfigurationBuilder configurationBuilder, IConfiguration config);
+        public static IConfigurationBuilder AddConfiguration(this IConfigurationBuilder configurationBuilder, IConfiguration config, bool shouldDisposeConfiguration);
     }
-    public class ChainedConfigurationProvider : IConfigurationProvider {
+    public class ChainedConfigurationProvider : IConfigurationProvider, IDisposable {
         public ChainedConfigurationProvider(ChainedConfigurationSource source);
+        public void Dispose();
         public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath);
         public IChangeToken GetReloadToken();
         public void Load();
         public void Set(string key, string value);
         public bool TryGet(string key, out string value);
     }
     public class ChainedConfigurationSource : IConfigurationSource {
         public ChainedConfigurationSource();
         public IConfiguration Configuration { get; set; }
+        public bool ShouldDisposeConfiguration { get; set; }
         public IConfigurationProvider Build(IConfigurationBuilder builder);
     }
     public static class CommandLineConfigurationExtensions {
         public static IConfigurationBuilder AddCommandLine(this IConfigurationBuilder builder, Action<CommandLineConfigurationSource> configureSource);
         public static IConfigurationBuilder AddCommandLine(this IConfigurationBuilder configurationBuilder, string[] args);
         public static IConfigurationBuilder AddCommandLine(this IConfigurationBuilder configurationBuilder, string[] args, IDictionary<string, string> switchMappings);
     }
     public static class ConfigurationBinder {
         public static void Bind(this IConfiguration configuration, object instance);
         public static void Bind(this IConfiguration configuration, object instance, Action<BinderOptions> configureOptions);
         public static void Bind(this IConfiguration configuration, string key, object instance);
         public static object Get(this IConfiguration configuration, Type type);
         public static object Get(this IConfiguration configuration, Type type, Action<BinderOptions> configureOptions);
         public static T Get<T>(this IConfiguration configuration);
         public static T Get<T>(this IConfiguration configuration, Action<BinderOptions> configureOptions);
         public static object GetValue(this IConfiguration configuration, Type type, string key);
         public static object GetValue(this IConfiguration configuration, Type type, string key, object defaultValue);
         public static T GetValue<T>(this IConfiguration configuration, string key);
         public static T GetValue<T>(this IConfiguration configuration, string key, T defaultValue);
     }
     public class ConfigurationBuilder : IConfigurationBuilder {
         public ConfigurationBuilder();
         public IDictionary<string, object> Properties { get; }
         public IList<IConfigurationSource> Sources { get; }
         public IConfigurationBuilder Add(IConfigurationSource source);
         public IConfigurationRoot Build();
     }
     public static class ConfigurationExtensions {
         public static IConfigurationBuilder Add<TSource>(this IConfigurationBuilder builder, Action<TSource> configureSource) where TSource : IConfigurationSource, new();
         public static IEnumerable<KeyValuePair<string, string>> AsEnumerable(this IConfiguration configuration);
         public static IEnumerable<KeyValuePair<string, string>> AsEnumerable(this IConfiguration configuration, bool makePathsRelative);
         public static bool Exists(this IConfigurationSection section);
         public static string GetConnectionString(this IConfiguration configuration, string name);
     }
     public class ConfigurationKeyComparer : IComparer<string> {
         public ConfigurationKeyComparer();
         public static ConfigurationKeyComparer Instance { get; }
         public int Compare(string x, string y);
     }
     public static class ConfigurationPath {
         public static readonly string KeyDelimiter;
         public static string Combine(IEnumerable<string> pathSegments);
         public static string Combine(params string[] pathSegments);
         public static string GetParentPath(string path);
         public static string GetSectionKey(string path);
     }
     public abstract class ConfigurationProvider : IConfigurationProvider {
         protected ConfigurationProvider();
         protected IDictionary<string, string> Data { get; set; }
         public virtual IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath);
         public IChangeToken GetReloadToken();
         public virtual void Load();
         protected void OnReload();
         public virtual void Set(string key, string value);
+        public override string ToString();
         public virtual bool TryGet(string key, out string value);
     }
     public class ConfigurationReloadToken : IChangeToken {
         public ConfigurationReloadToken();
         public bool ActiveChangeCallbacks { get; }
         public bool HasChanged { get; }
         public void OnReload();
         public IDisposable RegisterChangeCallback(Action<object> callback, object state);
     }
-    public class ConfigurationRoot : IConfiguration, IConfigurationRoot {
+    public class ConfigurationRoot : IConfiguration, IConfigurationRoot, IDisposable {
         public ConfigurationRoot(IList<IConfigurationProvider> providers);
         public IEnumerable<IConfigurationProvider> Providers { get; }
         public string this[string key] { get; set; }
+        public void Dispose();
         public IEnumerable<IConfigurationSection> GetChildren();
         public IChangeToken GetReloadToken();
         public IConfigurationSection GetSection(string key);
         public void Reload();
     }
+    public static class ConfigurationRootExtensions {
+        public static string GetDebugView(this IConfigurationRoot root);
+    }
     public class ConfigurationSection : IConfiguration, IConfigurationSection {
-        public ConfigurationSection(ConfigurationRoot root, string path);

+        public ConfigurationSection(IConfigurationRoot root, string path);
         public string Key { get; }
         public string Path { get; }
         public string this[string key] { get; set; }
         public string Value { get; set; }
         public IEnumerable<IConfigurationSection> GetChildren();
         public IChangeToken GetReloadToken();
         public IConfigurationSection GetSection(string key);
     }
     public static class EnvironmentVariablesExtensions {
         public static IConfigurationBuilder AddEnvironmentVariables(this IConfigurationBuilder configurationBuilder);
         public static IConfigurationBuilder AddEnvironmentVariables(this IConfigurationBuilder builder, Action<EnvironmentVariablesConfigurationSource> configureSource);
         public static IConfigurationBuilder AddEnvironmentVariables(this IConfigurationBuilder configurationBuilder, string prefix);
     }
     public static class FileConfigurationExtensions {
         public static Action<FileLoadExceptionContext> GetFileLoadExceptionHandler(this IConfigurationBuilder builder);
         public static IFileProvider GetFileProvider(this IConfigurationBuilder builder);
         public static IConfigurationBuilder SetBasePath(this IConfigurationBuilder builder, string basePath);
         public static IConfigurationBuilder SetFileLoadExceptionHandler(this IConfigurationBuilder builder, Action<FileLoadExceptionContext> handler);
         public static IConfigurationBuilder SetFileProvider(this IConfigurationBuilder builder, IFileProvider fileProvider);
     }
-    public abstract class FileConfigurationProvider : ConfigurationProvider {
+    public abstract class FileConfigurationProvider : ConfigurationProvider, IDisposable {
         public FileConfigurationProvider(FileConfigurationSource source);
         public FileConfigurationSource Source { get; }
+        public void Dispose();
+        protected virtual void Dispose(bool disposing);
         public override void Load();
         public abstract void Load(Stream stream);
+        public override string ToString();
     }
     public abstract class FileConfigurationSource : IConfigurationSource {
         protected FileConfigurationSource();
         public IFileProvider FileProvider { get; set; }
         public Action<FileLoadExceptionContext> OnLoadException { get; set; }
         public bool Optional { get; set; }
         public string Path { get; set; }
         public int ReloadDelay { get; set; }
         public bool ReloadOnChange { get; set; }
         public abstract IConfigurationProvider Build(IConfigurationBuilder builder);
         public void EnsureDefaults(IConfigurationBuilder builder);
         public void ResolveFileProvider();
     }
     public class FileLoadExceptionContext {
         public FileLoadExceptionContext();
         public Exception Exception { get; set; }
         public bool Ignore { get; set; }
         public FileConfigurationProvider Provider { get; set; }
     }
     public interface IConfiguration {
         string this[string key] { get; set; }
         IEnumerable<IConfigurationSection> GetChildren();
         IChangeToken GetReloadToken();
         IConfigurationSection GetSection(string key);
     }
     public interface IConfigurationBuilder {
         IDictionary<string, object> Properties { get; }
         IList<IConfigurationSource> Sources { get; }
         IConfigurationBuilder Add(IConfigurationSource source);
         IConfigurationRoot Build();
     }
     public interface IConfigurationProvider {
         IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath);
         IChangeToken GetReloadToken();
         void Load();
         void Set(string key, string value);
         bool TryGet(string key, out string value);
     }
     public interface IConfigurationRoot : IConfiguration {
         IEnumerable<IConfigurationProvider> Providers { get; }
         void Reload();
     }
     public interface IConfigurationSection : IConfiguration {
         string Key { get; }
         string Path { get; }
         string Value { get; set; }
     }
     public interface IConfigurationSource {
         IConfigurationProvider Build(IConfigurationBuilder builder);
     }
     public static class IniConfigurationExtensions {
         public static IConfigurationBuilder AddIniFile(this IConfigurationBuilder builder, IFileProvider provider, string path, bool optional, bool reloadOnChange);
         public static IConfigurationBuilder AddIniFile(this IConfigurationBuilder builder, Action<IniConfigurationSource> configureSource);
         public static IConfigurationBuilder AddIniFile(this IConfigurationBuilder builder, string path);
         public static IConfigurationBuilder AddIniFile(this IConfigurationBuilder builder, string path, bool optional);
         public static IConfigurationBuilder AddIniFile(this IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange);
+        public static IConfigurationBuilder AddIniStream(this IConfigurationBuilder builder, Stream stream);
     }
     public static class JsonConfigurationExtensions {
         public static IConfigurationBuilder AddJsonFile(this IConfigurationBuilder builder, IFileProvider provider, string path, bool optional, bool reloadOnChange);
         public static IConfigurationBuilder AddJsonFile(this IConfigurationBuilder builder, Action<JsonConfigurationSource> configureSource);
         public static IConfigurationBuilder AddJsonFile(this IConfigurationBuilder builder, string path);
         public static IConfigurationBuilder AddJsonFile(this IConfigurationBuilder builder, string path, bool optional);
         public static IConfigurationBuilder AddJsonFile(this IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange);
+        public static IConfigurationBuilder AddJsonStream(this IConfigurationBuilder builder, Stream stream);
     }
     public static class KeyPerFileConfigurationBuilderExtensions {
         public static IConfigurationBuilder AddKeyPerFile(this IConfigurationBuilder builder, Action<KeyPerFileConfigurationSource> configureSource);
         public static IConfigurationBuilder AddKeyPerFile(this IConfigurationBuilder builder, string directoryPath, bool optional);
     }
     public static class MemoryConfigurationBuilderExtensions {
         public static IConfigurationBuilder AddInMemoryCollection(this IConfigurationBuilder configurationBuilder);
         public static IConfigurationBuilder AddInMemoryCollection(this IConfigurationBuilder configurationBuilder, IEnumerable<KeyValuePair<string, string>> initialData);
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
         public static IConfigurationBuilder AddUserSecrets(this IConfigurationBuilder configuration, Assembly assembly);
         public static IConfigurationBuilder AddUserSecrets(this IConfigurationBuilder configuration, Assembly assembly, bool optional);
+        public static IConfigurationBuilder AddUserSecrets(this IConfigurationBuilder configuration, Assembly assembly, bool optional, bool reloadOnChange);
         public static IConfigurationBuilder AddUserSecrets(this IConfigurationBuilder configuration, string userSecretsId);
+        public static IConfigurationBuilder AddUserSecrets(this IConfigurationBuilder configuration, string userSecretsId, bool reloadOnChange);
         public static IConfigurationBuilder AddUserSecrets<T>(this IConfigurationBuilder configuration) where T : class;
         public static IConfigurationBuilder AddUserSecrets<T>(this IConfigurationBuilder configuration, bool optional) where T : class;
+        public static IConfigurationBuilder AddUserSecrets<T>(this IConfigurationBuilder configuration, bool optional, bool reloadOnChange) where T : class;
     }
     public static class XmlConfigurationExtensions {
         public static IConfigurationBuilder AddXmlFile(this IConfigurationBuilder builder, IFileProvider provider, string path, bool optional, bool reloadOnChange);
         public static IConfigurationBuilder AddXmlFile(this IConfigurationBuilder builder, Action<XmlConfigurationSource> configureSource);
         public static IConfigurationBuilder AddXmlFile(this IConfigurationBuilder builder, string path);
         public static IConfigurationBuilder AddXmlFile(this IConfigurationBuilder builder, string path, bool optional);
         public static IConfigurationBuilder AddXmlFile(this IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange);
+        public static IConfigurationBuilder AddXmlStream(this IConfigurationBuilder builder, Stream stream);
     }
 }
```

