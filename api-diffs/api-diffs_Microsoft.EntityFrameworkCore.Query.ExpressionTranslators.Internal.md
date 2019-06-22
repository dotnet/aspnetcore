# Microsoft.EntityFrameworkCore.Query.ExpressionTranslators.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.Query.ExpressionTranslators.Internal {
 {
-    public class EnumHasFlagTranslator : IMethodCallTranslator {
 {
-        public EnumHasFlagTranslator();

-        public virtual Expression Translate(MethodCallExpression methodCallExpression);

-    }
-    public class EqualsTranslator : IMethodCallTranslator {
 {
-        public EqualsTranslator(IDiagnosticsLogger<DbLoggerCategory.Query> logger);

-        public virtual Expression Translate(MethodCallExpression methodCallExpression);

-    }
-    public class GetValueOrDefaultTranslator : IMethodCallTranslator {
 {
-        public GetValueOrDefaultTranslator();

-        public virtual Expression Translate(MethodCallExpression methodCallExpression);

-    }
-    public class IsNullOrEmptyTranslator : IMethodCallTranslator {
 {
-        public IsNullOrEmptyTranslator();

-        public virtual Expression Translate(MethodCallExpression methodCallExpression);

-    }
-    public class LikeTranslator : IMethodCallTranslator {
 {
-        public LikeTranslator();

-        public virtual Expression Translate(MethodCallExpression methodCallExpression);

-    }
-    public class StringCompareTranslator : IExpressionFragmentTranslator {
 {
-        public StringCompareTranslator();

-        public virtual Expression Translate(Expression expression);

-    }
-    public class StringConcatTranslator : IExpressionFragmentTranslator {
 {
-        public StringConcatTranslator();

-        public virtual Expression Translate(Expression expression);

-    }
-}
```

