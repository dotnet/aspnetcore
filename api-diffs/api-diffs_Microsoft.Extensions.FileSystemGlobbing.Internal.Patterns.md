# Microsoft.Extensions.FileSystemGlobbing.Internal.Patterns

``` diff
 namespace Microsoft.Extensions.FileSystemGlobbing.Internal.Patterns {
     public class PatternBuilder {
         public PatternBuilder();
         public PatternBuilder(StringComparison comparisonType);
         public StringComparison ComparisonType { get; }
         public IPattern Build(string pattern);
     }
 }
```

