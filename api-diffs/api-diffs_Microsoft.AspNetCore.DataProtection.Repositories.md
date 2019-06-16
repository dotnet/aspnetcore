# Microsoft.AspNetCore.DataProtection.Repositories

``` diff
 namespace Microsoft.AspNetCore.DataProtection.Repositories {
     public class FileSystemXmlRepository : IXmlRepository {
         public FileSystemXmlRepository(DirectoryInfo directory, ILoggerFactory loggerFactory);
         public static DirectoryInfo DefaultKeyStorageDirectory { get; }
         public DirectoryInfo Directory { get; }
         public virtual IReadOnlyCollection<XElement> GetAllElements();
         public virtual void StoreElement(XElement element, string friendlyName);
     }
     public interface IXmlRepository {
         IReadOnlyCollection<XElement> GetAllElements();
         void StoreElement(XElement element, string friendlyName);
     }
     public class RegistryXmlRepository : IXmlRepository {
         public RegistryXmlRepository(RegistryKey registryKey, ILoggerFactory loggerFactory);
         public static RegistryKey DefaultRegistryKey { get; }
         public RegistryKey RegistryKey { get; }
         public virtual IReadOnlyCollection<XElement> GetAllElements();
         public virtual void StoreElement(XElement element, string friendlyName);
     }
 }
```

