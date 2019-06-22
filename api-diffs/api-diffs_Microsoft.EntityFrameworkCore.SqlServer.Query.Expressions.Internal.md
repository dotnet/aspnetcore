# Microsoft.EntityFrameworkCore.SqlServer.Query.Expressions.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.SqlServer.Query.Expressions.Internal {
 {
-    public class RowNumberExpression : Expression {
 {
-        public RowNumberExpression(IReadOnlyList<Ordering> orderings);

-        public override bool CanReduce { get; }

-        public override ExpressionType NodeType { get; }

-        public virtual IReadOnlyList<Ordering> Orderings { get; }

-        public override Type Type { get; }

-        protected override Expression Accept(ExpressionVisitor visitor);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        protected override Expression VisitChildren(ExpressionVisitor visitor);

-    }
-}
```

