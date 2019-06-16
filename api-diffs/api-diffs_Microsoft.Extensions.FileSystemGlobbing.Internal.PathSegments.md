# Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments

``` diff
 namespace Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments {
     public class CurrentPathSegment : IPathSegment {
         public CurrentPathSegment();
         public bool CanProduceStem { get; }
         public bool Match(string value);
     }
     public class LiteralPathSegment : IPathSegment {
         public LiteralPathSegment(string value, StringComparison comparisonType);
         public bool CanProduceStem { get; }
         public string Value { get; }
         public override bool Equals(object obj);
         public override int GetHashCode();
         public bool Match(string value);
     }
     public class ParentPathSegment : IPathSegment {
         public ParentPathSegment();
         public bool CanProduceStem { get; }
         public bool Match(string value);
     }
     public class RecursiveWildcardSegment : IPathSegment {
         public RecursiveWildcardSegment();
         public bool CanProduceStem { get; }
         public bool Match(string value);
     }
     public class WildcardPathSegment : IPathSegment {
         public static readonly WildcardPathSegment MatchAll;
         public WildcardPathSegment(string beginsWith, List<string> contains, string endsWith, StringComparison comparisonType);
         public string BeginsWith { get; }
         public bool CanProduceStem { get; }
         public List<string> Contains { get; }
         public string EndsWith { get; }
         public bool Match(string value);
     }
 }
```

