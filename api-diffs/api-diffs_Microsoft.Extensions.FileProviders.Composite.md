# Microsoft.Extensions.FileProviders.Composite

``` diff
 namespace Microsoft.Extensions.FileProviders.Composite {
     public class CompositeDirectoryContents : IDirectoryContents, IEnumerable, IEnumerable<IFileInfo> {
         public CompositeDirectoryContents(IList<IFileProvider> fileProviders, string subpath);
         public bool Exists { get; }
         public IEnumerator<IFileInfo> GetEnumerator();
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
     }
 }
```

