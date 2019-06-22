# Microsoft.EntityFrameworkCore.Extensions.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.Extensions.Internal {
 {
-    public static class AsyncQueryProviderExtensions {
 {
-        public static ConstantExpression CreateEntityQueryableExpression(this IAsyncQueryProvider entityQueryProvider, Type type);

-    }
-    public static class EFPropertyExtensions {
 {
-        public static Expression CreateEFPropertyExpression(this Expression target, IPropertyBase property, bool makeNullable = true);

-        public static Expression CreateEFPropertyExpression(this Expression target, MemberInfo memberInfo);

-        public static bool IsEFProperty(this MethodCallExpression methodCallExpression);

-        public static bool IsEFPropertyMethod(this MethodInfo methodInfo);

-    }
-    public static class MethodInfoExtensions {
 {
-        public static bool MethodIsClosedFormOf(this MethodInfo methodInfo, MethodInfo genericMethod);

-    }
-    public static class QueryableExtensions {
 {
-        public static IAsyncEnumerable<TSource> AsAsyncEnumerable<TSource>(this IQueryable<TSource> source);

-    }
-}
```

