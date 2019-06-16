# Microsoft.Extensions.Localization.Internal

``` diff
 namespace Microsoft.Extensions.Localization.Internal {
     public class AssemblyWrapper {
         public AssemblyWrapper(Assembly assembly);
         public Assembly Assembly { get; }
         public virtual string FullName { get; }
         public virtual Stream GetManifestResourceStream(string name);
     }
     public interface IResourceStringProvider {
         IList<string> GetAllResourceStrings(CultureInfo culture, bool throwOnMissing);
     }
     public class ResourceManagerStringProvider : IResourceStringProvider {
         public ResourceManagerStringProvider(IResourceNamesCache resourceCache, ResourceManager resourceManager, Assembly assembly, string baseName);
         public IList<string> GetAllResourceStrings(CultureInfo culture, bool throwOnMissing);
     }
 }
```

