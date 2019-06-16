# Microsoft.Extensions.FileProviders.Physical

``` diff
 namespace Microsoft.Extensions.FileProviders.Physical {
     public enum ExclusionFilters {
         DotPrefixed = 1,
         Hidden = 2,
         None = 0,
         Sensitive = 7,
         System = 4,
     }
     public class PhysicalDirectoryInfo : IFileInfo {
         public PhysicalDirectoryInfo(DirectoryInfo info);
         public bool Exists { get; }
         public bool IsDirectory { get; }
         public DateTimeOffset LastModified { get; }
         public long Length { get; }
         public string Name { get; }
         public string PhysicalPath { get; }
         public Stream CreateReadStream();
     }
     public class PhysicalFileInfo : IFileInfo {
         public PhysicalFileInfo(FileInfo info);
         public bool Exists { get; }
         public bool IsDirectory { get; }
         public DateTimeOffset LastModified { get; }
         public long Length { get; }
         public string Name { get; }
         public string PhysicalPath { get; }
         public Stream CreateReadStream();
     }
     public class PhysicalFilesWatcher : IDisposable {
         public PhysicalFilesWatcher(string root, FileSystemWatcher fileSystemWatcher, bool pollForChanges);
         public PhysicalFilesWatcher(string root, FileSystemWatcher fileSystemWatcher, bool pollForChanges, ExclusionFilters filters);
         public IChangeToken CreateFileChangeToken(string filter);
         public void Dispose();
         protected virtual void Dispose(bool disposing);
         ~PhysicalFilesWatcher();
     }
     public class PollingFileChangeToken : IChangeToken, IPollingChangeToken {
         public PollingFileChangeToken(FileInfo fileInfo);
         public bool ActiveChangeCallbacks { get; internal set; }
         public bool HasChanged { get; }
         public IDisposable RegisterChangeCallback(Action<object> callback, object state);
     }
     public class PollingWildCardChangeToken : IChangeToken, IPollingChangeToken {
         public PollingWildCardChangeToken(string root, string pattern);
         public bool ActiveChangeCallbacks { get; internal set; }
         public bool HasChanged { get; }
         protected virtual DateTime GetLastWriteUtc(string path);
         IDisposable Microsoft.Extensions.Primitives.IChangeToken.RegisterChangeCallback(Action<object> callback, object state);
     }
 }
```

