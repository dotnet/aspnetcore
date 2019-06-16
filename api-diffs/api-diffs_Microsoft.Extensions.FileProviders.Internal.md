# Microsoft.Extensions.FileProviders.Internal

``` diff
 namespace Microsoft.Extensions.FileProviders.Internal {
     public class PhysicalDirectoryContents : IDirectoryContents, IEnumerable, IEnumerable<IFileInfo> {
         public PhysicalDirectoryContents(string directory);
         public PhysicalDirectoryContents(string directory, ExclusionFilters filters);
         public bool Exists { get; }
         public IEnumerator<IFileInfo> GetEnumerator();
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
     }
 }
```

