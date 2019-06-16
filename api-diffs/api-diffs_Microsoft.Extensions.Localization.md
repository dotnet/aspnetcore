# Microsoft.Extensions.Localization

``` diff
 namespace Microsoft.Extensions.Localization {
     public interface IResourceNamesCache {
         IList<string> GetOrAdd(string name, Func<string, IList<string>> valueFactory);
     }
     public interface IStringLocalizer {
         LocalizedString this[string name, params object[] arguments] { get; }
         LocalizedString this[string name] { get; }
         IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures);
         IStringLocalizer WithCulture(CultureInfo culture);
     }
-    public interface IStringLocalizer<T> : IStringLocalizer
+    public interface IStringLocalizer<out T> : IStringLocalizer
     public interface IStringLocalizerFactory {
         IStringLocalizer Create(string baseName, string location);
         IStringLocalizer Create(Type resourceSource);
     }
     public class LocalizationOptions {
         public LocalizationOptions();
         public string ResourcesPath { get; set; }
     }
     public class LocalizedString {
         public LocalizedString(string name, string value);
         public LocalizedString(string name, string value, bool resourceNotFound);
         public LocalizedString(string name, string value, bool resourceNotFound, string searchedLocation);
         public string Name { get; }
         public bool ResourceNotFound { get; }
         public string SearchedLocation { get; }
         public string Value { get; }
         public static implicit operator string (LocalizedString localizedString);
         public override string ToString();
     }
     public class ResourceLocationAttribute : Attribute {
         public ResourceLocationAttribute(string resourceLocation);
         public string ResourceLocation { get; }
     }
     public class ResourceManagerStringLocalizer : IStringLocalizer {
         public ResourceManagerStringLocalizer(ResourceManager resourceManager, AssemblyWrapper resourceAssemblyWrapper, string baseName, IResourceNamesCache resourceNamesCache, ILogger logger);
         public ResourceManagerStringLocalizer(ResourceManager resourceManager, IResourceStringProvider resourceStringProvider, string baseName, IResourceNamesCache resourceNamesCache, ILogger logger);
         public ResourceManagerStringLocalizer(ResourceManager resourceManager, Assembly resourceAssembly, string baseName, IResourceNamesCache resourceNamesCache, ILogger logger);
         public virtual LocalizedString this[string name, params object[] arguments] { get; }
         public virtual LocalizedString this[string name] { get; }
         public virtual IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures);
         protected IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures, CultureInfo culture);
         protected string GetStringSafely(string name, CultureInfo culture);
         public IStringLocalizer WithCulture(CultureInfo culture);
     }
     public class ResourceManagerStringLocalizerFactory : IStringLocalizerFactory {
         public ResourceManagerStringLocalizerFactory(IOptions<LocalizationOptions> localizationOptions, ILoggerFactory loggerFactory);
         public IStringLocalizer Create(string baseName, string location);
         public IStringLocalizer Create(Type resourceSource);
         protected virtual ResourceManagerStringLocalizer CreateResourceManagerStringLocalizer(Assembly assembly, string baseName);
         protected virtual ResourceLocationAttribute GetResourceLocationAttribute(Assembly assembly);
         protected virtual string GetResourcePrefix(TypeInfo typeInfo);
         protected virtual string GetResourcePrefix(TypeInfo typeInfo, string baseNamespace, string resourcesRelativePath);
         protected virtual string GetResourcePrefix(string baseResourceName, string baseNamespace);
         protected virtual string GetResourcePrefix(string location, string baseName, string resourceLocation);
         protected virtual RootNamespaceAttribute GetRootNamespaceAttribute(Assembly assembly);
     }
     public class ResourceManagerWithCultureStringLocalizer : ResourceManagerStringLocalizer {
         public ResourceManagerWithCultureStringLocalizer(ResourceManager resourceManager, Assembly resourceAssembly, string baseName, IResourceNamesCache resourceNamesCache, CultureInfo culture, ILogger logger);
         public override LocalizedString this[string name, params object[] arguments] { get; }
         public override LocalizedString this[string name] { get; }
         public override IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures);
     }
     public class ResourceNamesCache : IResourceNamesCache {
         public ResourceNamesCache();
         public IList<string> GetOrAdd(string name, Func<string, IList<string>> valueFactory);
     }
     public class RootNamespaceAttribute : Attribute {
         public RootNamespaceAttribute(string rootNamespace);
         public string RootNamespace { get; }
     }
     public class StringLocalizer<TResourceSource> : IStringLocalizer, IStringLocalizer<TResourceSource> {
         public StringLocalizer(IStringLocalizerFactory factory);
         public virtual LocalizedString this[string name, params object[] arguments] { get; }
         public virtual LocalizedString this[string name] { get; }
         public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures);
         public virtual IStringLocalizer WithCulture(CultureInfo culture);
     }
     public static class StringLocalizerExtensions {
         public static IEnumerable<LocalizedString> GetAllStrings(this IStringLocalizer stringLocalizer);
         public static LocalizedString GetString(this IStringLocalizer stringLocalizer, string name);
         public static LocalizedString GetString(this IStringLocalizer stringLocalizer, string name, params object[] arguments);
     }
 }
```

