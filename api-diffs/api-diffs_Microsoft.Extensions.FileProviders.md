# Microsoft.Extensions.FileProviders

``` diff
 namespace Microsoft.Extensions.FileProviders {
     public class CompositeFileProvider : IFileProvider {
         public CompositeFileProvider(params IFileProvider[] fileProviders);
         public CompositeFileProvider(IEnumerable<IFileProvider> fileProviders);
         public IEnumerable<IFileProvider> FileProviders { get; }
         public IDirectoryContents GetDirectoryContents(string subpath);
         public IFileInfo GetFileInfo(string subpath);
         public IChangeToken Watch(string pattern);
     }
     public class EmbeddedFileProvider : IFileProvider {
         public EmbeddedFileProvider(Assembly assembly);
         public EmbeddedFileProvider(Assembly assembly, string baseNamespace);
         public IDirectoryContents GetDirectoryContents(string subpath);
         public IFileInfo GetFileInfo(string subpath);
         public IChangeToken Watch(string pattern);
     }
     public interface IDirectoryContents : IEnumerable, IEnumerable<IFileInfo> {
         bool Exists { get; }
     }
     public interface IFileInfo {
         bool Exists { get; }
         bool IsDirectory { get; }
         DateTimeOffset LastModified { get; }
         long Length { get; }
         string Name { get; }
         string PhysicalPath { get; }
         Stream CreateReadStream();
     }
     public interface IFileProvider {
         IDirectoryContents GetDirectoryContents(string subpath);
         IFileInfo GetFileInfo(string subpath);
         IChangeToken Watch(string filter);
     }
     public class ManifestEmbeddedFileProvider : IFileProvider {
         public ManifestEmbeddedFileProvider(Assembly assembly);
         public ManifestEmbeddedFileProvider(Assembly assembly, string root);
         public ManifestEmbeddedFileProvider(Assembly assembly, string root, DateTimeOffset lastModified);
         public ManifestEmbeddedFileProvider(Assembly assembly, string root, string manifestName, DateTimeOffset lastModified);
         public Assembly Assembly { get; }
         public IDirectoryContents GetDirectoryContents(string subpath);
         public IFileInfo GetFileInfo(string subpath);
         public IChangeToken Watch(string filter);
     }
     public class NotFoundDirectoryContents : IDirectoryContents, IEnumerable, IEnumerable<IFileInfo> {
         public NotFoundDirectoryContents();
         public bool Exists { get; }
         public static NotFoundDirectoryContents Singleton { get; }
         public IEnumerator<IFileInfo> GetEnumerator();
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
     }
     public class NotFoundFileInfo : IFileInfo {
         public NotFoundFileInfo(string name);
         public bool Exists { get; }
         public bool IsDirectory { get; }
         public DateTimeOffset LastModified { get; }
         public long Length { get; }
         public string Name { get; }
         public string PhysicalPath { get; }
         public Stream CreateReadStream();
     }
     public class NullChangeToken : IChangeToken {
         public bool ActiveChangeCallbacks { get; }
         public bool HasChanged { get; }
         public static NullChangeToken Singleton { get; }
         public IDisposable RegisterChangeCallback(Action<object> callback, object state);
     }
     public class NullFileProvider : IFileProvider {
         public NullFileProvider();
         public IDirectoryContents GetDirectoryContents(string subpath);
         public IFileInfo GetFileInfo(string subpath);
         public IChangeToken Watch(string filter);
     }
     public class PhysicalFileProvider : IDisposable, IFileProvider {
         public PhysicalFileProvider(string root);
         public PhysicalFileProvider(string root, ExclusionFilters filters);
         public string Root { get; }
         public bool UseActivePolling { get; set; }
         public bool UsePollingFileWatcher { get; set; }
         public void Dispose();
         protected virtual void Dispose(bool disposing);
         ~PhysicalFileProvider();
         public IDirectoryContents GetDirectoryContents(string subpath);
         public IFileInfo GetFileInfo(string subpath);
         public IChangeToken Watch(string filter);
     }
 }
```

