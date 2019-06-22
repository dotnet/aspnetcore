# Microsoft.EntityFrameworkCore.Query.Expressions.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.Query.Expressions.Internal {
 {
-    public interface IPrintable {
 {
-        void Print(ExpressionPrinter expressionPrinter);

-    }
-    public class NullConditionalExpression : Expression, IPrintable {
 {
-        public NullConditionalExpression(Expression caller, Expression accessOperation);

-        public virtual Expression AccessOperation { get; }

-        public virtual Expression Caller { get; }

-        public override bool CanReduce { get; }

-        public override ExpressionType NodeType { get; }

-        public override Type Type { get; }

-        public virtual void Print(ExpressionPrinter expressionPrinter);

-        public override Expression Reduce();

-        public override string ToString();

-        protected override Expression VisitChildren(ExpressionVisitor visitor);

-    }
-    public class NullSafeEqualExpression : Expression, IPrintable {
 {
-        public NullSafeEqualExpression(Expression outerKeyNullCheck, BinaryExpression equalExpression);

-        public override bool CanReduce { get; }

-        public virtual BinaryExpression EqualExpression { get; }

-        public override ExpressionType NodeType { get; }

-        public virtual Expression OuterKeyNullCheck { get; }

-        public override Type Type { get; }

-        public virtual void Print(ExpressionPrinter expressionPrinter);

-        public override Expression Reduce();

-        public override string ToString();

-        protected override Expression VisitChildren(ExpressionVisitor visitor);

-    }
-}
```

