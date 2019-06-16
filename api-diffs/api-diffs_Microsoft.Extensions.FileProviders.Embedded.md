# Microsoft.Extensions.FileProviders.Embedded

``` diff
 namespace Microsoft.Extensions.FileProviders.Embedded {
     public class EmbeddedResourceFileInfo : IFileInfo {
         public EmbeddedResourceFileInfo(Assembly assembly, string resourcePath, string name, DateTimeOffset lastModified);
         public bool Exists { get; }
         public bool IsDirectory { get; }
         public DateTimeOffset LastModified { get; }
         public long Length { get; }
         public string Name { get; }
         public string PhysicalPath { get; }
         public Stream CreateReadStream();
     }
 }
```

