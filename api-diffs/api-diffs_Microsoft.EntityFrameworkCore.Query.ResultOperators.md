# Microsoft.EntityFrameworkCore.Query.ResultOperators

``` diff
-namespace Microsoft.EntityFrameworkCore.Query.ResultOperators {
 {
-    public interface ICloneableQueryAnnotation : IQueryAnnotation {
 {
-        ICloneableQueryAnnotation Clone(IQuerySource querySource, QueryModel queryModel);

-    }
-    public interface IQueryAnnotation {
 {
-        QueryModel QueryModel { get; set; }

-        IQuerySource QuerySource { get; set; }

-    }
-}
```

