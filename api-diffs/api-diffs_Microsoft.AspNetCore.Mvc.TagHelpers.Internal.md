# Microsoft.AspNetCore.Mvc.TagHelpers.Internal

``` diff
-namespace Microsoft.AspNetCore.Mvc.TagHelpers.Internal {
 {
-    public static class AttributeMatcher {
 {
-        public static bool TryDetermineMode<TMode>(TagHelperContext context, IReadOnlyList<ModeAttributes<TMode>> modeInfos, Func<TMode, TMode, int> compare, out TMode result);

-    }
-    public class CacheTagHelperMemoryCacheFactory {
 {
-        public CacheTagHelperMemoryCacheFactory(IOptions<CacheTagHelperOptions> options);

-        public IMemoryCache Cache { get; }

-    }
-    public class CurrentValues {
 {
-        public CurrentValues(ICollection<string> values);

-        public ICollection<string> Values { get; }

-        public ICollection<string> ValuesAndEncodedValues { get; set; }

-    }
-    public class FileProviderGlobbingDirectory : DirectoryInfoBase {
 {
-        public FileProviderGlobbingDirectory(IFileProvider fileProvider, IFileInfo fileInfo, FileProviderGlobbingDirectory parent);

-        public override string FullName { get; }

-        public override string Name { get; }

-        public override DirectoryInfoBase ParentDirectory { get; }

-        public string RelativePath { get; }

-        public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos();

-        public override DirectoryInfoBase GetDirectory(string path);

-        public override FileInfoBase GetFile(string path);

-    }
-    public class FileProviderGlobbingFile : FileInfoBase {
 {
-        public FileProviderGlobbingFile(IFileInfo fileInfo, DirectoryInfoBase parent);

-        public override string FullName { get; }

-        public override string Name { get; }

-        public override DirectoryInfoBase ParentDirectory { get; }

-    }
-    public class GlobbingUrlBuilder {
 {
-        public GlobbingUrlBuilder(IFileProvider fileProvider, IMemoryCache cache, PathString requestPathBase);

-        public IMemoryCache Cache { get; }

-        public IFileProvider FileProvider { get; }

-        public PathString RequestPathBase { get; }

-        public virtual IReadOnlyList<string> BuildUrlList(string staticUrl, string includePattern, string excludePattern);

-    }
-    public static class JavaScriptResources {
 {
-        public static string GetEmbeddedJavaScript(string resourceName);

-    }
-    public class ModeAttributes<TMode> {
 {
-        public ModeAttributes(TMode mode, string[] attributes);

-        public string[] Attributes { get; }

-        public TMode Mode { get; }

-    }
-}
```

