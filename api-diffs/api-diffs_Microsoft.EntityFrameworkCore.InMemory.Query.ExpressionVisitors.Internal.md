# Microsoft.EntityFrameworkCore.InMemory.Query.ExpressionVisitors.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.InMemory.Query.ExpressionVisitors.Internal {
 {
-    public class InMemoryEntityQueryableExpressionVisitor : EntityQueryableExpressionVisitor {
 {
-        public InMemoryEntityQueryableExpressionVisitor(IModel model, IInMemoryMaterializerFactory materializerFactory, EntityQueryModelVisitor entityQueryModelVisitor, IQuerySource querySource);

-        protected override Expression VisitEntityQueryable(Type elementType);

-    }
-    public class InMemoryEntityQueryableExpressionVisitorFactory : IEntityQueryableExpressionVisitorFactory {
 {
-        public InMemoryEntityQueryableExpressionVisitorFactory(IModel model, IInMemoryMaterializerFactory materializerFactory);

-        public virtual ExpressionVisitor Create(EntityQueryModelVisitor queryModelVisitor, IQuerySource querySource);

-    }
-}
```

