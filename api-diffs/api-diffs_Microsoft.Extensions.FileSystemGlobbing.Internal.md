# Microsoft.Extensions.FileSystemGlobbing.Internal

``` diff
 namespace Microsoft.Extensions.FileSystemGlobbing.Internal {
     public interface ILinearPattern : IPattern {
         IList<IPathSegment> Segments { get; }
     }
     public interface IPathSegment {
         bool CanProduceStem { get; }
         bool Match(string value);
     }
     public interface IPattern {
         IPatternContext CreatePatternContextForExclude();
         IPatternContext CreatePatternContextForInclude();
     }
     public interface IPatternContext {
         void Declare(Action<IPathSegment, bool> onDeclare);
         void PopDirectory();
         void PushDirectory(DirectoryInfoBase directory);
         bool Test(DirectoryInfoBase directory);
         PatternTestResult Test(FileInfoBase file);
     }
     public interface IRaggedPattern : IPattern {
         IList<IList<IPathSegment>> Contains { get; }
         IList<IPathSegment> EndsWith { get; }
         IList<IPathSegment> Segments { get; }
         IList<IPathSegment> StartsWith { get; }
     }
     public class MatcherContext {
         public MatcherContext(IEnumerable<IPattern> includePatterns, IEnumerable<IPattern> excludePatterns, DirectoryInfoBase directoryInfo, StringComparison comparison);
         public PatternMatchingResult Execute();
     }
     public struct PatternTestResult {
         public static readonly PatternTestResult Failed;
         public bool IsSuccessful { get; }
         public string Stem { get; }
         public static PatternTestResult Success(string stem);
     }
 }
```

