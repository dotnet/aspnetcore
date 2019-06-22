# Microsoft.EntityFrameworkCore.SqlServer.Query.ExpressionTranslators.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.SqlServer.Query.ExpressionTranslators.Internal {
 {
-    public class SqlServerCompositeMemberTranslator : RelationalCompositeMemberTranslator {
 {
-        public SqlServerCompositeMemberTranslator(RelationalCompositeMemberTranslatorDependencies dependencies);

-    }
-    public class SqlServerCompositeMethodCallTranslator : RelationalCompositeMethodCallTranslator {
 {
-        public SqlServerCompositeMethodCallTranslator(RelationalCompositeMethodCallTranslatorDependencies dependencies);

-    }
-    public class SqlServerContainsOptimizedTranslator : IMethodCallTranslator {
 {
-        public SqlServerContainsOptimizedTranslator();

-        public virtual Expression Translate(MethodCallExpression methodCallExpression);

-    }
-    public class SqlServerConvertTranslator : IMethodCallTranslator {
 {
-        public SqlServerConvertTranslator();

-        public virtual Expression Translate(MethodCallExpression methodCallExpression);

-    }
-    public class SqlServerDateAddTranslator : IMethodCallTranslator {
 {
-        public SqlServerDateAddTranslator();

-        public virtual Expression Translate(MethodCallExpression methodCallExpression);

-    }
-    public class SqlServerDateDiffTranslator : IMethodCallTranslator {
 {
-        public SqlServerDateDiffTranslator();

-        public virtual Expression Translate(MethodCallExpression methodCallExpression);

-    }
-    public class SqlServerDateTimeMemberTranslator : IMemberTranslator {
 {
-        public SqlServerDateTimeMemberTranslator();

-        public virtual Expression Translate(MemberExpression memberExpression);

-    }
-    public class SqlServerEndsWithOptimizedTranslator : IMethodCallTranslator {
 {
-        public SqlServerEndsWithOptimizedTranslator();

-        public virtual Expression Translate(MethodCallExpression methodCallExpression);

-    }
-    public class SqlServerFullTextSearchMethodCallTranslator : IMethodCallTranslator {
 {
-        public SqlServerFullTextSearchMethodCallTranslator();

-        public virtual Expression Translate(MethodCallExpression methodCallExpression);

-    }
-    public class SqlServerMathTranslator : IMethodCallTranslator {
 {
-        public SqlServerMathTranslator();

-        public virtual Expression Translate(MethodCallExpression methodCallExpression);

-    }
-    public class SqlServerNewGuidTranslator : SingleOverloadStaticMethodCallTranslator {
 {
-        public SqlServerNewGuidTranslator();

-    }
-    public class SqlServerObjectToStringTranslator : IMethodCallTranslator {
 {
-        public SqlServerObjectToStringTranslator();

-        public virtual Expression Translate(MethodCallExpression methodCallExpression);

-    }
-    public class SqlServerStartsWithOptimizedTranslator : IMethodCallTranslator {
 {
-        public SqlServerStartsWithOptimizedTranslator();

-        public virtual Expression Translate(MethodCallExpression methodCallExpression);

-    }
-    public class SqlServerStringConcatMethodCallTranslator : IMethodCallTranslator {
 {
-        public SqlServerStringConcatMethodCallTranslator();

-        public virtual Expression Translate(MethodCallExpression methodCallExpression);

-    }
-    public class SqlServerStringIndexOfTranslator : IMethodCallTranslator {
 {
-        public SqlServerStringIndexOfTranslator();

-        public virtual Expression Translate(MethodCallExpression methodCallExpression);

-    }
-    public class SqlServerStringIsNullOrWhiteSpaceTranslator : IMethodCallTranslator {
 {
-        public SqlServerStringIsNullOrWhiteSpaceTranslator();

-        public virtual Expression Translate(MethodCallExpression methodCallExpression);

-    }
-    public class SqlServerStringLengthTranslator : IMemberTranslator {
 {
-        public SqlServerStringLengthTranslator();

-        public virtual Expression Translate(MemberExpression memberExpression);

-    }
-    public class SqlServerStringReplaceTranslator : IMethodCallTranslator {
 {
-        public SqlServerStringReplaceTranslator();

-        public virtual Expression Translate(MethodCallExpression methodCallExpression);

-    }
-    public class SqlServerStringSubstringTranslator : IMethodCallTranslator {
 {
-        public SqlServerStringSubstringTranslator();

-        public virtual Expression Translate(MethodCallExpression methodCallExpression);

-    }
-    public class SqlServerStringToLowerTranslator : ParameterlessInstanceMethodCallTranslator {
 {
-        public SqlServerStringToLowerTranslator();

-    }
-    public class SqlServerStringToUpperTranslator : ParameterlessInstanceMethodCallTranslator {
 {
-        public SqlServerStringToUpperTranslator();

-    }
-    public class SqlServerStringTrimEndTranslator : IMethodCallTranslator {
 {
-        public SqlServerStringTrimEndTranslator();

-        public virtual Expression Translate(MethodCallExpression methodCallExpression);

-    }
-    public class SqlServerStringTrimStartTranslator : IMethodCallTranslator {
 {
-        public SqlServerStringTrimStartTranslator();

-        public virtual Expression Translate(MethodCallExpression methodCallExpression);

-    }
-    public class SqlServerStringTrimTranslator : IMethodCallTranslator {
 {
-        public SqlServerStringTrimTranslator();

-        public virtual Expression Translate(MethodCallExpression methodCallExpression);

-    }
-}
```

