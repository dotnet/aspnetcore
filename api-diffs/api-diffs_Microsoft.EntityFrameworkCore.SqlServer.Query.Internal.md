# Microsoft.EntityFrameworkCore.SqlServer.Query.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.SqlServer.Query.Internal {
 {
-    public class SqlServerCompiledQueryCacheKeyGenerator : RelationalCompiledQueryCacheKeyGenerator {
 {
-        public SqlServerCompiledQueryCacheKeyGenerator(CompiledQueryCacheKeyGeneratorDependencies dependencies, RelationalCompiledQueryCacheKeyGeneratorDependencies relationalDependencies);

-        public override object GenerateCacheKey(Expression query, bool async);

-    }
-    public class SqlServerQueryCompilationContext : RelationalQueryCompilationContext {
 {
-        public SqlServerQueryCompilationContext(QueryCompilationContextDependencies dependencies, ILinqOperatorProvider linqOperatorProvider, IQueryMethodProvider queryMethodProvider, bool trackQueryResults);

-        public override bool IsLateralJoinSupported { get; }

-    }
-    public class SqlServerQueryCompilationContextFactory : RelationalQueryCompilationContextFactory {
 {
-        public SqlServerQueryCompilationContextFactory(QueryCompilationContextDependencies dependencies, RelationalQueryCompilationContextDependencies relationalDependencies);

-        public override QueryCompilationContext Create(bool async);

-    }
-}
```

