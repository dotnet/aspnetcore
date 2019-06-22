# Microsoft.EntityFrameworkCore.InMemory.Query.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.InMemory.Query.Internal {
 {
-    public interface IInMemoryMaterializerFactory {
 {
-        Expression<Func<IEntityType, MaterializationContext, object>> CreateMaterializer(IEntityType entityType);

-    }
-    public class InMemoryMaterializerFactory : IInMemoryMaterializerFactory {
 {
-        public InMemoryMaterializerFactory(IEntityMaterializerSource entityMaterializerSource);

-        public virtual Expression<Func<IEntityType, MaterializationContext, object>> CreateMaterializer(IEntityType entityType);

-    }
-    public class InMemoryQueryContext : QueryContext {
 {
-        public InMemoryQueryContext(QueryContextDependencies dependencies, Func<IQueryBuffer> queryBufferFactory, IInMemoryStore store);

-        public virtual IInMemoryStore Store { get; }

-    }
-    public class InMemoryQueryContextFactory : QueryContextFactory {
 {
-        public InMemoryQueryContextFactory(QueryContextDependencies dependencies, IInMemoryStoreCache storeCache, IDbContextOptions contextOptions);

-        public override QueryContext Create();

-    }
-    public class InMemoryQueryModelVisitor : EntityQueryModelVisitor {
 {
-        public static readonly MethodInfo EntityQueryMethodInfo;

-        public static readonly MethodInfo ProjectionQueryMethodInfo;

-        public InMemoryQueryModelVisitor(EntityQueryModelVisitorDependencies dependencies, QueryCompilationContext queryCompilationContext);

-    }
-    public class InMemoryQueryModelVisitorFactory : EntityQueryModelVisitorFactory {
 {
-        public InMemoryQueryModelVisitorFactory(EntityQueryModelVisitorDependencies dependencies);

-        public override EntityQueryModelVisitor Create(QueryCompilationContext queryCompilationContext, EntityQueryModelVisitor parentEntityQueryModelVisitor);

-    }
-}
```

