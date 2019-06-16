# Microsoft.Extensions.FileSystemGlobbing

``` diff
 namespace Microsoft.Extensions.FileSystemGlobbing {
     public struct FilePatternMatch : IEquatable<FilePatternMatch> {
         public FilePatternMatch(string path, string stem);
         public string Path { get; }
         public string Stem { get; }
         public bool Equals(FilePatternMatch other);
         public override bool Equals(object obj);
         public override int GetHashCode();
     }
     public class InMemoryDirectoryInfo : DirectoryInfoBase {
         public InMemoryDirectoryInfo(string rootDir, IEnumerable<string> files);
         public override string FullName { get; }
         public override string Name { get; }
         public override DirectoryInfoBase ParentDirectory { get; }
         public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos();
         public override DirectoryInfoBase GetDirectory(string path);
         public override FileInfoBase GetFile(string path);
     }
     public class Matcher {
         public Matcher();
         public Matcher(StringComparison comparisonType);
         public virtual Matcher AddExclude(string pattern);
         public virtual Matcher AddInclude(string pattern);
         public virtual PatternMatchingResult Execute(DirectoryInfoBase directoryInfo);
     }
     public static class MatcherExtensions {
         public static void AddExcludePatterns(this Matcher matcher, params IEnumerable<string>[] excludePatternsGroups);
         public static void AddIncludePatterns(this Matcher matcher, params IEnumerable<string>[] includePatternsGroups);
         public static IEnumerable<string> GetResultsInFullPath(this Matcher matcher, string directoryPath);
         public static PatternMatchingResult Match(this Matcher matcher, IEnumerable<string> files);
         public static PatternMatchingResult Match(this Matcher matcher, string file);
         public static PatternMatchingResult Match(this Matcher matcher, string rootDir, IEnumerable<string> files);
         public static PatternMatchingResult Match(this Matcher matcher, string rootDir, string file);
     }
     public class PatternMatchingResult {
         public PatternMatchingResult(IEnumerable<FilePatternMatch> files);
         public PatternMatchingResult(IEnumerable<FilePatternMatch> files, bool hasMatches);
         public IEnumerable<FilePatternMatch> Files { get; set; }
         public bool HasMatches { get; }
     }
 }
```

