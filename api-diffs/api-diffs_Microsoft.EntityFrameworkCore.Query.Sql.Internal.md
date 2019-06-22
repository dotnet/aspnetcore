# Microsoft.EntityFrameworkCore.Query.Sql.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.Query.Sql.Internal {
 {
-    public class FromSqlNonComposedQuerySqlGenerator : DefaultQuerySqlGenerator {
 {
-        public FromSqlNonComposedQuerySqlGenerator(QuerySqlGeneratorDependencies dependencies, SelectExpression selectExpression, string sql, Expression arguments);

-        public override bool RequiresRuntimeProjectionRemapping { get; }

-        public override IRelationalValueBufferFactory CreateValueBufferFactory(IRelationalValueBufferFactoryFactory relationalValueBufferFactoryFactory, DbDataReader dataReader);

-        public override Expression Visit(Expression expression);

-    }
-}
```

