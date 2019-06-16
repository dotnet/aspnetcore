# Microsoft.Extensions.FileSystemGlobbing.Abstractions

``` diff
 namespace Microsoft.Extensions.FileSystemGlobbing.Abstractions {
     public abstract class DirectoryInfoBase : FileSystemInfoBase {
         protected DirectoryInfoBase();
         public abstract IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos();
         public abstract DirectoryInfoBase GetDirectory(string path);
         public abstract FileInfoBase GetFile(string path);
     }
     public class DirectoryInfoWrapper : DirectoryInfoBase {
         public DirectoryInfoWrapper(DirectoryInfo directoryInfo);
         public override string FullName { get; }
         public override string Name { get; }
         public override DirectoryInfoBase ParentDirectory { get; }
         public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos();
         public override DirectoryInfoBase GetDirectory(string name);
         public override FileInfoBase GetFile(string name);
     }
     public abstract class FileInfoBase : FileSystemInfoBase {
         protected FileInfoBase();
     }
     public class FileInfoWrapper : FileInfoBase {
         public FileInfoWrapper(FileInfo fileInfo);
         public override string FullName { get; }
         public override string Name { get; }
         public override DirectoryInfoBase ParentDirectory { get; }
     }
     public abstract class FileSystemInfoBase {
         protected FileSystemInfoBase();
         public abstract string FullName { get; }
         public abstract string Name { get; }
         public abstract DirectoryInfoBase ParentDirectory { get; }
     }
 }
```

