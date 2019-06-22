# Microsoft.EntityFrameworkCore.SqlServer.Query.ExpressionVisitors.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.SqlServer.Query.ExpressionVisitors.Internal {
 {
-    public class SqlServerSqlTranslatingExpressionVisitor : SqlTranslatingExpressionVisitor {
 {
-        public SqlServerSqlTranslatingExpressionVisitor(SqlTranslatingExpressionVisitorDependencies dependencies, RelationalQueryModelVisitor queryModelVisitor, SelectExpression targetSelectExpression = null, Expression topLevelPredicate = null, bool inProjection = false);

-        protected override Expression VisitBinary(BinaryExpression binaryExpression);

-    }
-    public class SqlServerSqlTranslatingExpressionVisitorFactory : SqlTranslatingExpressionVisitorFactory {
 {
-        public SqlServerSqlTranslatingExpressionVisitorFactory(SqlTranslatingExpressionVisitorDependencies dependencies);

-        public override SqlTranslatingExpressionVisitor Create(RelationalQueryModelVisitor queryModelVisitor, SelectExpression targetSelectExpression = null, Expression topLevelPredicate = null, bool inProjection = false);

-    }
-}
```

