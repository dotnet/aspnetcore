# Microsoft.EntityFrameworkCore.SqlServer.Query.Sql.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.SqlServer.Query.Sql.Internal {
 {
-    public interface ISqlServerExpressionVisitor {
 {
-        Expression VisitRowNumber(RowNumberExpression rowNumberExpression);

-    }
-    public class SqlServerQuerySqlGenerator : DefaultQuerySqlGenerator, ISqlServerExpressionVisitor {
 {
-        public SqlServerQuerySqlGenerator(QuerySqlGeneratorDependencies dependencies, SelectExpression selectExpression, bool rowNumberPagingEnabled);

-        protected override Expression ApplyExplicitCastToBoolInProjectionOptimization(Expression expression);

-        protected override void GenerateLimitOffset(SelectExpression selectExpression);

-        protected override Expression VisitBinary(BinaryExpression binaryExpression);

-        public override Expression VisitCrossJoinLateral(CrossJoinLateralExpression crossJoinLateralExpression);

-        public virtual Expression VisitRowNumber(RowNumberExpression rowNumberExpression);

-        public override Expression VisitSqlFunction(SqlFunctionExpression sqlFunctionExpression);

-    }
-    public class SqlServerQuerySqlGeneratorFactory : QuerySqlGeneratorFactoryBase {
 {
-        public SqlServerQuerySqlGeneratorFactory(QuerySqlGeneratorDependencies dependencies, ISqlServerOptions sqlServerOptions);

-        public override IQuerySqlGenerator CreateDefault(SelectExpression selectExpression);

-    }
-}
```

